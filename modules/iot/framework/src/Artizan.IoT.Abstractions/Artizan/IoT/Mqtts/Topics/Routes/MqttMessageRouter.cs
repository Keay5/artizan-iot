using Artizan.IoT.Mqtts.MessageHanlders;
using Artizan.IoT.Mqtts.Messages;
using Artizan.IoT.Mqtts.Messages.Parsers;
using Artizan.IoT.Mqtts.Topics.Registrys;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtts.Topics.Routes;

/// <summary>
/// MQTT消息路由核心（核心职责：消息Topic匹配、Handler调度、全流程执行）
/// 【设计思想】：单一职责原则 - 仅处理消息路由逻辑，不管理路由元数据
/// 【设计理念】：流程化设计 - 将路由逻辑拆分为多个步骤，便于维护和扩展
/// 【设计模式】：策略模式 - 可扩展不同的匹配策略、执行策略
/// </summary>
public class MqttMessageRouter : IMqttMessageRouter, ISingletonDependency, IDisposable
{
    #region 私有字段（依赖+配置+并发控制）
    /// <summary>
    /// 日志组件（ABP依赖注入）
    /// </summary>
    private readonly ILogger<MqttMessageRouter> _logger;

    /// <summary>
    /// 全局根ServiceProvider（ABP注入，永不释放，用于创建Handler）
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Topic模板解析器（负责将模板转为正则，匹配实际Topic）
    /// </summary>
    private readonly MqttTopicTemplateParser _templateParser;

    /// <summary>
    /// 路由注册中心（依赖注入，获取已注册的路由元数据）
    /// </summary>
    private readonly IDynamicMqttTopicRegistry _topicRegistry;

    /// <summary>
    /// 路由配置项（如最大并发数）
    /// </summary>
    private readonly MqttRouterOptions _routerOptions;

    /// <summary>
    /// 并发控制信号量（限制最大并发处理数，避免线程池耗尽）
    /// 【设计】：SemaphoreSlim确保并发控制，防止高并发下的资源耗尽
    /// </summary>
    private readonly SemaphoreSlim _concurrentSemaphore;
    #endregion

    #region 构造函数（依赖注入+配置初始化）
    /// <summary>
    /// 构造函数（依赖注入：日志、ServiceProvider、解析器、注册中心、配置）
    /// 【设计理念】：依赖倒置 - 依赖抽象（IDynamicMqttTopicRegistry）而非具体实现
    /// </summary>
    /// <param name="logger">日志组件</param>
    /// <param name="serviceProvider">全局根ServiceProvider</param>
    /// <param name="templateParser">Topic模板解析器</param>
    /// <param name="topicRegistry">路由注册中心</param>
    /// <param name="routerOptions">路由配置项（IOptions模式）</param>
    public MqttMessageRouter(
        ILogger<MqttMessageRouter> logger,
        IServiceProvider serviceProvider,
        MqttTopicTemplateParser templateParser,
        IDynamicMqttTopicRegistry topicRegistry,
        IOptions<MqttRouterOptions> routerOptions)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _templateParser = templateParser;
        _topicRegistry = topicRegistry;
        _routerOptions = routerOptions.Value;

        // 初始化并发控制信号量（最大并发数从配置读取）
        _concurrentSemaphore = new SemaphoreSlim(
            initialCount: _routerOptions.MaxConcurrentHandlers,
            maxCount: _routerOptions.MaxConcurrentHandlers);

