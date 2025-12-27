using Artizan.IoT.Alinks.DataObjects.MessageCommunications;
using Artizan.IoT.Messages.PostProcessors;
using Artizan.IoT.Mqtts.MessageHanlders;
using Artizan.IoT.Mqtts.Messages;
using Artizan.IoT.Mqtts.Messages.Parsers;
using Artizan.IoT.Mqtts.Topics;
using Artizan.IoT.Mqtts.Topics.Routes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoTHub.Mqtts.MessageHanlders;

/// <summary>
/// 设备属性上报Handler（生产环境业务示例）
/// 特性说明：
/// 1. [MqttTopicRoute]：标记Topic模板，模块启动时自动注册；
/// 2. Priority=10：普通业务优先级；
/// 3. 继承SafeMqttMessageHandler：利用内置SyncRoot保障线程安全；
/// 4. ITransientDependency：瞬时生命周期，天然隔离状态。
/// </summary>
/// <example>
/// MQTT Topic：/sys/${productKey}/${deviceName}/thing/event/property/post
/// MQTT Payload： Alink Json:：
///     {"method":"thing.event.property.post","id":1,"params":{"prop_float":0.0,"prop_int16":50,"prop_bool":true},"version":"1.0"}
/// </example>  
//[MqttTopicRoute("/sys/${productKey}/${deviceName}/thing/event/property/post", Priority = 10)]
[MqttTopicRoute(MqttTopicSpeciesConsts.ThingModelCommunication.PropertyReport.Post, Priority = 10)]
public class ThingPropertyReportMessageHandler : SafeMqttMessageHandler, ITransientDependency
{ 
    private readonly ILogger<ThingPropertyReportMessageHandler> _logger;
    private readonly IEnumerable<IMessagePostProcessor<MqttMessageContext>> _sortedProcessors; // 后处理插件

    ///// <summary>
    ///// 支持的消息类型（与Topic匹配，例如"thing.property.report"）
    ///// </summary>
    // ABP依赖注入：自动注入日志、仓储等服务
    public ThingPropertyReportMessageHandler(
        ILogger<ThingPropertyReportMessageHandler> logger,
        IEnumerable<IMessagePostProcessor<MqttMessageContext>> postProcessors)
    {
        _logger = logger;

        _sortedProcessors = postProcessors
            .Where(p => p.IsEnabled) // 过滤启用的插件
            .OrderBy(p => p.Priority); // 按优先级执行
    }

