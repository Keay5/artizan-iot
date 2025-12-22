//using Artizan.IoT.Mqtts.MessageHanlders;
//using Microsoft.Extensions.Logging;
//using MQTTnet;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Threading.Tasks;
//using Volo.Abp;
//using Volo.Abp.Reflection;

//namespace Artizan.IoT.Mqtts.Topics.Routes;

///// <summary>
///// MQTT Topic 路由管理器（核心组件）
///// 设计思路：
///// 1. 自动扫描：利用 ABP 的 IAssemblyFinder 扫描所有模块中带 MqttTopicRouteAttribute 的 Handler；
///// 2. 路由注册：解析特性中的 Topic 模板，生成通配符路由表，按优先级排序；
///// 3. 消息分发：收到 MQTT 消息后，匹配路由表，调用对应的 Handler 处理；
///// 4. 业务解耦：不干预 Handler 的消息解析和业务逻辑，仅负责“匹配+调度”。
///// </summary>
//public class MqttTopicRouterManager
//{
//    /// <summary>路由表（存储 Topic 通配符模板与 Handler 的映射关系）</summary>
//    private readonly List<RouteEntry> _routeTable = new();

//    /// <summary>ABP 日志器（兼容 ABP 日志系统，记录路由过程日志）</summary>
//    private readonly ILogger<MqttTopicRouterManager> _logger;

//    /// <summary>
//    /// 构造函数（注入 ABP 日志器）
//    /// </summary>
//    /// <param name="logger">ABP 日志服务</param>
//    public MqttTopicRouterManager(ILogger<MqttTopicRouterManager> logger)
//    {
//        _logger = logger;
//    }

//    /// <summary>
//    /// 自动注册所有模块中的 MQTT Handler（核心方法）
//    /// 调用时机：ABP 模块初始化时（OnApplicationInitializationAsync）
//    /// </summary>
//    /// <param name="serviceProvider">ABP 依赖注入容器（用于验证 Handler 可实例化）</param>
//    /// <param name="assemblies">所有模块的程序集（由 ABP 的 IAssemblyFinder 提供）</param>
//    /// <exception cref="AbpException">当 Handler 类型不合法或特性配置错误时抛出</exception>
//    public void AutoRegisterHandlers(IServiceProvider serviceProvider, IEnumerable<Assembly> assemblies)
//    {
//        Check.NotNull(serviceProvider, nameof(serviceProvider));
//        Check.NotNull(assemblies, nameof(assemblies));

//        _logger.LogInformation("开始扫描并注册 MQTT 路由...");

//        foreach (var assembly in assemblies)
//        {
//            try
//            {
//                // 扫描当前程序集中符合条件的 Handler：
//                // 1. 实现 IMqttMessageHandler 接口；
//                // 2. 非接口、非抽象类；
//                // 3. 标记 MqttTopicRouteAttribute 特性。
//                var handlerTypes = assembly.GetTypes()
//                    .Where(t => typeof(IMqttMessageHandler).IsAssignableFrom(t)
//                                && !t.IsInterface
//                                && !t.IsAbstract
//                                && t.GetCustomAttributes<MqttTopicRouteAttribute>(false).Any());

//                foreach (var handlerType in handlerTypes)
//                {
//                    // 提取 Handler 上的所有 MqttTopicRouteAttribute 特性（支持多 Topic 绑定）
//                    var routeAttributes = handlerType.GetCustomAttributes<MqttTopicRouteAttribute>(false)
//                        .Where(attr => attr.Enabled) // 过滤禁用的路由
//                        .ToList();

//                    if (!routeAttributes.Any())
//                    {
//                        _logger.LogDebug("Handler {HandlerType} 未启用任何路由特性，跳过注册", handlerType.FullName);
//                        continue;
//                    }

//                    // 验证 Handler 可通过 DI 容器实例化（避免构造函数依赖缺失）
//                    if (!serviceProvider.CanResolve(handlerType))
//                    {
//                        _logger.LogWarning("Handler {HandlerType} 无法通过依赖注入容器解析（可能缺少构造函数依赖），跳过注册", handlerType.FullName);
//                        continue;
//                    }

