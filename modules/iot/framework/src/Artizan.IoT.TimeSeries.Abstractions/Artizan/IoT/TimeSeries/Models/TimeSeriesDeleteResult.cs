namespace Artizan.IoT.TimeSeries.Models;

/// <summary>
/// 删除操作结果
/// 设计思路：删除操作需明确是否成功及影响条数
/// 设计考量：部分时序库不返回删除条数，用-1标识未知
/// </summary>
public class TimeSeriesDeleteResult
{
    /// <summary>
    /// 是否删除成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 成功删除的条数（-1表示无法获取）
    /// </summary>
    public int DeletedCount { get; set; } = -1;

    /// <summary>
    /// 错误消息（失败时非空）
    /// </summary>
    public string? ErrorMessage { get; set; }
}