    /// <summary>
    /// 核心业务逻辑：属性上报处理
    /// 流程：解析Payload→验证设备→保存属性→更新上下文状态
    /// </summary>
    public override async Task HandleAsync(MqttMessageContext context)
    {
        // 防御式校验：检查上下文是否已释放
        context.CheckDisposed();

        try
        {
            // 消息解析
            var parseStopwatch = Stopwatch.StartNew();
            await ParseMessageAsync(context);
            parseStopwatch.Stop();

            // 若解析失败，终止处理（记录步骤结果）
            if (!context.IsParsedSuccess)
            {
                context.UpdateStepResult(
                    stepName: $"MQTT Messgae Handler:{nameof(ThingPropertyReportMessageHandler)}/ParseMessage",
                    isSuccess: false,
                    elapsed: parseStopwatch.Elapsed,
                    errorMsg: context.ParseErrorMsg
                );

                return;
            }

            //// 5. 设备合法性验证（核心业务步骤1）
            //var validationStopwatch = System.Diagnostics.Stopwatch.StartNew();
            //var (isValid, validationError) = await _deviceValidationService.ValidateAsync(
            //    context.ProductKey, context.DeviceName
            //);
            //validationStopwatch.Stop();

            //context.UpdateStepResult(
            //    stepName: "DeviceValidation",
            //    isSuccess: isValid,
            //    elapsed: validationStopwatch.Elapsed,
            //    errorMsg: !isValid ? validationError : null
            //);

            //if (!isValid)
            //{
            //    // 验证失败，抛出异常（由上层分发器捕获并处理）
            //    throw new InvalidOperationException($"设备[{context.ProductKey}/{context.DeviceName}]验证失败：{validationError}");
            //}

            //// 6. 属性数据处理（核心业务步骤2：格式转换、规则校验等）
            //var processStopwatch = Stopwatch.StartNew();
            //var processedData = await _propertyProcessor.ProcessAsync(
            //    context.ProductKey,
            //    context.DeviceName,
            //    (PropertyReportData)context.ParsedData // 强转解析后的数据（依赖ParseType保障类型安全）
            //);
            //processStopwatch.Stop();

            //context.UpdateStepResult(
            //    stepName: "PropertyProcessing",
            //    isSuccess: true,
            //    elapsed: processStopwatch.Elapsed
            //);

            // 7. 触发后消息处理插件（如缓存、时序库存储等）
            await ExecutePostProcessorsAsync(context);
        }
        catch (Exception ex)
        {
            // 异常处理：记录步骤失败+追踪异常
            context.UpdateStepResult(
                stepName: $"MQTT Messgae Handler:{nameof(ThingPropertyReportMessageHandler)}",
                isSuccess: false,
                elapsed: TimeSpan.Zero,
                errorMsg: $"[{context.TraceId}][MQTT Messgae Handler] | 处理器 [{nameof(ThingPropertyReportMessageHandler)}] 内部发生异常 | 异常：{ex.Message}",
                exception: ex
            );

            throw; // 抛出异常由分发器统一处理（如熔断、重试）
        }
    }

    #region 私有方法（解析消息+执行插件）
    /// <summary>
    /// 解析MQTT原始消息为属性上报结构化数据
    /// </summary>
    private async Task ParseMessageAsync(MqttMessageContext context)
    {
        // 若已解析，直接返回（避免重复解析）
        if (context.IsParsedSuccess)
        {
            return;
        }

        //try
        //{
        //    // 调用专用解析器（例如从RawMessage.Payload解析为PropertyReportData）
        //    var parser = new PropertyReportMessageParser();
        //    var parsedData = await parser.ParseAsync(context.RawMessage.Payload);

        //    // 更新解析结果到上下文（标记成功）
        //    context.SetParseSuccess(
        //        parseType: MqttMessageDataParseType.PropertyReport,
        //        parsedData: parsedData,
        //        elapsed: parser.Elapsed
        //    );
        //}
        //catch (Exception ex)
        //{
        //    // 更新解析结果到上下文（标记失败）
        //    context.SetParseFailed(
        //        errorMsg: $"属性上报消息解析失败：{ex.Message}",
        //        elapsed: TimeSpan.Zero,
        //        exception: ex
        //    );
        //}

        var traceId = context.TraceId;
        var stepName = nameof(ThingPropertyReportMessageHandler);

        _logger.LogDebug("[{TraceId}][物模型-属性上报] 开始处理", traceId);

        // 1. 从上下文提取核心数据（路由系统已自动填充）
        var productKey = context.ProductKey;
        var deviceName = context.DeviceName;
        var payloadBytes = context.RawMessage.PayloadSegment.Array;
        var payloadString = context.RawMessage.PayloadString;

        // 2. 业务校验：核心字段不能为空
        if (string.IsNullOrWhiteSpace(productKey) || string.IsNullOrWhiteSpace(deviceName))
        {
            var errorMsg = $"核心字段缺失：productKey={productKey}，deviceName={deviceName}";
            _logger.LogError("[{TraceId}][物模型-属性上报][业务校验] 失败：{ErrorMsg}", traceId, errorMsg);
            context.SetParseFailed(errorMsg, TimeSpan.Zero);
            return;
        }

        // TODO:参数TSL校验
        //var validateResult =  await alinkData.Validate();

        var parseStopwatch = Stopwatch.StartNew();

        // 3. 解析Payload（业务自主解析，路由系统不干预）
        PropertyPostRequest? alinkData = null;
        try
        {
            alinkData = JsonSerializer.Deserialize<PropertyPostRequest>(payloadString);

            if (alinkData == null || alinkData.Params == null || !alinkData.Params.Any())
            {
                var errorMsg = "解析后「Alink数据」为空，尝试检查上传的「数据格式」是否正确。";
                _logger.LogError("[{TraceId}][物模型-属性上报][数据解析] ×失败×：{ErrorMsg}", traceId, errorMsg);
                context.SetParseFailed(
                   errorMsg: errorMsg,
                   elapsed: parseStopwatch.Elapsed);

                return;
            }

            //TODO？：var alinkDataContext = new AlinkDataContext(_currentTraceIdAccessor);

            parseStopwatch.Stop();
            // 更新解析结果到上下文（标记成功）
            context.SetParseSuccess(
                parseType: MqttMessageDataParseType.AlinkJson,
                parsedData: alinkData,
                elapsed: parseStopwatch.Elapsed
            );
        }
        catch (Exception ex)
        {
            parseStopwatch.Stop();
            var errorMsg = "Payload「数据格式」有误，解析失败";
            _logger.LogError(ex, "[{TraceId}][物模型-属性上报][数据解析] 失败：{ErrorMsg} | 摘要：{PayloadSummary}",
                traceId, errorMsg, context.RawMessage.GetPayloadSummary(100));

            //context.SetParseFailed(errorMsg, parseStopwatch.Elapsed, ex);
            // 更新解析结果到上下文（标记失败）
            context.SetParseFailed(
                errorMsg: $"属性上报消息解析失败：{ex.Message}",
                elapsed: TimeSpan.Zero,
                exception: ex
            );

            return;
        }

    }

