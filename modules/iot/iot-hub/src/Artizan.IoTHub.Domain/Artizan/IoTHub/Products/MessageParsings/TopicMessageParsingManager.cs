using Artizan.IoT.Mqtts.Etos;
using Artizan.IoTHub.Products.Caches;
using Artizan.IoTHub.Products.MessageParsings.Etos;
using Artizan.IoTHub.Products.Properties;
using Artizan.IoTHub.Topics;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace Artizan.IoTHub.Products.MessageParsings;

public class TopicMessageParsingManager : ISingletonDependency
{
    // 日志组件
    private readonly ILogger<TopicMessageParsingManager> _logger;

    // 产品缓存
    private readonly ProductCache _productCache;
    private readonly ProductMessageParserCache _productMessageParserCache;

    // 分布式事件总线
    private readonly IDistributedEventBus _distributedEventBus;

    // 自定义主题消息解析器
    private readonly ICustomTopicMessageParser _customTopicMessageParser;

    // 物模型消息解析器
    private readonly IThingModelPassThroughTopicMessageParser _thingModelTopicMessageParser;

    public TopicMessageParsingManager(
        ILogger<TopicMessageParsingManager> logger,
        ProductCache productCache,
        IDistributedEventBus distributedEventBus,
        ICustomTopicMessageParser customTopicMessageParser,
        IThingModelPassThroughTopicMessageParser thingModelTopicMessageParser)
    {
        _logger = logger;
        _productCache = productCache;
        _distributedEventBus = distributedEventBus;
        _customTopicMessageParser = customTopicMessageParser;
        _thingModelTopicMessageParser = thingModelTopicMessageParser;
    }

