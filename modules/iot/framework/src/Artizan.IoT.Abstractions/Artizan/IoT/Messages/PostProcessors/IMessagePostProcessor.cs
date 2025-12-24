using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Messages.PostProcessors;

/// <summary>
/// 消息后处理插件接口（泛型，支持多协议）
/// 设计理念：
/// - 泛型约束：通过TContext约束协议类型，确保类型安全。
/// - 可配置：支持开关和优先级，灵活控制执行逻辑。
/// 设计模式：
/// - 策略模式：接口定义策略，实现类为具体策略（如缓存、入库）。
/// </summary>
/// <typeparam name="TContext">协议上下文类型（继承自MessageContext）</typeparam>
public interface IMessagePostProcessor<in TContext> 
    where TContext : MessageContext
{
    /// <summary>
    /// 执行优先级（数字越小越先执行）
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 是否启用（通过配置动态开关）
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 执行后处理逻辑
    /// </summary>
    Task ProcessAsync(TContext context, CancellationToken cancellationToken = default);
}



