using System.Collections.Generic;
using System.Linq;

namespace Artizan.IoT.BatchProcessing.Health;

/// <summary>
/// 健康检查器扩展方法（便于使用）
/// </summary>
public static class PartitionHealthCheckerExtensions
{
    /// <summary>
    /// 快速检查分区是否健康（非警告/异常/熔断）
    /// </summary>
    public static bool IsPartitionHealthy(this PartitionHealthChecker checker, string partitionKey)
    {
        var status = checker.GetHealthStatus(partitionKey);
        return status != null && status.HealthLevel == PartitionHealthLevel.Healthy;
    }

    /// <summary>
    /// 获取所有异常/熔断的分区（用于告警）
    /// </summary>
    public static List<PartitionHealthStatus> GetUnhealthyPartitions(this PartitionHealthChecker checker)
    {
        return checker.GetAllPartitionStatuses()
            .Where(s => s.HealthLevel is PartitionHealthLevel.Abnormal or PartitionHealthLevel.CircuitBroken)
            .ToList();
    }
}