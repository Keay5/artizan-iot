using Artizan.IoT.Alinks;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtts.Messages.Parsers;

/// <summary>
/// 穿透格式解析器（字节流+JS脚本解析）
/// </summary>
public class PassThroughJavaScriptMqttMessageParser : IMqttMessageParser, ITransientDependency
{
    public static readonly string PassThroughMqttMessageParseJavaScriptIdentifier = "PassThroughMqttMessageParseJavaScript";

    private readonly ILogger<AlinkJsonMqttMessageParser> _logger;
    private readonly IJavaScriptExecutor _javaScriptExecutor; // JS脚本执行器（如Jint）

    public MqttMessageDataParseType SupportType => MqttMessageDataParseType.PassThrough;

    public PassThroughJavaScriptMqttMessageParser(
        ILogger<AlinkJsonMqttMessageParser> logger,
        IJavaScriptExecutor javaScriptExecutor)
    {
        _logger = logger;
        _javaScriptExecutor = javaScriptExecutor;
    }

    public bool Match(MqttMessageContext context)
    {
        // TODO: 根据实际业务逻辑判断是否匹配该解析器
        return context.ProductKey.StartsWith("PASSTHROUGH_");
    }

    public async Task<object> ParseAsync(MqttMessageContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // 1. 从上下文中获取JS解析脚本
            var javaScript =
                context.Extension.Get<string>(PassThroughMqttMessageParseJavaScriptIdentifier);
            // 2. 获取原始Payload字节流
            var payloadSegment = context.RawMessage.PayloadSegment;
            var rawPayload = payloadSegment.Array!.AsSpan(payloadSegment.Offset, payloadSegment.Count).ToArray();
            // 3.执行JS脚本，解析原始Payload中的字节流为Alink Json格式
            var parsedJson = await _javaScriptExecutor.Execute(javaScript, rawPayload);
            // 4. 反序列化Alink JSON为Alink数据对象
            var alinkData = AlinkSerializer.DeserializeObject<AlinkDataModel>(parsedJson);
            // 5. 删除临时存储的JS脚本
            context.Extension.Remove(PassThroughMqttMessageParseJavaScriptIdentifier);

            // 更新解析成功状态
            context.SetParseSuccess(context.ParseType, alinkData, stopwatch.Elapsed);
            _logger.LogInformation("Parse success, TraceId: {TraceId}, ParseType: {ParseType}", context.TraceId, context.ParseType.ToString());

            return Task.FromResult(alinkData);
        }
        catch (Exception ex)
        {
            // 更新解析失败状态
            context.SetParseFailed("Alink JSON parse failed", stopwatch.Elapsed, ex);
            _logger.LogError(ex, "Parse failed, TraceId: {TraceId},ParseType: {ParseType}", context.TraceId, context.ParseType.ToString());

            throw;
        }

    }

}
