using Artizan.IoT.Results;

namespace Artizan.IoTHub.Things;

/// <summary>
/// 发布资源回调监听器接口（对应 Java IPublishResourceListener）
/// </summary>
public interface IPublishResourceListener
{
    /// <summary>
    /// 发布成功回调
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <param name="data">返回数据</param>
    void OnSuccess(string resourceId, object data);

    /// <summary>
    /// 发布失败回调
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <param name="error">错误信息</param>
    void OnError(string resourceId, IoTResult error);
}
