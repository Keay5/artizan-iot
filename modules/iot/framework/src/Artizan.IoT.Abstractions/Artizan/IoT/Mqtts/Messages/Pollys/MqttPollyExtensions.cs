using Artizan.IoT.Mqtts.Messages.Metrics;
using Artizan.IoT.Mqtts.Messages.Pollys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using System;

public static class MqttPollyExtensions
{
    /// <summary>
    /// 注册MQTT业务的Polly容错策略
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合（链式调用）</returns>
    public static IServiceCollection AddMqttPolicies(this IServiceCollection services)
    {
        // 注册策略注册表（单例），延迟创建以支持日志注入
        services.AddSingleton<IPolicyRegistry<string>>(provider =>
        {
            // 从DI容器获取日志工厂，创建指定类别的日志实例（无需自定义类）
            var logger = provider.GetRequiredService<ILoggerFactory>()
                                 .CreateLogger("Artizan.IoT.Mqtts.Policys");

            var policyRegistry = new PolicyRegistry();

            // ========== 1. 数据转发策略 ==========
            // 舱壁：最多10个并发
            var forwardBulkhead = Policy.BulkheadAsync(10, 0);
            // 熔断：5分钟内失败10次 → 熔断30秒
            var forwardCircuit = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 10,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (ex, breakDuration) =>
                    {
                        logger.LogError(ex, "【数据转发】熔断触发，暂停{Duration}秒", breakDuration.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        logger.LogInformation("【数据转发】熔断重置，恢复正常请求");
                    });
            // 重试：失败后重试3次
            var forwardRetry = Policy
                .Handle<Exception>()
                .RetryAsync(3, (ex, retryCount) =>
                {
                    logger.LogWarning(ex, "【数据转发】第{RetryCount}次重试", retryCount);
                });
            // 组合策略：重试 → 熔断 → 舱壁（外层先执行）
            var forwardPolicy = forwardRetry.WrapAsync(forwardCircuit.WrapAsync(forwardBulkhead));
            policyRegistry.Add(MqttPollyConsts.DataForwardPolicyName, forwardPolicy);

            // ========== 2. 规则引擎策略 ==========
            var ruleBulkhead = Policy.BulkheadAsync(5, 0); // 最多5个并发
            var ruleCircuit = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(20, TimeSpan.FromMinutes(1), // 20次失败熔断1分钟
                    onBreak: (ex, dur) => logger.LogError(ex, "【规则引擎】熔断触发，暂停{Duration}分钟", dur.TotalMinutes),
                    onReset: () => logger.LogInformation("【规则引擎】熔断重置"));
            var ruleRetry = Policy.Handle<Exception>().RetryAsync(2); // 重试2次
            var ruleEnginePolicy = ruleRetry.WrapAsync(ruleCircuit.WrapAsync(ruleBulkhead));
            policyRegistry.Add(MqttPollyConsts.RuleEnginePolicyName, ruleEnginePolicy);

            // ========== 3. 设备联动策略 ==========
            var linkageBulkhead = Policy.BulkheadAsync(8, 0); // 最多8个并发
            var linkageCircuit = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(20), // 5次失败熔断20秒
                    onBreak: (ex, dur) => logger.LogError(ex, "【设备联动】熔断触发，暂停{Duration}秒", dur.TotalSeconds),
                    onReset: () => logger.LogInformation("【设备联动】熔断重置"));
            // 设备联动不重试（幂等性低），仅熔断+舱壁
            var linkagePolicy = linkageCircuit.WrapAsync(linkageBulkhead);
            policyRegistry.Add(MqttPollyConsts.DeviceLinkagePolicyName, linkagePolicy);

            return policyRegistry;
        });

        return services;
    }

    //// 修复：AddMqttMetrics 扩展方法（正确调用AddOpenTelemetry）
    //public static IServiceCollection AddMqttMetrics(this IServiceCollection services)
    //{
    //    // 正确写法：OpenTelemetry 1.6.0+ 注册Metrics的方式
    //    services.AddOpenTelemetry() // 作用于IServiceCollection的AddOpenTelemetry
    //        .WithMetrics(builder =>
    //        {
    //            // 添加自定义Meter（名称必须和创建时一致）
    //            builder.AddMeter("Artizan.IoT.Mqtts")
    //                   // 添加Prometheus导出器（暴露/metrics端点）
    //                   .AddPrometheusExporter(options =>
    //                   {
    //                       options.ScrapeEndpointPath = "/metrics"; // 自定义端点（可选）
    //                   });
    //        });

    //    return services;
    //}
}