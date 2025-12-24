using Artizan.IoT.Mqtts.Messages;
using Artizan.IoT.Mqtts.Messages.Pollys;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoTHub.Mqtts.Messages;

/// <summary>
/// 设备联动步骤
/// </summary>
public class DeviceLinkageStep : IMqttWorkflowStep
{
    private readonly ILogger<DeviceLinkageStep> _logger;

    private readonly AsyncPolicy _policy;
    //private readonly IDeviceLinkageService _linkageService; // TODO:设备联动服务

    public string StepIdentifier => "DeviceLinkage";
    public bool IsEnabled => true;

    public DeviceLinkageStep(
        ILogger<DeviceLinkageStep> logger,
        IPolicyRegistry<string> policyRegistry)
    {
        _logger = logger;
        _policy = policyRegistry.Get<AsyncPolicy>(MqttPollyConsts.DeviceLinkagePolicyName);
    }

    public async Task ExecuteAsync(MqttMessageContext context)
    {
        await _policy.ExecuteAsync(async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // 触发设备联动（如温度>80度，控制水压阀开启）
                //await _linkageService.TriggerLinkageAsync(context.ProductKey, context.DeviceName, context.ParsedData);

                _logger.LogInformation("Device linkage success, TraceId: {TraceId}, ParseType: {ParseType}", context.TraceId, context.ParseType.ToString());

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
                    errorMsg: "Device linkage excute failed",
                    exception: ex);

                throw; // 抛给Polly处理重试/熔断
            }
        });
    }
}
