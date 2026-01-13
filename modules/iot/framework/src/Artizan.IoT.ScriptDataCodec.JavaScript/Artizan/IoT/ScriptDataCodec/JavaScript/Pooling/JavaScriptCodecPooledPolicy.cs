using Microsoft.Extensions.ObjectPool;
using System;

namespace Artizan.IoT.ScriptDataCodec.JavaScript.Pooling;

/// <summary>
/// JS编解码器对象池策略
/// 设计模式：策略模式（实现 IPooledObjectPolicy接口）
/// 设计思路：定义对象池的创建和归还规则，控制实例生命周期
/// 设计考量：
/// 1. Create：创建新的实例
/// 2. Return：校验实例状态，确保归还的实例可用
/// </summary>
public class JavaScriptCodecPooledPolicy : IPooledObjectPolicy<JavaScriptDataCodec>
{
    private readonly string _scriptContent;
    private readonly CodecLogger _codecLogger;
    public int MaxSize { get; } // 暴露池上限

    public JavaScriptCodecPooledPolicy(string scriptContent, CodecLogger codecLogger, int maxSize = 0)
    {
        _scriptContent = scriptContent ?? throw new ArgumentNullException(nameof(scriptContent), "JS脚本内容不能为空");
        _codecLogger = codecLogger ?? throw new ArgumentNullException(nameof(codecLogger), "编解码日志器不能为空");

        MaxSize = maxSize > 0 ? maxSize : Environment.ProcessorCount * 2;
    }

    /// <inheritdoc/>
    public JavaScriptDataCodec Create()
    {
        return new JavaScriptDataCodec(_scriptContent, _codecLogger);
    }

    /// <inheritdoc/>
    public bool Return(JavaScriptDataCodec codec)
    {
        #region 设计缺陷
        // Return 方法  核心职责是判断实例是否 “健康可复用”，而非 “修改实例状态”。如下代码的问题在于混淆了职责边界

        // 归还条件：实例未释放且未被占用
        //if (codec == null || codec.IsDisposed)
        //{
        //    return false;
        //}
        //codec.Release(); // 确保释放占用状态
        //return true;
        #endregion

        // 健康检查：实例未释放则允许归还
        return codec != null && !codec.IsDisposed;
    }
}