//                    // 遍历特性，注册每个 Topic 路由
//                    foreach (var routeAttr in routeAttributes)
//                    {
//                        // 验证 Topic 模板是否合法（必须是 TopicSpeciesConsts 中的常量）
//                        if (!IsValidTopicTemplate(routeAttr.TopicTemplate))
//                        {
//                            _logger.LogError("Handler {HandlerType} 的路由特性配置错误：Topic 模板 {TopicTemplate} 未在 TopicSpeciesConsts 中定义，跳过注册",
//                                handlerType.FullName, routeAttr.TopicTemplate);
//                            continue;
//                        }

//                        // 提取 Topic 模板中的占位符（如 ${productKey} → productKey）
//                        var placeholders = ExtractPlaceholders(routeAttr.TopicTemplate);

//                        // 将 Topic 模板中的占位符替换为 MQTT 通配符 +（单级匹配）
//                        var wildcardTopic = ReplacePlaceholdersWithWildcard(routeAttr.TopicTemplate);

//                        // 添加到路由表
//                        _routeTable.Add(new RouteEntry
//                        {
//                            WildcardTopic = wildcardTopic,
//                            OriginalTopicTemplate = routeAttr.TopicTemplate,
//                            HandlerType = handlerType,
//                            PlaceholderNames = placeholders,
//                            Priority = routeAttr.Priority
//                        });

//                        _logger.LogInformation(
//                            "MQTT 路由注册成功：Handler={HandlerType}，Topic模板={TopicTemplate}，通配符模板={WildcardTopic}，优先级={Priority}",
//                            handlerType.FullName, routeAttr.TopicTemplate, wildcardTopic, routeAttr.Priority);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "扫描程序集 {AssemblyName} 时发生异常，跳过该程序集的 MQTT 路由注册", assembly.FullName);
//            }
//        }

//        // 路由表按优先级排序（数值越大越优先，解决模板冲突）
//        _routeTable.Sort((a, b) => b.Priority.CompareTo(a.Priority));

//        _logger.LogInformation("MQTT 路由注册完成，共注册 {RouteCount} 条路由", _routeTable.Count);
//    }

//    /// <summary>
//    /// 路由并处理 MQTT 消息（核心方法）
//    /// 调用时机：MQTT 客户端收到消息时（ApplicationMessageReceivedAsync 事件）
//    /// </summary>
//    /// <param name="actualTopic">实际收到的 MQTT Topic</param>
//    /// <param name="rawPayload">原始消息载荷（字节数组）</param>
//    /// <param name="protocolContext">MQTT 协议上下文</param>
//    /// <param name="serviceProvider">ABP 依赖注入容器（用于解析 Handler 实例）</param>
//    public async Task RouteAndHandleAsync(
//        string actualTopic,
//        byte[] rawPayload,
//        MqttProtocolContext protocolContext,
//        IServiceProvider serviceProvider)
//    {
//        Check.NotNullOrEmpty(actualTopic, nameof(actualTopic));
//        Check.NotNull(rawPayload, nameof(rawPayload));
//        Check.NotNull(protocolContext, nameof(protocolContext));
//        Check.NotNull(serviceProvider, nameof(serviceProvider));

//        _logger.LogDebug("收到 MQTT 消息：Topic={Topic}，Payload长度={PayloadLength}字节，ClientId={ClientId}",
//            actualTopic, rawPayload.Length, protocolContext.ClientId);

//        try
//        {
//            // 遍历路由表，按优先级匹配 Topic（高优先级先匹配）
//            foreach (var routeEntry in _routeTable)
//            {
//                if (IsTopicMatch(actualTopic, routeEntry.WildcardTopic))
//                {
//                    _logger.LogDebug("Topic {Topic} 匹配路由：通配符模板={WildcardTopic}，Handler={HandlerType}",
//                        actualTopic, routeEntry.WildcardTopic, routeEntry.HandlerType.FullName);

