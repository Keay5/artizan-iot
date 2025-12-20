using Artizan.IoT.Abstractions.Commons.Tracing;
using Artizan.IoT.Alinks.DataObjects;
using Artizan.IoT.Alinks.DataObjects.MessageCommunications;
using Artizan.IoT.Alinks.Serializers;
using Artizan.IoT.Mqtts.MessageHanlders;
using Artizan.IoT.Mqtts.Messages;
using Artizan.IoT.Mqtts.Messages.Parsers;
using Artizan.IoT.Mqtts.Topics.Routes;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Json;

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
[MqttTopicRoute("/sys/${productKey}/${deviceName}/thing/event/property/post", Priority = 10)]
public class ThingPropertyReportMessageHandler : SafeMqttMessageHandler, ITransientDependency
{
    private readonly ILogger<ThingPropertyReportMessageHandler> _logger;
    private readonly ICurrentTraceIdAccessor _currentTraceIdAccessor;
    //private readonly IDevicePropertyRepository _propertyRepository; // 假设的业务仓储
    private readonly IJsonSerializer _jsonSerializer; // ABP内置JSON序列化器

    // ABP依赖注入：自动注入日志、仓储等服务
    public ThingPropertyReportMessageHandler(
        ILogger<ThingPropertyReportMessageHandler> logger,
        ICurrentTraceIdAccessor currentTraceIdAccessor,

        //IDevicePropertyRepository propertyRepository,
        IJsonSerializer jsonSerializer)
    {
        _logger = logger;
        _currentTraceIdAccessor = currentTraceIdAccessor;
       // _propertyRepository = propertyRepository;
        _jsonSerializer = jsonSerializer;
    }

    /// <summary>
    /// 核心业务逻辑：属性上报处理
    /// 流程：解析Payload→验证设备→保存属性→更新上下文状态
    /// </summary>
    public override async Task HandleAsync(MqttMessageContext messageContext)
    {
        var traceId = messageContext.TraceId;
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
            var productKey = messageContext.ProductKey;
            var deviceName = messageContext.DeviceName;
            var payloadBytes = messageContext.RawMessage.PayloadSegment.Array;
            var payloadString = messageContext.RawMessage.PayloadString;

            // 2. 业务校验：核心字段不能为空
            if (string.IsNullOrWhiteSpace(productKey) || string.IsNullOrWhiteSpace(deviceName))
            {
                var errorMsg = $"核心字段缺失：productKey={productKey}，deviceName={deviceName}";
                _logger.LogError("[{TraceId}][物模型-属性上报][业务校验] 失败：{ErrorMsg}", traceId, errorMsg);
                messageContext.SetParseFailed(errorMsg, TimeSpan.Zero);
                return;
            }

            var parseStopwatch = Stopwatch.StartNew();

            // 3. 解析Payload（业务自主解析，路由系统不干预）
            PropertyPostRequest? alinkData = null;
            try
            {
                //alinkData = _jsonSerializer.Deserialize<PropertyPostRequest>(payloadString);
                alinkData = JsonSerializer.Deserialize<PropertyPostRequest>(payloadString);

                //var validateResult =  await alinkData.Validate();

                parseStopwatch.Stop();
            }
            catch (Exception ex)
            {
                parseStopwatch.Stop();
                var errorMsg = "Payload「数据格式」有误，解析失败";
                _logger.LogError(ex, "[{TraceId}][物模型-属性上报][数据解析] 失败：{ErrorMsg} | 摘要：{PayloadSummary}",
                    traceId, errorMsg, messageContext.RawMessage.GetPayloadSummary(100));
                messageContext.SetParseFailed(errorMsg, parseStopwatch.Elapsed, ex);
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
            messageContext.SetParseSuccess(MqttMessageDataParseType.AlinkJson, alinkData, parseStopwatch.Elapsed);

            // 5. 业务逻辑：保存属性到数据库
            var businessStopwatch = Stopwatch.StartNew();

            // TODO: 消息分发：缓存、入时序库、设备联动、规则引擎

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

            await Task.Delay(500); // 异步延迟，非阻塞（推荐）

            businessStopwatch.Stop();

            // 6. 记录业务步骤成功
            messageContext.UpdateStepResult(stepName, true, businessStopwatch.Elapsed);

            _logger.LogInformation(
                "[{TraceId}][物模型-属性上报]处理成功 | productKey={productKey} | deviceName={deviceName} | 属性数：{Count} | 总耗时：{TotalMs}ms",
                traceId, productKey, deviceName, alinkData.Params.Count,
                (parseStopwatch.Elapsed + businessStopwatch.Elapsed).TotalMilliseconds);
        }
        catch (BusinessException ex)
        {
            // 业务异常：记录日志，不向上抛出
            _logger.LogError(ex, "[{TraceId}][物模型-属性上报]业务异常：{Message}", traceId, ex.Message);
            messageContext.UpdateStepResult(stepName, false, TimeSpan.Zero, ex.Message);
        }
        catch (Exception ex)
        {
            // 系统异常：记录日志并向上抛出（触发监控告警）
            _logger.LogError(ex, "[{TraceId}][物模型-属性上报]系统异常：{Message}", traceId, ex.Message);
            messageContext.UpdateStepResult(stepName, false, TimeSpan.Zero, "系统异常", ex);
            throw;
        }
    }
}