    /// <summary>
    /// 解析Topic消息
    /// </summary>
    /// <param name="eventData">MQTT消息事件数据</param>
    /// <param name="consumerId">消费者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    public async Task TryParseTopicMessageAsync(
        MqttClientPublishTopicEto eventData,
        int consumerId,
        CancellationToken cancellationToken)
    {
        //前置空值校验：生产环境防御性编程
        if (eventData == null)
        {
            _logger.LogWarning("[数据解析][Consumer:{ConsumerId}] 接收到空的MQTT事件数据，跳过处理", consumerId);
            return;
        }

        // 直接抛出 OperationCanceledException；表示任务被取消的异常
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // 自定义Topic消息解析
            await ParseCustomTopicMessageAsync(
                topic: eventData.MqttTopic,
                rawData: eventData.MqttPayload,
                trackId: eventData.MqttTrackId,
                productKey: eventData.ProductKey,
                deviceName: eventData.DeviceName,
                cancellationToken);

            // 物模型透传 Topic消息解析
            await ParseThingModelPassThroughTopicRawDataAsync(
                topic: eventData.MqttTopic,
                rawData: eventData.MqttPayload,
                trackId: eventData.MqttTrackId,
                productKey: eventData.ProductKey,
                deviceName: eventData.DeviceName,
                cancellationToken);

            _logger.LogInformation(
                "[MQTT消息解析][Consumer:{ConsumerId}][TrackId:{TrackId}] 消息解析完成 | ProductKey:{ProductKey}",
                consumerId,
                eventData.MqttTrackId,
                eventData.ProductKey);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "[MQTT消息解析][Consumer:{ConsumerId}][TrackId:{TrackId}] 任务被取消 | ProductKey:{ProductKey}",
                consumerId,
                eventData.MqttTrackId,
                eventData.ProductKey);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[MQTT消息解析][Consumer:{ConsumerId}][TrackId:{TrackId}] 处理失败 | ProductKey:{ProductKey}",
                consumerId,
                eventData.MqttTrackId,
                eventData.ProductKey);
            throw;
        }
    }

    #region 物模型透传消息解析

    /// <summary>
    /// 将原始（透传）数据转换为协议格式数据（设备上传消息时使用）
    /// </summary>
    /// <returns>协议数据JSON字符串</returns> 
    protected async Task ParseThingModelPassThroughTopicRawDataAsync(
        string topic,
        byte[] rawData,
        string trackId,
        string productKey,
        string deviceName,
        CancellationToken cancellationToken)
    {
        Check.NotNullOrWhiteSpace(topic, nameof(topic));
        Check.NotNull(rawData, nameof(rawData));
        Check.NotNullOrWhiteSpace(trackId, nameof(trackId));
        Check.NotNullOrWhiteSpace(productKey, nameof(productKey));
        Check.NotNullOrWhiteSpace(deviceName, nameof(deviceName));

        var product = await _productCache.GetAsync(productKey);
        if (product == null)
        {
            _logger.LogError(
                "[RawData -> 协议数据][ProductKey:{ProductKey}] 产品缓存不存在",
                productKey);
            return;
        }

        if (product.DataFormat != ProductDataFormat.PassThroughOrCustom &&
            TopicChecker.IsThingModelThroughUpRaw(topic))
        {
            var protocolData = await ConvertRawDataToProtocolDataAsync(
                productKey,
                rawData,
                cancellationToken);

            if (protocolData == null)
            {
                _logger.LogWarning(
                      "[RawData -> 协议数据][设备:{ProductKey}/{DeviceName}] 解析后的数据为null",
                      productKey,
                      deviceName);
                return;
            }

            var eventData = new ThingModelPassThroughTopicRawDataParsedEto(
                trackId: trackId,
                topic: topic,
                alinkJsonData: protocolData,
                productKey: productKey,
                deviceName: deviceName
            );
            await _distributedEventBus.PublishAsync(eventData, onUnitOfWorkComplete: false, useOutbox: true);
        }
    }

    /// <summary>
    /// 将协议格式数据转换为原始（透传）数据（平台下发消息时使用）
    /// </summary>
    /// <returns>原始数据字节数组</returns>
    public async Task ParseThingModelPassThroughTopicProtocolDataAsync(
        string topic,
        string protocolData,
        string trackId,
        string productKey,
        string deviceName,
        CancellationToken cancellationToken)
    {
        Check.NotNullOrWhiteSpace(topic, nameof(topic));
        Check.NotNullOrWhiteSpace(protocolData, nameof(protocolData));
        Check.NotNullOrWhiteSpace(trackId, nameof(trackId));
        Check.NotNullOrWhiteSpace(productKey, nameof(productKey));
        Check.NotNullOrWhiteSpace(deviceName, nameof(deviceName));

        var product = await _productCache.GetAsync(productKey);
        if (product == null)
        {
            _logger.LogError(
                "[RawData -> 协议数据][ProductKey:{ProductKey}] 产品缓存不存在",
                productKey);
            return;
        }

        if (product.DataFormat != ProductDataFormat.PassThroughOrCustom &&
            TopicChecker.IsThingModelThroughDownRaw(topic))
        {
            var rawData = await ConvertProtocolDataToRawDataAsync(
                productKey,
                protocolData,
                cancellationToken);

            if (rawData == null)
            {
                _logger.LogWarning(
                      "[RawData -> 协议数据][设备:{ProductKey}/{DeviceName}] 解析后的数据为null",
                      productKey,
                      deviceName);
                return;
            }

            var eventData = new ThingModelPassThroughTopicProtocolDataParsedEto(
                 trackId: trackId,
                 topic: topic,
                 rawData: rawData,
                 productKey: productKey,
                 deviceName: deviceName
             );
            await _distributedEventBus.PublishAsync(eventData, onUnitOfWorkComplete: false, useOutbox: true);
        }
    }

    /// <summary>
    /// 将原始数据转换为协议格式数据（设备上传消息时使用）
    /// </summary>
    /// <param name="productKey">产品Key</param>
    /// <param name="rawData">原始数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>协议数据JSON字符串</returns>
    protected async Task<string?> ConvertRawDataToProtocolDataAsync(
        string productKey,
        byte[] rawData,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey));
        }

        if (rawData == null)
        {
            throw new ArgumentNullException(nameof(rawData));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger.LogDebug(
                "[RawData -> 协议数据][ProductKey:{ProductKey}] 开始将协议数据转换为原始数据",
                productKey);

            // 1. 获取产品解析器配置 
            var messageParseCache = await _productMessageParserCache.GetAsync(productKey);
            if (messageParseCache == null)
            {
                _logger.LogError(
                    "[RawData -> 协议数据][ProductKey:{ProductKey}] 产品缓存不存在",
                    productKey);
                return null;
            }

            // 2. 检查解析脚本配置
            if (messageParseCache.MessageParserScript == null ||
                messageParseCache.MessageParserScriptLanguage != ProuctMessageParserScriptLanguage.JavaScript_ECMAScrtrip5)
            {
                _logger.LogWarning(
                    "[RawData -> 协议数据][ProductKey:{ProductKey}] 产品未配置有效的JavaScript解析脚本",
                    productKey);
                return null;
            }

            // 3. 原始字节转换为协议数据
            var protocolData = await _thingModelTopicMessageParser.RawDataToProtocolDataAsync(
                rawData,
                messageParseCache.MessageParserScript);

            _logger.LogDebug(
                "[RawData -> 协议数据][ProductKey:{ProductKey}] 转换完成，原始数据长度：{Length}字节",
                productKey,
                protocolData?.Length ?? 0);

            return protocolData;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[RawData -> 协议数据][ProductKey:{ProductKey}] 转换失败",
                productKey);
            throw;
        }
    }

    /// <summary>
    /// 将协议格式数据转换为原始数据（平台下发消息时使用）
    /// </summary>
    /// <param name="productKey">产品Key</param>
    /// <param name="protocolData">协议格式JSON字符串</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>原始数据字节数组</returns>
    public async Task<byte[]?> ConvertProtocolDataToRawDataAsync(
        string productKey,
        string protocolData,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey));
        }

        if (string.IsNullOrWhiteSpace(protocolData))
        {
            throw new ArgumentNullException(nameof(protocolData));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger.LogDebug(
                "[协议数据 -> RawData ][ProductKey:{ProductKey}] 开始将协议数据转换为原始数据",
                productKey);

            // 1. 获取产品解析器配置 
            var messageParseCache = await _productMessageParserCache.GetAsync(productKey);
            if (messageParseCache == null)
            {
                _logger.LogError(
                    "[协议数据 -> RawData ][ProductKey:{ProductKey}] 产品缓存不存在",
                    productKey);
                return null;
            }

            // 2. 检查解析脚本配置
            if (messageParseCache.MessageParserScript == null ||
                messageParseCache.MessageParserScriptLanguage != ProuctMessageParserScriptLanguage.JavaScript_ECMAScrtrip5)
            {
                _logger.LogWarning(
                    "[协议数据 -> RawData ][ProductKey:{ProductKey}] 产品未配置有效的JavaScript解析脚本",
                    productKey);
                return null;
            }

            // 3. 转换协议数据为原始字节
            var rawData = await _thingModelTopicMessageParser.ProtocolDataToRawDataAsync(
                protocolData,
                messageParseCache.MessageParserScript);

            _logger.LogDebug(
                "[协议数据 -> RawData ][ProductKey:{ProductKey}] 转换完成，原始数据长度：{Length}字节",
                productKey,
                rawData?.Length ?? 0);

            return rawData;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[协议数据 -> RawData ][ProductKey:{ProductKey}] 转换失败",
                productKey);
            throw;
        }
    }
    #endregion

    #region 自定义主题消息解析

    /// <summary>
    ///  将原始数据转换为自定义格式数据（设备上传消息时使用）
    /// </summary>
    /// <returns>自定义格式数据JSON字符串</returns>
    protected async Task ParseCustomTopicMessageAsync(
        string topic,
        byte[] rawData,
        string trackId,
        string productKey,
        string deviceName,
        CancellationToken cancellationToken)
    {
        Check.NotNullOrWhiteSpace(topic, nameof(topic));
        Check.NotNull(rawData, nameof(rawData));
        Check.NotNullOrWhiteSpace(trackId, nameof(trackId));
        Check.NotNullOrWhiteSpace(productKey, nameof(productKey));
        Check.NotNullOrWhiteSpace(deviceName, nameof(deviceName));

        if (TopicChecker.IsCustomTopicAndNeedTransformPayload(topic))
        {
            var customJsonData = await ConvertRawDataToCustomDataAsync(
                topic,
                rawData,
                productKey,
                cancellationToken);

            if (customJsonData == null)
            {
                _logger.LogWarning(
                      "[RawData -> 自定义数据][设备:{ProductKey}/{DeviceName}] 解析后的数据为null",
                      productKey,
                      deviceName);
                return;
            }

            var eventData = new CustomTopicRawDataParsedEto(
                 trackId: trackId,
                 topic: topic,
                 jsonData: customJsonData,
                 productKey: productKey,
                 deviceName: deviceName
            );

            await _distributedEventBus.PublishAsync(eventData, onUnitOfWorkComplete: false, useOutbox: true);
        }
    }

    /// <summary>
    /// 将原始数据转换为自定义格式数据（设备上传消息时使用）
    /// </summary>
    /// <param name="productKey">产品Key</param>
    /// <param name="rawData">原始数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>自定义格式数据JSON字符串</returns>
    protected async Task<string?> ConvertRawDataToCustomDataAsync(
        string topic,
        byte[] rawData,
        string productKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productKey))
        {
            throw new ArgumentNullException(nameof(productKey));
        }

        if (rawData == null)
        {
            throw new ArgumentNullException(nameof(rawData));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger.LogDebug(
                "[RawData -> 自定义数据][ProductKey:{ProductKey}] 开始将协议原始转换为自定义数据",
                productKey);

            // 1. 获取产品解析器配置 
            var messageParseCache = await _productMessageParserCache.GetAsync(productKey);
            if (messageParseCache == null)
            {
                _logger.LogError(
                    "[RawData -> 自定义数据][ProductKey:{ProductKey}] 产品缓存不存在",
                    productKey);
                return null;
            }

            // 2. 检查解析脚本配置
            if (messageParseCache.MessageParserScript == null ||
                messageParseCache.MessageParserScriptLanguage != ProuctMessageParserScriptLanguage.JavaScript_ECMAScrtrip5)
            {
                _logger.LogWarning(
                    "[RawData -> 自定义数据][ProductKey:{ProductKey}] 产品未配置有效的JavaScript解析脚本",
                    productKey);
                return null;
            }

            // 3. 原始字节转换为自定义数据
            var protocolData = await _customTopicMessageParser.TransformPayloadAsync(
                topic,
                rawData,
                messageParseCache.MessageParserScript);

            _logger.LogDebug(
                "[RawData -> 自定义数据][ProductKey:{ProductKey}] 转换完成，原始数据长度：{Length}字节",
                productKey,
                protocolData?.Length ?? 0);

            return protocolData;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[RawData -> 自定义数据][ProductKey:{ProductKey}] 转换失败",
                productKey);
            throw;
        }
    }

    #endregion
}
