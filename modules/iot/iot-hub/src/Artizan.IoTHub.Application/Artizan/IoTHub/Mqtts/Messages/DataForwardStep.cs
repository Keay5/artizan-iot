using Artizan.IoT.Mqtts.Messages;
using Artizan.IoT.Mqtts.Messages.Pollys;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using Polly;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoTHub.Mqtts.Messages;

/// <summary>
/// 数据转发步骤
/// </summary>
public class DataForwardStep : IMqttWorkflowStep
{
    //private readonly IMqttClientFactory _mqttClientFactory;
    //private readonly IForwardConfigRepository _forwardConfigRepo;

    private readonly ILogger<DataForwardStep> _logger;
    private readonly AsyncPolicy _policy; // Polly容错策略

    public string StepIdentifier => "DataForward";
    public bool IsEnabled => true; // 可从配置读取

    public DataForwardStep(
    ILogger<DataForwardStep> logger,
    IPolicyRegistry<string> policyRegistry)
    {
        _logger = logger;
        // 从策略注册表获取该步骤的容错策略（熔断+重试+舱壁）
        _policy = policyRegistry.Get<AsyncPolicy>(MqttPollyConsts.DataForwardPolicyName);
    }

    public async Task ExecuteAsync(MqttMessageContext context)
    {
        // 用Polly包装执行，失败触发熔断/重试
        await _policy.ExecuteAsync(async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                //// 1. 获取转发配置（本平台/其他平台Topic）
                //var forwardConfigs = await _forwardConfigRepo.GetByProductKeyAsync(context.ProductKey);
                //// 2. 遍历转发
                //foreach (var config in forwardConfigs)
                //{
                //    var client = _mqttClientFactory.GetClient(config.PlatformType);
                //    var message = new MqttApplicationMessage
                //    {
                //        Topic = config.TargetTopic,
                //        PayloadSegment = JsonSerializer.SerializeToUtf8Bytes(context.ParsedData),
                //        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce
                //    };
                //    await client.PublishAsync(message, CancellationToken.None);
                //}

                _logger.LogInformation("Data forward success, TraceId: {TraceId}, ParseType: {ParseType}", context.TraceId, context.ParseType.ToString());

                // 更新步骤成功状态
                context.UpdateStepResult(
                    stepName: StepIdentifier,
                    isSuccess: true,
                    elapsed: stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                // 更新步骤失败状态
                context.UpdateStepResult(
                    stepName: StepIdentifier,
                    isSuccess: false,
                    elapsed: stopwatch.Elapsed,
                    errorMsg: "Data forward failed",
                    exception: ex);

                throw; // 抛给Polly处理重试/熔断
            }
        });
    }
}
