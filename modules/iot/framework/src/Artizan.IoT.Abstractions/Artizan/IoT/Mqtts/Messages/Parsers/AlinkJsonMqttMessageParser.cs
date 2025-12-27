using Artizan.IoT.Alinks.DataObjects.MessageCommunications;
using Artizan.IoT.Alinks.Serializers;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtts.Messages.Parsers;

/// <summary>
/// 物模型Alink JSON解析器
/// </summary>
public class AlinkJsonMqttMessageParser : IMqttMessageParser, ITransientDependency
{
    private readonly ILogger<AlinkJsonMqttMessageParser> _logger;
    public AlinkJsonMqttMessageParser(ILogger<AlinkJsonMqttMessageParser> logger)
    {
        _logger = logger;
    }

    public MqttMessageDataParseType SupportType => MqttMessageDataParseType.AlinkJson;

    public bool Match(MqttMessageContext context)
    {
        // TODO:可从配置/产品属性判断：比如ProductKey以“ALINK_”开头用该解析器
        return context.ProductKey.StartsWith("ALINK_");
    }

    public async Task<object> ParseAsync(MqttMessageContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // 解析原始Payload为Alink格式
            var payloadSegment = context.RawMessage.PayloadSegment;
            var rawPayload = payloadSegment.Array!.AsSpan(payloadSegment.Offset, payloadSegment.Count).ToArray();
            var rawJsonData = Encoding.UTF8.GetString(rawPayload);
            var alinkData = AlinkSerializer.DeserializeObject<PropertyPostRequest>(rawJsonData);

            // 更新解析成功状态
            context.SetParseSuccess(context.ParseType, alinkData, stopwatch.Elapsed);
            _logger.LogInformation("Parse success, TraceId: {TraceId}, ParseType: {ParseType}", context.TraceId, context.ParseType.ToString());

            return Task.FromResult(alinkData);
        }
        catch (Exception ex)
        {
            // 更新解析失败状态
            context.SetParseFailed("Alink JSON parse failed", stopwatch.Elapsed, ex);
            _logger.LogError(ex, "Parse failed, TraceId: {TraceId}", context.TraceId);

            throw;
        }
    }

}
