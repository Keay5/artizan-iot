using Artizan.IoT.Mqtt.Topics.Registries;
using Artizan.IoT.Mqtt.Topics.Routes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Reflection;

namespace Artizan.IoT.Mqtt.MessageHanlders;

public class MqttTopicMessageHandlerRegister
{
    /// <summary>
    /// 扫描程序集并注册MQTT主题路由
    /// </summary>
    public static void ScanAssembliesAndRegisterMqttTopicMessageHandlers(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<MqttTopicMessageHandlerRegister>>();
        var registry = serviceProvider.GetRequiredService<IDynamicMqttTopicRegistry>();
        var assemblyFinder = serviceProvider.GetRequiredService<IAssemblyFinder>();

        logger.LogInformation("===== IoT MQTT路由模块：开始自动扫描Handler =====");

        try
        {
            // 扫描ABP框架管理的所有业务程序集
            foreach (var assembly in assemblyFinder.Assemblies)
            {
                try
                {
                    // 过滤系统程序集，提升扫描效率
                    if (IsSystemAssembly(assembly))
                    {
                        logger.LogDebug("跳过系统程序集：{AssemblyName}", assembly.GetName().Name);
                        continue;
                    }

                    logger.LogDebug("开始扫描程序集：{AssemblyName}", assembly.GetName().Name);

                    // 查找符合条件的MQTT Handler：
                    // 1. 实现IMqttMessageHandler接口
                    // 2. 非接口、非抽象类（仅具体实现类可被实例化）
                    // 3. 标记MqttTopicRoute特性（路由规则）
                    // 4. 可被DI容器解析（已注册具体类）
                    var handlerTypes = assembly.GetTypes()
                        .Where(t =>
                            typeof(IMqttTopicMessageHandler).IsAssignableFrom(t)
                            && !t.IsInterface         // 过滤接口
                            && !t.IsAbstract          // 过滤抽象类
                            && t.GetCustomAttributes<MqttTopicRouteAttribute>(false).Any() // 仅扫描标记路由特性的类
                            && IsTypeResolvable(serviceProvider, t) // 确保DI已注册该具体类
                        );

                    // 遍历Handler并注册路由规则
                    foreach (var handlerType in handlerTypes)
                    {
                        var routeAttrs = handlerType.GetCustomAttributes<MqttTopicRouteAttribute>(false)
                            .Where(attr => attr.Enabled)
                            .ToList();

                        foreach (var attr in routeAttrs)
                        {
                            registry.Register(new DynamicMqttTopicMetadata(
                                topicTemplate: attr.TopicTemplate,
                                handlerType: handlerType,
                                priority: attr.Priority
                            ));

                            // 记录注册日志，便于问题排查
                            logger.LogInformation(
                                "自动注册MQTT路由 | 程序集：{AssemblyName} | Handler：{HandlerType} | Topic模板：{TopicTemplate} | 优先级：{Priority}",
                                assembly.GetName().Name, handlerType.FullName, attr.TopicTemplate, attr.Priority);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 单个程序集扫描异常不中断整体流程，仅记录日志
                    logger.LogError(ex, "扫描程序集{AssemblyName}时发生异常，已跳过该程序集", assembly.GetName().Name);
                }
            }

            // 输出注册结果，便于运维监控
            var registeredCount = registry.GetRegisteredTemplates().Count;
            logger.LogInformation("===== IoT MQTT路由模块：自动扫描完成，共注册[{RegisteredCount}]条路由规则 =====", registeredCount);
        }
        catch (Exception ex)
        {
            // 全局异常捕获，终止模块初始化并抛出框架级异常（触发告警）
            logger.LogError(ex, "IoT MQTT路由模块扫描注册Handler失败，模块初始化终止");
            throw new AbpException("MQTT路由核心模块初始化失败，无法处理MQTT消息", ex);
        }
    }

    /// <summary>
    /// 辅助方法：过滤系统程序集（提升扫描效率，避免扫描Microsoft/System开头的程序集）
    /// </summary>
    /// <summary>
    /// 辅助方法：判断是否为系统程序集（过滤非业务程序集）
    /// </summary>
    private static bool IsSystemAssembly(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name;
        return assemblyName.StartsWith("Microsoft.")
               || assemblyName.StartsWith("System.")
               || assemblyName.StartsWith("Volo.Abp.")
               || assemblyName.StartsWith("netstandard")
               || assemblyName.StartsWith("mscorlib")
               || assemblyName.StartsWith("Newtonsoft.")
               || assemblyName.StartsWith("MQTTnet.");
    }

    private static bool IsTypeResolvable(IServiceProvider serviceProvider, Type type)
    {
        try
        {
            // 尝试解析类型，无异常则表示可解析
            serviceProvider.GetRequiredService(type);
            return true;
        }
        catch (InvalidOperationException)
        {
            // 类型未注册到DI容器，返回false
            return false;
        }
        catch (Exception)
        {
            // 其他异常（如构造函数参数缺失），视为不可解析
            return false;
        }
    }
}
