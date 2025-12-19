using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtts.Topics.Permissions;

/// <summary>
/// MQTT Topic权限规则提供器接口（抽象数据源，支持配置/数据库/缓存）
/// 【设计】：接口隔离，可替换不同数据源（静态配置/动态数据库/配置中心）
/// 【设计理念】：
/// 1. 动态加载权限规则：支持从数据库或配置文件动态获取规则，便于运维调整；
/// 2. 支持多种规则源：可扩展实现类，从不同数据源加载规则（如SQL、NoSQL、配置文件等）；
/// 3. 缓存优化：可选缓存机制，提升高并发环境下的规则查询性能；
/// 4. 线程安全：确保多线程环境下的规则加载和访问安全。
/// </summary>
public interface IMqttTopicPermissionRuleProvider : ISingletonDependency
{
    /// <summary>
    /// 获取所有有效权限规则（建议本地缓存，定时刷新）
    /// </summary>
    Task<List<MqttTopicPermissionRule>> GetRulesAsync();

    /// <summary>
    /// 刷新规则（支持动态更新）
    /// </summary>
    Task RefreshRulesAsync();
}
