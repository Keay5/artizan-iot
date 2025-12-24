using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Messages.PostProcessors;

/// <summary>
/// 插件工厂（负责获取指定协议的插件）
/// 设计模式：工厂模式，封装插件实例化和筛选逻辑。
/// </summary>
public interface IMessagePostProcessorFactory : ISingletonDependency
{
    /// <summary>
    /// 获取指定协议的所有插件
    /// </summary>
    IEnumerable<IMessagePostProcessor<TContext>> GetProcessors<TContext>()
        where TContext : MessageContext;
}
