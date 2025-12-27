using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Artizan.IoT.Messages.PostProcessors;

/// <summary>
/// 插件工厂实现（基于依赖注入容器获取插件）
/// </summary>
public class MessagePostProcessorFactory : IMessagePostProcessorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public MessagePostProcessorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 从容器中获取指定类型的插件
    /// </summary>
    public IEnumerable<IMessagePostProcessor<TContext>> GetProcessors<TContext>()
        where TContext : MessageContext
    {
        return _serviceProvider.GetServices<IMessagePostProcessor<TContext>>();
    }
}
