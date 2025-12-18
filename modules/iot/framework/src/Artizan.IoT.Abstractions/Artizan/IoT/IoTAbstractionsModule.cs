using Artizan.IoT.Localization;
using Artizan.IoT.Mqtt.Signs;
using Artizan.IoT.Mqtts.MessageHanlders;
using Artizan.IoT.Mqtts.Topics.Registrys;
using Artizan.IoT.Mqtts.Topics.Routes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Modularity;
using Volo.Abp.Reflection;
using Volo.Abp.Threading;
using Volo.Abp.VirtualFileSystem;

namespace Artizan.IoT;

[DependsOn(
    typeof(IoTCoreModule),
    typeof(AbpCachingModule),
    typeof(AbpVirtualFileSystemModule),
    typeof(AbpThreadingModule)
)]
public class IoTAbstractionsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<IoTAbstractionsModule>();
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<IoTResource>()
                .AddVirtualJson("Artizan/IoT/Localization/Abstractions");
        });

        Configure<AbpExceptionLocalizationOptions>(options =>
        {
            options.MapCodeNamespace(IoTErrorCodes.Namespace, typeof(IoTResource));
        });

        ConfigureMqttServices(context);
    }

    private static void ConfigureMqttServices(ServiceConfigurationContext context)
    {
        // 注册MQTT Polly策略
        context.Services.AddMqttPolicies();

        // 1. 注册配置选项（支持通过appsettings.json配置）
        context.Services.Configure<MqttRouterOptions>(context.Configuration.GetSection("IoTMqtt:Router"));

        // 2. 注册核心服务（按ABP生命周期管理）
        //context.Services.AddSingleton<IDynamicMqttTopicRegistry, DynamicMqttTopicRegistry>(); // 注册中心（单例，全局唯一）
        //context.Services.AddSingleton<TopicTemplateParser>(); // 模板解析器（单例，无状态）

        // 3. 注册后台服务：定时清理解析缓存
        context.Services.AddHostedService<MqttTopicRouteCacheCleanupBackgroundService>();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        // 将全局的 LazyServiceProvider 赋值给静态类
        MqttSignHelper.LazyServiceProvider = context.ServiceProvider.GetRequiredService<IAbpLazyServiceProvider>();
    }

    /// <summary>
    /// 模块初始化（自动扫描Handler并注册路由）
    /// 执行时机：应用启动时，所有模块配置完成后
    /// </summary>
    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        #region 可删除，但保留供参考
        //var logger = context.ServiceProvider.GetRequiredService<ILogger<IoTAbstractionsModule>>();
        //var registry = context.ServiceProvider.GetRequiredService<IDynamicMqttTopicRegistry>();
        //// 注意：IAssemblyFinder的正确注入方式（ABP 8.x+ 标准）
        //var assemblyFinder = context.ServiceProvider.GetRequiredService<IAssemblyFinder>();
        //var serviceProvider = context.ServiceProvider;

        //logger.LogInformation("===== IoT MQTT路由模块：开始自动扫描Handler =====");

        //try
        //{
        //    // ========== 核心修复：替换为GetAllAssemblies() ==========
        //    // 扫描ABP框架管理的所有程序集（跨模块/跨项目）
        //    var assemblies = AppDomain.CurrentDomain.GetAssemblies()
        //        .Where(a => !a.IsDynamic) // 排除动态程序集
        //        .Where(a => !IsSystemAssembly(a)); // 过滤系统程序集

        //    foreach (var assembly in assemblies)
        //    {
        //        try
        //        {
        //            // 过滤无效程序集（避免系统程序集，提升扫描效率）
        //            if (IsSystemAssembly(assembly))
        //            {
        //                continue;
        //            }

        //            // 查找符合条件的MQTT Handler：
        //            // 1. 实现IMqttMessageHandler接口 2. 非接口/抽象类 3. 标记路由特性 4. 可被DI解析
        //            var handlerTypes = assembly.GetTypes()
        //                .Where(t => typeof(IMqttMessageHandler).IsAssignableFrom(t)
        //                            && !t.IsInterface
        //                            && !t.IsAbstract
        //                            && t.GetCustomAttributes<MqttTopicRouteAttribute>(false).Any()
        //                            && IsTypeResolvable(serviceProvider, t));

        //            foreach (var handlerType in handlerTypes)
        //            {
        //                var routeAttrs = handlerType.GetCustomAttributes<MqttTopicRouteAttribute>(false)
        //                    .Where(attr => attr.Enabled)
        //                    .ToList();

        //                foreach (var attr in routeAttrs)
        //                {
        //                    registry.Register(new DynamicMqttTopicMetadata(
        //                        topicTemplate: attr.TopicTemplate,
        //                        handlerFactory: () => (IMqttMessageHandler)serviceProvider.GetRequiredService(handlerType),
        //                        priority: attr.Priority
        //                    ));

        //                    logger.LogInformation(
        //                        "自动注册路由 | 程序集：{AssemblyName} | Handler：{HandlerType} | Topic：{TopicTemplate} | 优先级：{Priority}",
        //                        assembly.GetName().Name, handlerType.FullName, attr.TopicTemplate, attr.Priority);
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            logger.LogError(ex, "扫描程序集{AssemblyName}时异常，跳过该程序集", assembly.GetName().Name);
        //        }
        //    }

        //    var registeredCount = registry.GetRegisteredTemplates().Count;
        //    logger.LogInformation("===== IoT MQTT路由模块：自动扫描完成，共注册{Count}条路由 =====", registeredCount);
        //}
        //catch (Exception ex)
        //{
        //    logger.LogError(ex, "IoT MQTT路由模块初始化失败");
        //    throw new AbpException("MQTT路由模块启动失败，影响MQTT消息处理", ex);
        //}

        ////await base.OnApplicationInitializationAsync(context); 
        #endregion

        await ScanAndRegisterMqttTopicHandlers(context);
    }

    /// <summary>
    /// 扫描所有程序集并自动注册MQTT Topic路由处理器
    /// 命名逻辑：Scan（扫描）+ Register（注册）+ MqttTopicHandlers（核心对象），语义完整无歧义
    /// </summary>
    private async Task ScanAndRegisterMqttTopicHandlers(ApplicationInitializationContext context)
    {
        var logger = context.ServiceProvider.GetRequiredService<ILogger<IoTAbstractionsModule>>();
        var registry = context.ServiceProvider.GetRequiredService<IDynamicMqttTopicRegistry>();
        var assemblyFinder = context.ServiceProvider.GetRequiredService<IAssemblyFinder>();
        var serviceProvider = context.ServiceProvider;

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
                    // 1. 实现IMqttMessageHandler接口（或继承抽象基类SafeMqttMessageHandler）
                    // 2. 非接口、非抽象类（仅具体实现类可被实例化）
                    // 3. 标记MqttTopicRoute特性（路由规则）
                    // 4. 可被DI容器解析（已注册具体类）
                    var handlerTypes = assembly.GetTypes()
                        .Where(t =>
                            typeof(IMqttMessageHandler).IsAssignableFrom(t) 
                            && !t.IsInterface         // 过滤接口
                            && !t.IsAbstract          // 过滤抽象类（关键：排除SafeMqttMessageHandler）
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

        // 异步方法标记：无异步操作时返回CompletedTask（符合async/await规范）
        await Task.CompletedTask;
    }

    /// <summary>
    /// 辅助方法：过滤系统程序集（提升扫描效率，避免扫描Microsoft/System开头的程序集）
    /// </summary>
    /// <summary>
    /// 辅助方法：判断是否为系统程序集（过滤非业务程序集）
    /// </summary>
    private bool IsSystemAssembly(Assembly assembly)
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

    private bool IsTypeResolvable(IServiceProvider serviceProvider, Type type)
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