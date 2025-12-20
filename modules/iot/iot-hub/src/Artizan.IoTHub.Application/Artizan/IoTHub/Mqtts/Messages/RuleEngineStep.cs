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

public class RuleEngineStep : IMqttWorkflowStep
{
    private readonly ILogger<RuleEngineStep> _logger;
    //private readonly IRuleEngine _ruleEngine; // TODO:规则引擎服务
    private readonly AsyncPolicy _policy;

    public string StepIdentifier => "RuleEngine";
    public bool IsEnabled => true;

    public RuleEngineStep(
        ILogger<RuleEngineStep> logger,
        IPolicyRegistry<string> policyRegistry)
    {
        _logger = logger;
        _policy = policyRegistry.Get<AsyncPolicy>(MqttPollyConsts.RuleEnginePolicyName);
    }

    public async Task ExecuteAsync(MqttMessageContext context)
    {
        await _policy.ExecuteAsync(async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                //// 1. 触发规则引擎（判断是否告警）
                //var alertResults = await _ruleEngine.EvaluateAsync(context.ProductKey, context.ParsedData);
                //// 2. 执行告警（短信/邮件/钉钉等）
                //foreach (var alert in alertResults)
                //{
                //    await _alertService.SendAsync(alert.ContactType, alert.Content);
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
                    errorMsg: "RuleEngine excute failed",
                    exception: ex);

                throw; // 抛给Polly处理重试/熔断
            }
        });
    }
}

