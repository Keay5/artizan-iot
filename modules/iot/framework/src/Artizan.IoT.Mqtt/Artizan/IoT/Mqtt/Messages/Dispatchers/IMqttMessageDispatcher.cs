using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Artizan.IoT.Mqtt.Messages.Dispatchers;

/// <summary>
/// MQTT消息分发器接口（高并发入口，负责消息调度）
/// 设计理念：
/// - 解耦接收与处理：通过Channel缓冲消息，发布端非阻塞入队，消费端异步处理。
/// - 分区并行：按设备维度分区，单分区有序，多分区并行，提升吞吐量。
/// 设计模式：
/// - 生产者-消费者模式：基于Channel实现，发布端（EnqueueAsync）为生产者，消费端（内部线程）为消费者。
/// </summary>
public interface IMqttMessageDispatcher : IHostedService
{
    /// <summary>
    /// 提交消息到分发器（非阻塞，高并发入口）
    /// </summary>
    /// <param name="context">MQTT消息上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnqueueAsync(MqttMessageContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动分发器（初始化消费者线程和Channel）
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止分发器（优雅关闭，处理剩余消息）
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