//                    // 从实际 Topic 中提取占位符值（如 /sys/a1b2c3/device001/... → productKey=a1b2c3，deviceName=device001）
//                    var placeholderValues = ExtractPlaceholderValues(
//                        routeEntry.OriginalTopicTemplate,
//                        actualTopic,
//                        routeEntry.PlaceholderNames);

//                    // 创建消息上下文
//                    var messageContext = new MqttMessageContext()
//                    {
//                        ActualTopic = actualTopic,
//                        RawPayload = rawPayload,
//                        PlaceholderValues = placeholderValues,
//                        ProtocolContext = protocolContext
//                    };

//                    // 从 DI 容器创建 Handler 作用域（支持 Scoped 生命周期依赖）
//                    using var scope = serviceProvider.CreateScope();
//                    var handler = (IMqttMessageHandler)scope.ServiceProvider.GetRequiredService(routeEntry.HandlerType);

//                    // 调用 Handler 处理消息
//                    var handleResult = await handler.HandleAsync(messageContext);

//                    // 记录处理结果日志
//                    if (handleResult.IsSuccess)
//                    {
//                        _logger.LogInformation(
//                            "Handler {HandlerId} 处理消息成功：Topic={Topic}，ClientId={ClientId}",
//                            handler.HandlerId, actualTopic, protocolContext.ClientId);
//                    }
//                    else
//                    {
//                        _logger.LogError(
//                            "Handler {HandlerId} 处理消息失败：Topic={Topic}，ClientId={ClientId}，ErrorCode={ErrorCode}，ErrorMessage={ErrorMessage}",
//                            handler.HandlerId, actualTopic, protocolContext.ClientId, handleResult.ErrorCode, handleResult.ErrorMessage);
//                    }

//                    // 匹配到一个 Handler 后直接返回（确保一个消息仅被一个 Handler 处理）
//                    return;
//                }
//            }

//            // 未匹配到任何 Handler
//            _logger.LogWarning(
//                "未找到匹配的 MQTT Handler：Topic={Topic}，ClientId={ClientId}，PayloadLength={PayloadLength}字节",
//                actualTopic, protocolContext.ClientId, rawPayload.Length);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex,
//                "路由并处理 MQTT 消息时发生异常：Topic={Topic}，ClientId={ClientId}",
//                actualTopic, protocolContext.ClientId);
//        }
//    }

//    #region 辅助方法（内部使用，封装路由核心逻辑）
//    /// <summary>
//    /// 验证 Topic 模板是否合法（必须是 TopicSpeciesConsts 中的公共常量）
//    /// </summary>
//    /// <param name="topicTemplate">待验证的 Topic 模板</param>
//    private bool IsValidTopicTemplate(string topicTemplate)
//    {
//        // 利用 ABP 的 ReflectionHelper 递归获取 TopicSpeciesConsts 中的所有公共常量
//        var allValidTemplates = ReflectionHelper.GetPublicConstantsRecursively(typeof(TopicSpeciesConsts))
//            .Select(c => c.Value?.ToString())
//            .Where(t => !string.IsNullOrEmpty(t))
//            .ToHashSet(StringComparer.Ordinal);

//        return allValidTemplates.Contains(topicTemplate);
//    }

//    /// <summary>
//    /// 提取 Topic 模板中的占位符名称（如 ${productKey} → productKey）
//    /// </summary>
//    /// <param name="topicTemplate">Topic 模板</param>
//    private List<string> ExtractPlaceholders(string topicTemplate)
//    {
//        var placeholders = new List<string>();
//        var matches = System.Text.RegularExpressions.Regex.Matches(topicTemplate, @"\$\{(\w+)\}");
//        foreach (System.Text.RegularExpressions.Match match in matches)
//        {
//            if (match.Groups.Count > 1)
//            {
//                placeholders.Add(match.Groups[1].Value);
//            }
//        }
//        return placeholders;
//    }

