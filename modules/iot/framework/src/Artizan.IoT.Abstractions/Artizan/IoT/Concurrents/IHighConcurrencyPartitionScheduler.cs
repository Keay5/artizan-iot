using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Concurrents;

/// <summary>
/// 泛型高并发分区调度器接口
/// 【设计定位】：抽象通用化的分区任务调度能力，解耦并发控制与业务实现，适配任意任务结果类型与分区场景
/// 【核心设计思路】：
/// 1. 泛型化适配：通过T泛型参数兼容任意任务返回结果，脱离具体业务数据类型绑定
/// 2. 分区核心抽象：以partitionKey为统一分区依据，适配设备ID、用户ID等各类场景，实现同键任务串行、异键任务并行
/// 3. 职责边界清晰：仅暴露任务调度与队列监控核心能力，屏蔽底层分区计算、并发控制细节，降低业务使用成本
/// 4. 异步优先：全接口异步签名，契合高并发场景下的非阻塞执行需求，提升系统吞吐
/// </summary>
/// <typeparam name="T">任务执行结果类型，支持任意引用/值类型</typeparam>
public interface IHighConcurrencyPartitionScheduler<T>
{
    /// <summary>
    /// 提交泛型任务到对应分区执行
    /// 【核心逻辑】：根据partitionKey路由到指定分区，任务入队后触发异步消费，保证同分区任务有序执行、不同分区并行执行
    /// </summary>
    /// <param name="partitionKey">分区键（任务分区唯一依据，需保证同一业务主体键值唯一，如设备ID、用户ID）</param>
    /// <param name="taskFunc">泛型任务委托（业务层封装的异步任务，返回T类型结果）</param>
    /// <returns>任务执行后的T类型结果，异步等待获取</returns>
    Task<T> ScheduleAsync(string partitionKey, Func<Task<T>> taskFunc);

    /// <summary>
    /// 获取各分区任务队列状态
    /// 【设计目的】：提供运维监控能力，实时感知各分区任务堆积情况，便于优化分区数与并发参数
    /// </summary>
    /// <returns>分区索引-队列长度字典，key为分区编号，value为对应分区待执行任务数</returns>
    Dictionary<int, int> GetPartitionQueueStats();
}
