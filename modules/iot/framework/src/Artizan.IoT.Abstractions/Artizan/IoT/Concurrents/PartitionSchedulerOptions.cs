using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Concurrents
{
    /// <summary>
    /// 高并发分区调度器配置选项类
    /// 【设计思路】：封装所有调度器核心配置参数，统一管理可配置项，支持appsettings.json绑定，便于运维调整
    /// 包含分区数、单分区并发度、任务超时时间等核心参数，适配不同业务场景动态调整
    /// </summary>
    public class PartitionSchedulerOptions
    {
        /// <summary>
        /// 分区总数（核心配置，默认128）
        /// 适配建议：
        /// 数据处理（IO 密集型）：CPU核心数×4（假设16核，设置为64），适配海量设备，海量设备（10 万 +），每个设备每秒上报 1~5 条数据；
        /// 10万级主体（设备/用户）设256，
        /// 百万级设512，平衡队列竞争与资源占用：
        /// 默认128（适配中小规模业务，可按需调整）
        /// </summary>
        public int PartitionCount { get; set; } = 128;

        /// <summary>
        /// 单个分区内最大并发数（核心并发参数,默认1，串行执行）, 默认1（适配多数有序场景）
        /// 适配建议：
        ///     IO 密集型任务（如 HTTP 请求、数据库操作）：可设为 2~4（任务大部分时间等待 IO，多并发可提升利用率,利用等待时间提升并发）。
        ///     CPU 密集型任务（如数据计算、序列化）：建议设为 1~2（避免同一分区内多任务争抢 CPU，导致上下文切换）,避免线程切换开销。
        /// 风险点：若设置过大，可能导致单个分区占用过多线程 / 资源，引发服务器整体负载过高（如 CPU 100%、数据库连接池耗尽）。
        /// </summary>
        public int MaxParallelPerPartition { get; set; } = 1;

        /// <summary>
        /// 单个任务超时时间（异常防护参数,默认500ms）
        /// 【设计思路】：可根据业务耗时调整,避免异常任务（如死循环、网络超时）长期占用分区资源，阻塞后续任务，默认500ms（可根据业务耗时调整）
        /// 设置原则：超时时间需略大于业务任务的正常执行时间（如正常执行 1 秒，超时设为 3 秒），避免 “正常任务被误判超时”。 
        /// 避坑点： 
        ///     超时时间过短：频繁触发超时异常，业务任务重复执行（若有重试逻辑），加剧系统压力。 
        ///     超时时间过长：阻塞分区信号量释放，导致后续任务长时间等待，甚至引发 “信号量泄漏”（极端情况）。
        /// 差异化处理：若不同业务任务的超时需求不同，可扩展调度器支持 “任务级超时配置”（而非全局配置）。
        /// </summary>
        public TimeSpan TaskTimeout { get; set; } = TimeSpan.FromMilliseconds(500);
    }
}