//    /// <summary>
//    /// 将 Topic 模板中的占位符替换为 MQTT 通配符 +（单级匹配）
//    /// 示例：/sys/${productKey}/${deviceName}/... → /sys/+/+/...
//    /// </summary>
//    /// <param name="topicTemplate">Topic 模板</param>
//    private string ReplacePlaceholdersWithWildcard(string topicTemplate)
//    {
//        return System.Text.RegularExpressions.Regex.Replace(topicTemplate, @"\$\{\w+\}", "+");
//    }

//    /// <summary>
//    /// 从实际 Topic 中提取占位符值
//    /// 示例：模板 /sys/${productKey}/${deviceName}/... → 实际 Topic /sys/a1b2c3/device001/... → { "productKey": "a1b2c3", "deviceName": "device001" }
//    /// </summary>
//    /// <param name="topicTemplate">原始 Topic 模板</param>
//    /// <param name="actualTopic">实际收到的 Topic</param>
//    /// <param name="placeholderNames">占位符名称列表</param>
//    private Dictionary<string, string> ExtractPlaceholderValues(string topicTemplate, string actualTopic, List<string> placeholderNames)
//    {
//        var templateSegments = topicTemplate.Split('/', StringSplitOptions.RemoveEmptyEntries);
//        var actualSegments = actualTopic.Split('/', StringSplitOptions.RemoveEmptyEntries);
//        var placeholderValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

//        if (templateSegments.Length != actualSegments.Length)
//        {
//            _logger.LogWarning(
//                "Topic 模板与实际 Topic 分段数不匹配：模板={TopicTemplate}（分段数={TemplateSegmentCount}），实际Topic={ActualTopic}（分段数={ActualSegmentCount}）",
//                topicTemplate, templateSegments.Length, actualTopic, actualSegments.Length);
//            return placeholderValues;
//        }

//        int placeholderIndex = 0;
//        for (int i = 0; i < templateSegments.Length; i++)
//        {
//            var templateSegment = templateSegments[i];
//            // 判断当前分段是否为占位符（${xxx} 格式）
//            if (templateSegment.StartsWith("${", StringComparison.Ordinal) && templateSegment.EndsWith("}", StringComparison.Ordinal))
//            {
//                if (placeholderIndex < placeholderNames.Count && i < actualSegments.Length)
//                {
//                    var placeholderName = placeholderNames[placeholderIndex];
//                    placeholderValues[placeholderName] = actualSegments[i];
//                    placeholderIndex++;
//                }
//            }
//        }

//        return placeholderValues;
//    }

//    /// <summary>
//    /// 验证实际 Topic 是否与通配符模板匹配（遵循 MQTT 协议规范）
//    /// 依赖：MQTTnet 内置的 Topic 匹配逻辑，确保兼容性
//    /// </summary>
//    /// <param name="actualTopic">实际收到的 Topic</param>
//    /// <param name="wildcardTopic">通配符模板</param>
//    private bool IsTopicMatch(string actualTopic, string wildcardTopic)
//    {
//        //var topicFilter = new MqttTopicFilter
//        //{
//        //    Topic = wildcardTopic,
//        //    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce // 匹配逻辑与 QoS 无关，仅用默认值
//        //};

//        return MqttTopicFilterComparer.Compare(actualTopic, wildcardTopic) == MqttTopicFilterCompareResult.IsMatch;
//    }
//    #endregion

//    /// <summary>
//    /// 路由表条目（内部类，封装路由核心信息）
//    /// </summary>
//    private class RouteEntry
//    {
//        /// <summary>替换占位符后的 MQTT 通配符模板</summary>
//        public string WildcardTopic { get; set; } = string.Empty;

//        /// <summary>原始 Topic 模板（来自 TopicSpeciesConsts等）</summary>
//        public string OriginalTopicTemplate { get; set; } = string.Empty;

//        /// <summary>对应的 Handler 类型</summary>
//        public Type HandlerType { get; set; } = typeof(void);

//        /// <summary>Topic 模板中的占位符名称列表</summary>
//        public List<string> PlaceholderNames { get; set; } = new();

//        /// <summary>路由优先级</summary>
//        public int Priority { get; set; }
//    }
//}