        _logger.LogInformation("[MQTT路由初始化] 完成 | 最大并发数：{MaxConcurrent}", _routerOptions.MaxConcurrentHandlers);
    }
    #endregion

    #region 核心功能：消息路由全流程
    /// <summary>
    /// 路由消息到匹配的Handler（全流程：前置校验→并发控制→Topic匹配→Handler执行→结果记录）
    /// 【设计理念】：异常边界 - 外层捕获所有异常，确保路由系统不崩溃
    /// 【设计模式】：模板方法模式 - 固定流程框架，步骤可扩展
    /// </summary>
    /// <param name="context">MQTT消息上下文（包含Topic、Payload、TraceId等）</param>
    /// <param name="cancellationToken">取消令牌（支持优雅取消）</param>
    /// <returns>Task（异步执行）</returns>
    public async Task RouteMessageAsync(MqttMessageContext context, CancellationToken cancellationToken = default)
    {
        var routeStopwatch = Stopwatch.StartNew();
        // 防御式编程：校验上下文非空
        Check.NotNull(context, nameof(context));
        var traceId = context.TraceId;
        IMqttMessageHandler? matchedHandler = null; // 用于后续资源释放

        try
        {
            // 步骤1：前置校验（Topic为空、上下文非法等）
            if (!await ValidatePreconditionAsync(context))
                return;

            // 步骤2：并发控制（获取信号量，达到最大并发数时阻塞）
            // 扩展点：可添加超时控制（WaitAsync(TimeSpan)）
            await _concurrentSemaphore.WaitAsync(cancellationToken);

            try
            {
                // 步骤3：核心路由逻辑
                matchedHandler = await ProcessRoutingCoreAsync(context, traceId);
            }
            catch (Exception ex)
            {
                // 系统异常：记录日志并向上抛出（触发监控告警）
                _logger.LogError(ex, "[{TraceId}] 系统异常：{Message}", traceId, ex.Message);
                context.UpdateStepResult("RoutStep/Routing", false, TimeSpan.Zero, "系统异常", ex);
                throw;
            }
            finally
            {
                // 必须释放信号量，避免死锁
                _concurrentSemaphore.Release();
                // 释放Handler资源（若实现IDisposable）
                DisposeHandler(matchedHandler);
            }
        }
        catch (OperationCanceledException ex)
        {
            // 取消异常：优雅记录，不抛出
            context.UpdateStepResult("RouteStep", false, TimeSpan.Zero, "路由被取消", ex);
            _logger.LogInformation(ex, "[{TraceId}] [路由取消] | Topic：{Topic}", traceId, context.Topic);
        }
        catch (Exception ex)
        {
            context.SetGlobalException(ex);
            context.UpdateStepResult("RouteStep", false, TimeSpan.Zero, "路由系统致命异常", ex);

            // 汇总结果：输出结构化日志
            var logDict = context.ToLogDictionary();
            // 致命异常：记录并标记上下文，确保上层感知
            _logger.LogError(ex, "[{TraceId}] [路由系统] 致命异常 | 汇总日志：{LogDict}", traceId, logDict);
        }
    }

    #endregion

    #region 流程拆分：前置校验
    /// <summary>
    /// 路由前置校验（Topic为空、上下文非法等）
    /// 【扩展点】：可扩展更多校验规则（如Payload非空、TraceId合法等）
    /// </summary>
    /// <param name="context">消息上下文</param>
    /// <returns>校验通过返回true，失败返回false</returns>
    private async Task<bool> ValidatePreconditionAsync(MqttMessageContext context)
    {
        var traceId = context.TraceId;

        // 校验1：Topic为空
        if (string.IsNullOrWhiteSpace(context.Topic))
        {
            var errorMsg = "Topic为空";
            _logger.LogWarning("[{TraceId}] [路由失败] | 原因：{ErrorMsg}", traceId, errorMsg);
            context.UpdateStepResult("RouteStep/Validate", false, TimeSpan.Zero, errorMsg);
            return await Task.FromResult(false);
        }

        // 扩展点：添加Payload非空校验
        // if (context.RawMessage?.Payload == null) { ... }

        // 扩展点：添加TraceId合法性校验
        // if (!Guid.TryParse(traceId, out _)) { ... }

        return await Task.FromResult(true);
    }
    #endregion

    #region 流程拆分：核心路由逻辑
    /// <summary>
    /// 核心路由逻辑（获取排序后的路由→遍历匹配→执行Handler）
    /// 【设计理念】：关注点分离 - 将核心逻辑拆分为多个小方法，便于维护
    /// </summary>
    /// <param name="context">消息上下文</param>
    /// <param name="traceId">追踪ID（便于日志关联）</param>
    /// <param name="matchedHandler">匹配到的Handler（用于后续释放）</param>
    /// <returns>Task</returns>
    private async Task<IMqttMessageHandler?> ProcessRoutingCoreAsync(MqttMessageContext context, string traceId)
    {
        IMqttMessageHandler? matchedHandler = null; // 本地变量存储Handler

        // 启动路由耗时统计
        var routeStopwatch = Stopwatch.StartNew();

        // 步骤1：获取排序后的路由元数据（只读快照，避免迭代时集合修改）
        var sortedTopics = _topicRegistry.GetSortedTopics();


        // 步骤2：无已注册路由的处理
        if (sortedTopics.IsEmpty)
        {
            await HandleNoRegisteredTopicsAsync(context, traceId, routeStopwatch);
            return null;
        }

        // 步骤3：遍历路由元数据，按优先级匹配Topic
        foreach (var topicMeta in sortedTopics)
        {
            // 匹配成功则终止遍历，执行Handler
            var (isMatch, handler) = await TryMatchAndExecuteHandlerAsync(context, traceId, topicMeta, routeStopwatch);
            if (isMatch)
            {
                matchedHandler = handler; // 赋值给外层变量，用于后续释放
                return matchedHandler;
            }
        }

        // 步骤4：无匹配Handler的处理
        await HandleNoMatchedHandlerAsync(context, traceId, routeStopwatch);

        return matchedHandler;
    }
    #endregion

    #region 流程拆分：Topic匹配+Handler执行
    /// <summary>
    /// 尝试匹配Topic并执行对应的Handler
    /// 【设计理念】：异常隔离 - 单个路由匹配失败不影响其他路由
    /// </summary>
    /// <param name="context">消息上下文</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="topicMeta">路由元数据</param>
    /// <param name="routeStopwatch">路由耗时统计</param>
    /// <param name="matchedHandler">匹配到的Handler</param>
    /// <returns>匹配并执行成功返回true，否则返回false</returns>
    private async Task<(bool IsMatch, IMqttMessageHandler? Handler)> TryMatchAndExecuteHandlerAsync(
        MqttMessageContext context,
        string traceId,
        DynamicMqttTopicMetadata topicMeta,
        Stopwatch routeStopwatch)
    {
        try
        {
            // 步骤1：解析Topic模板并匹配实际Topic
            var parseResult = _templateParser.Parse(topicMeta.TopicTemplate);
            if (!parseResult.TemplateRegex.IsMatch(context.Topic))
            {
                return (false, null); // 匹配失败，返回false+null
            }

            // 步骤2：填充上下文（占位符、ProductKey、DeviceName等）
            PopulateContextFromTopic(context, parseResult);

            // 步骤3：记录路由匹配成功
            routeStopwatch.Stop();
            context.UpdateStepResult("RouteStep/RouteMatch", true, routeStopwatch.Elapsed);
            _logger.LogDebug(
                "[{TraceId}][MQTT Topic 路由匹配] 成功 | Topic：{Topic} | 模板：{Template} | 优先级：{Priority} | 耗时：{ElapsedMs}ms",
                traceId, context.Topic, topicMeta.TopicTemplate, topicMeta.Priority, routeStopwatch.ElapsedMilliseconds);

            // 步骤4：创建Handler实例（传入全局根ServiceProvider）
            var matchedHandler = topicMeta.CreateHandler(_serviceProvider);

            // 步骤5：执行Handler并记录结果
            await ExecuteHandlerAsync(context, traceId, matchedHandler);

            return (true, matchedHandler); // 匹配并执行成功，终止遍历
        }
        catch (Exception ex)
        {
            // 单个路由匹配/执行异常，记录日志并继续下一个路由
            _logger.LogError(
                ex, "[{TraceId}][MQTT Topic 路由匹配] 执行异常 | 模板：{Template} | Topic：{Topic}",
                traceId, topicMeta.TopicTemplate, context.Topic);
            context.UpdateStepResult("RouteStep", false, TimeSpan.Zero, "系统异常", ex);

            return (false, null); // 异常时返回false+null
        }
    }

    /// <summary>
    /// 从Topic中提取占位符并填充上下文
    /// 【扩展点】：可扩展更多核心字段（如DeviceType、ProductType等）
    /// </summary>
    /// <param name="context">消息上下文</param>
    /// <param name="parseResult">Topic模板解析结果</param>
    private void PopulateContextFromTopic(MqttMessageContext context, TopicTemplateParseResult parseResult)
    {
        // 匹配实际Topic与模板正则
        var match = parseResult.TemplateRegex.Match(context.Topic);

        // 步骤1：填充所有占位符（如${productKey}→productKey=xxx）
        foreach (var placeholderName in parseResult.PlaceholderNames)
        {
            if (match.Groups.TryGetValue(placeholderName, out var group) && !string.IsNullOrWhiteSpace(group.Value))
            {
                context.TopicPlaceholderValues[placeholderName] = group.Value;
            }
        }

        // 步骤2：填充核心业务字段（ProductKey/DeviceName）
        if (context.TopicPlaceholderValues.TryGetValue("productKey", out var productKey))
        {
            // 如果：MqttMessageContext中ProductKey是只读属性，此处通过反射更新（或修改为可写，推荐生产环境修改为可写）
            //var productKeyProperty = typeof(MqttMessageContext).GetProperty(nameof(context.ProductKey), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            //productKeyProperty?.SetValue(context, productKey);
            context.ProductKey = productKey;
        }
        if (context.TopicPlaceholderValues.TryGetValue("deviceName", out var deviceName))
        {
            context.DeviceName = deviceName;
        }

        // 扩展点：填充更多业务字段（如从Topic中提取eventType、messageType等）
        // if (context.TopicPlaceholderValues.TryGetValue("eventType", out var eventType))
        // {
        //     context.EventType = eventType;
        // }
    }

    /// <summary>
    /// 执行Handler并记录执行结果
    /// 【设计理念】：可观测性 - 记录执行耗时、成功状态，便于监控和问题排查
    /// </summary>
    /// <param name="context">消息上下文</param>
    /// <param name="traceId">追踪ID</param>
    /// <param name="handler">匹配到的Handler</param>
    /// <returns>Task</returns>
    private async Task ExecuteHandlerAsync(MqttMessageContext context, string traceId, IMqttMessageHandler handler)
    {
        var handlerStopwatch = Stopwatch.StartNew();

        try
        {
            // 执行Handler核心逻辑
            await handler.HandleAsync(context);
            handlerStopwatch.Stop();

            // 记录Handler执行结果（成功状态、耗时）
            var isSuccess = context.IsParsedSuccess && context.GlobalException == null;
            context.UpdateStepResult(
                stepName: $"RouteStep/ExecuteHandler/{handler.GetType().Name}",
                isSuccess: isSuccess,
                elapsed: handlerStopwatch.Elapsed);

            _logger.LogInformation(
                "[{TraceId}][MQTT Message Handler] 执行完成 | Handler：{HandlerType} | 耗时：{ElapsedMs}ms | 状态：{Success}",
                traceId, handler.GetType().Name, handlerStopwatch.ElapsedMilliseconds, isSuccess ? "成功" : "失败");
        }
        catch (Exception ex)
        {
            // Handler执行异常，记录并标记上下文
            handlerStopwatch.Stop();
            _logger.LogError(ex, "[{TraceId}][MQTT Message Handler] 执行异常 | Handler：{HandlerType}", traceId, handler.GetType().Name);
            context.UpdateStepResult(
                stepName: $"RouteStep/ExecuteHandler/{handler.GetType().Name}",
                isSuccess: false,
                elapsed: handlerStopwatch.Elapsed,
                errorMsg: ex.Message,
                exception: ex);
            throw; // 抛出异常，让上层感知（或根据业务决定是否吞掉）
        }
    }
    #endregion

    #region 流程拆分：异常场景处理
    /// <summary>
    /// 处理无已注册路由的场景
    /// </summary>
    private async Task HandleNoRegisteredTopicsAsync(MqttMessageContext context, string traceId, Stopwatch stopwatch)
    {
        var errorMsg = "无已注册的Topic路由规则";
        _logger.LogWarning("[{TraceId}] [路由失败] | 原因：{ErrorMsg}", traceId, errorMsg);
        context.UpdateStepResult("RouteMatch", false, stopwatch.Elapsed, errorMsg);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理无匹配Handler的场景
    /// </summary>
    private async Task HandleNoMatchedHandlerAsync(MqttMessageContext context, string traceId, Stopwatch stopwatch)
    {
        var errorMsg = $"未找到匹配的Handler | Topic：{context.Topic}";
        _logger.LogWarning("[{TraceId}] [路由失败] | 原因：{ErrorMsg}", traceId, errorMsg);
        context.UpdateStepResult("RouteMatch", false, stopwatch.Elapsed, errorMsg);
        await Task.CompletedTask;
    }
    #endregion

    #region 资源释放
    /// <summary>
    /// 释放Handler资源（若实现IDisposable）
    /// 【设计理念】：资源管理 - 确保实现IDisposable的Handler被正确释放，避免内存泄漏
    /// </summary>
    /// <param name="handler">Handler实例</param>
    private void DisposeHandler(IMqttMessageHandler? handler)
    {
        if (handler == null)
        {
            return;
        }

        if (handler is IDisposable disposableHandler)
        {
            try
            {
                disposableHandler.Dispose();
                _logger.LogDebug("[MQTT Message Handler资源释放] 成功 | 类型：{HandlerType}", handler.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MQTT Message Handler资源释放] 失败 | 类型：{HandlerType}", handler.GetType().Name);
            }
        }
    }

    /// <summary>
    /// 释放路由组件资源（如SemaphoreSlim）
    /// 【设计理念】：IDisposable规范 - 释放非托管资源，避免资源泄漏
    /// </summary>
    public void Dispose()
    {
        _concurrentSemaphore?.Dispose();
        GC.SuppressFinalize(this);
        _logger.LogInformation("[MQTT路由组件] 资源释放完成");
    }
    #endregion
}