    /// <summary>
    /// 执行后处理插件（按优先级排序）
    /// </summary>
    private async Task ExecutePostProcessorsAsync(MqttMessageContext context)
    {
        foreach (var processor in _sortedProcessors)
        {
            var processorStopwatch = Stopwatch.StartNew();

            try
            {
                await processor.ProcessAsync(context);
                context.UpdateStepResult(
                    stepName: $"PostProcessor:{processor.GetType().Name}",
                    isSuccess: true,
                    elapsed: processorStopwatch.Elapsed
                );
            }
            catch (Exception ex)
            {
                context.UpdateStepResult(
                    stepName: $"PostProcessor:{processor.GetType().Name}",
                    isSuccess: false,
                    elapsed: processorStopwatch.Elapsed,
                    errorMsg: $"插件执行失败：{ex.Message}",
                    exception: ex
                );
            }
        }
    }

    #endregion

    #region 方法备份

    public async Task Handle_BackupAsync(MqttMessageContext context)
    {
        var traceId = context.TraceId;
        var stepName = nameof(ThingPropertyReportMessageHandler);

        _logger.LogDebug("[{TraceId}][物模型-属性上报] 开始处理", traceId);

        // 线程安全块（若有共享状态，必须加锁）
        lock (SyncRoot)
        {
            // 示例：共享状态计数（如累计处理次数）
            // _processCount++;
        }

        // 否则就是伪造的Topic，可能存在安全风险：比如设备A冒充设备B，数据篡改等。
        // TODO [#0002]: 根据业务需要，添加更多的校验逻辑，如设备状态检查、权限验证等。

        try
        {
            // 1. 从上下文提取核心数据（路由系统已自动填充）
            var productKey = context.ProductKey;
            var deviceName = context.DeviceName;
            var payloadBytes = context.RawMessage.PayloadSegment.Array;
            var payloadString = context.RawMessage.PayloadString;

            // 2. 业务校验：核心字段不能为空
            if (string.IsNullOrWhiteSpace(productKey) || string.IsNullOrWhiteSpace(deviceName))
            {
                var errorMsg = $"核心字段缺失：productKey={productKey}，deviceName={deviceName}";
                _logger.LogError("[{TraceId}][物模型-属性上报][业务校验] 失败：{ErrorMsg}", traceId, errorMsg);
                context.SetParseFailed(errorMsg, TimeSpan.Zero);
                return;
            }

            var parseStopwatch = Stopwatch.StartNew();

            // 3. 解析Payload（业务自主解析，路由系统不干预）
            PropertyPostRequest? alinkData = null;
            try
            {
                alinkData = JsonSerializer.Deserialize<PropertyPostRequest>(payloadString);

                //var validateResult =  await alinkData.Validate();

                parseStopwatch.Stop();
            }
            catch (Exception ex)
            {
                parseStopwatch.Stop();
                var errorMsg = "Payload「数据格式」有误，解析失败";
                _logger.LogError(ex, "[{TraceId}][物模型-属性上报][数据解析] 失败：{ErrorMsg} | 摘要：{PayloadSummary}",
                    traceId, errorMsg, context.RawMessage.GetPayloadSummary(100));
                context.SetParseFailed(errorMsg, parseStopwatch.Elapsed, ex);
                return;
            }

            //if (alinkData == null || alinkData.Params == null || !alinkData.Params.Any())
            //{
            //    var errorMsg = "解析后「Alink数据」为空，尝试检查上传的「数据格式」是否正确。";
            //    _logger.LogError("[{TraceId}][物模型-属性上报][数据解析] ×失败×：{ErrorMsg}", traceId, errorMsg);
            //    context.SetParseFailed(errorMsg, parseStopwatch.Elapsed);
            //    return;
            //}

            //var alinkDataContext = new AlinkDataContext(_currentTraceIdAccessor);
            // 4. 标记解析成功
            context.SetParseSuccess(MqttMessageDataParseType.AlinkJson, alinkData, parseStopwatch.Elapsed);

            // 5. 业务逻辑：保存属性到数据库
            var businessStopwatch = Stopwatch.StartNew();

            #region 模拟场景
            //var device = await _propertyRepository.GetByProductKeyAndDeviceKeyAsync(productKey, deviceName);
            //if (device == null)
            //{
            //    var errorMsg = $"设备不存在：productKey={productKey}，deviceName={deviceName}";
            //    _logger.LogError("[{TraceId}] {ErrorMsg}", traceId, errorMsg);
            //    context.UpdateStepResult("BusinessStep", false, businessStopwatch.Elapsed, errorMsg);
            //    return;
            //}

            // 更新设备属性（业务逻辑）
            //device.UpdateProperties(payload.Params);
            //await _propertyRepository.UpdateAsync(device); 
            #endregion

            //await Task.Delay(500); // 异步延迟，非阻塞（推荐）

            // TODO: 消息分发：缓存、入时序库、设备联动、规则引擎


            businessStopwatch.Stop();

            // 6. 记录业务步骤成功
            context.UpdateStepResult(stepName, true, businessStopwatch.Elapsed);

            _logger.LogInformation(
                "[{TraceId}][物模型-属性上报] 处理成功 | productKey={productKey} | deviceName={deviceName} | 属性数：{Count} | 总耗时：{TotalMs}ms",
                traceId, productKey, deviceName, alinkData.Params.Count,
                (parseStopwatch.Elapsed + businessStopwatch.Elapsed).TotalMilliseconds);
        }
        catch (BusinessException ex)
        {
            // 业务异常：记录日志，不向上抛出
            _logger.LogError(ex, "[{TraceId}][物模型-属性上报] 业务异常：{Message}", traceId, ex.Message);
            context.UpdateStepResult(stepName, false, TimeSpan.Zero, ex.Message);
        }
        catch (Exception ex)
        {
            // 系统异常：记录日志并向上抛出（触发监控告警）
            _logger.LogError(ex, "[{TraceId}][物模型-属性上报]系统异常：{Message}", traceId, ex.Message);
            context.UpdateStepResult(stepName, false, TimeSpan.Zero, "系统异常", ex);
            throw;
        }
    }

    #endregion
}
