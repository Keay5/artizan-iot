namespace Artizan.IoT.Mqtts.MessageHanlders
{
    /// <summary>
    /// 消息处理结果（Handler → 路由系统反馈载体）
    /// 设计思路：统一返回格式，便于路由系统记录日志、监控执行状态，支持业务层扩展返回数据
    /// </summary>
    public class MqttHandleResult
    {
        /// <summary>处理是否成功</summary>
        public bool IsSuccess { get; set; }

        /// <summary>错误代码（处理失败时必填）</summary>
        /// 命名规范：模块名_错误类型_错误描述（如 "Device_DeviceNotFound"、"Ota_ProtobufParseFail"）
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>错误信息（处理失败时必填，需明确说明原因）</summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>扩展数据（处理成功时可选，可返回给设备的响应数据等）</summary>
        public object? ExtensionData { get; set; }

        /// <summary>创建成功结果（静态工厂方法，简化调用）</summary>
        /// <param name="extensionData">扩展数据（可选）</param>
        public static MqttHandleResult Success(object? extensionData = null)
        {
            return new MqttHandleResult
            {
                IsSuccess = true,
                ExtensionData = extensionData
            };
        }

        /// <summary>创建失败结果（静态工厂方法，简化调用）</summary>
        /// <param name="errorCode">错误代码</param>
        /// <param name="errorMessage">错误信息</param>
        public static MqttHandleResult Fail(string errorCode, string errorMessage)
        {
            return new MqttHandleResult
            {
                IsSuccess = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
    }
}
