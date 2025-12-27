using Artizan.IoT.Messages;
using Artizan.IoT.Messages.PostProcessors;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Artizan.IoTHub.Mqtts.Messages.PostProcessors.Caches;

/// <summary>
/// MQTT消息缓存处理器接口（核心契约）
/// 【接口职责】
/// 1. 处理MQTT解析后的设备数据，批量写入缓存；
/// 2. 托管服务生命周期（启动/停止）；
/// 3. 资源释放；
/// 4. 消息处理器管道（优先级/启用状态/处理逻辑）。
/// </summary>
public interface IMqttCacheMessagePostProcessor<in TContext> :
    IMessagePostProcessor<TContext>,  // 复用自定义消息处理器接口
    IHostedService,         // 复用ABP托管服务接口
    IDisposable             // 复用资源释放接口
    where TContext : MessageContext
{
    /// <summary>
    /// 手动触发批量写入（扩展方法，按需添加）
    /// 【场景】运维手动触发、缓存故障恢复后补写
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    Task TriggerBatchWriteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动分发器（初始化消费者线程和Channel）
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止分发器（优雅关闭，处理剩余消息）
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
