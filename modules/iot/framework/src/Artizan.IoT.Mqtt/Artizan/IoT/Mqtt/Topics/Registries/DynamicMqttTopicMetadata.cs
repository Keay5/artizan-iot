using Artizan.IoT.Mqtt.MessageHanlders;
using Microsoft.Extensions.DependencyInjection;
using System;
using Volo.Abp;

namespace Artizan.IoT.Mqtt.Topics.Registries;

/// <summary>
/// MQTT动态Topic路由元数据（路由规则的核心载体）
/// 【设计思想】：值对象模式 - 仅存储路由核心静态数据，无业务逻辑，确保不可变、线程安全
/// 【设计理念】：单一职责 - 仅承载路由模板、Handler类型、优先级等元数据，不参与任何业务逻辑
/// 设计理念：
/// 1. 封装核心属性：Topic模板+Handler工厂+优先级，不可变设计（构造后不允许修改）；
/// 2. CreateHandler：延迟创建Handler实例，避免提前初始化依赖；
/// 3. 与特性对齐：属性与MqttTopicRouteAttribute一一对应，便于自动扫描转换。
/// </summary>
public class DynamicMqttTopicMetadata
{
    /// <summary>
    /// MQTT Topic模板（支持${productKey}/${deviceName}格式占位符）,参见<see cref="MqttTopicSpeciesConsts"/>
    /// </summary>
    public string TopicTemplate { get; }

    /// <summary>
    /// 路由优先级（数值越大优先级越高，高优先级模板优先匹配）
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// 消息处理器类型（必须是IMqttMessageHandler的具体实现类，非抽象/非接口）
    /// </summary>
    public Type HandlerType { get; }

    /// <summary>
    /// 构造函数（强制校验入参，确保元数据合法性）
    /// 【设计理念】：防御式编程 - 入参严格校验，避免非法元数据进入系统
    /// </summary>
    /// <param name="topicTemplate">Topic模板（非空）</param>
    /// <param name="handlerType">消息处理器类型（非抽象、非接口、实现IMqttMessageHandler）</param>
    /// <param name="priority">优先级（默认0）</param>
    /// <exception cref="ArgumentNullException">入参为空时抛出</exception>
    /// <exception cref="ArgumentException">Handler类型不合法时抛出</exception>
    public DynamicMqttTopicMetadata(string topicTemplate, Type handlerType, int priority = 0)
    {
        // 严格校验Topic模板非空
        Check.NotNullOrWhiteSpace(topicTemplate, nameof(topicTemplate));
        // 严格校验Handler类型非空
        Check.NotNull(handlerType, nameof(handlerType));
        if (handlerType.IsAbstract || handlerType.IsInterface)
        {
            throw new ArgumentNullException(nameof(handlerType.FullName), $"{handlerType.FullName} 必须是具体类");
        }
        if (!typeof(IMqttTopicMessageHandler).IsAssignableFrom(handlerType))
        {
            throw new ArgumentNullException(nameof(handlerType.FullName), $"{handlerType.FullName} 必须实现IMqttMessageHandler接口");
        }

        TopicTemplate = topicTemplate;
        HandlerType = handlerType;
        Priority = priority;
    }

    /// <summary>
    /// 创建消息处理器实例（调用时传入有效ServiceProvider，避免缓存已释放的作用域）
    /// 【设计思想】：依赖注入 - 基于传入的根容器创建子作用域，解析Handler实例
    /// 【设计理念】：资源隔离 - 每次创建Handler都新建子作用域，避免作用域污染/释放异常
    /// </summary>
    /// <param name="serviceProvider">全局根ServiceProvider（ABP注入，永不释放）</param>
    /// <returns>IMqttMessageHandler具体实现实例</returns>
    /// <exception cref="InvalidOperationException">Handler创建失败时抛出</exception>
    public IMqttTopicMessageHandler CreateHandler(IServiceProvider serviceProvider)
    {
        Check.NotNull(serviceProvider, nameof(serviceProvider));

        try
        {
            /*
            为什么 using var scope = serviceProvider.CreateScope(); ，每次创建Handler都新建DI作用域，避免复用已释放的LifetimeScope
            核心原因：
                - 避免根容器污染：根容器是全局单例，直接解析范围型服务（如 DbContext）会导致其生命周期变成 “伪单例”，引发并发 / 数据错乱；
                - 作用域隔离：每个 Handler 的依赖（如数据库连接）需独立生命周期，子作用域确保每次调用 Handler 都有干净的依赖实例；
                - 资源及时释放：using自动销毁子作用域，回收 DbContext、网络连接等资源，避免内存泄漏；
                - 彻底杜绝释放异常：子作用域是基于根容器新建的全新作用域，完全规避 “复用已释放 LifetimeScope” 的报错。

            综上所述，这种设计确保了符合依赖注入最佳实践，保障应用稳定性和资源管理的高效性。
            （本质：符合.NET DI “范围型服务” 的设计规范，适配 Autofac/ABP 的生命周期管理逻辑）
           */
            // 每次创建Handler都新建DI作用域，避免复用已释放的LifetimeScope
            // 扩展点：可配置作用域创建策略（如全局作用域、请求作用域）
            // 核心：基于根容器创建独立子作用域，避免复用已释放的LifetimeScope
            using var scope = serviceProvider.CreateScope();

            // 从子作用域解析Handler实例（而非根容器，符合DI范围型服务生命周期规范）
            // 扩展点：可添加Handler实例缓存策略（需注意线程安全和资源释放）
            var handler = (IMqttTopicMessageHandler)scope.ServiceProvider.GetRequiredService(HandlerType);

            return handler;
        }
        catch (ObjectDisposedException ex)
        {
            throw new InvalidOperationException(
                $"[创建MQTT Handler] 失败：作用域已释放 | Handler类型：{HandlerType.FullName} | 模板：{TopicTemplate}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"[创建MQTT Handler] 失败 | Handler类型：{HandlerType.FullName} | 模板：{TopicTemplate}", ex);
        }
    }
}
