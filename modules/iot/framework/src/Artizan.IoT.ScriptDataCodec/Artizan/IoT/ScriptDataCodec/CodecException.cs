using System;

namespace Artizan.IoT.ScriptDataCodec;

/// <summary>
/// 编解码专用异常
/// 设计思路：区分业务异常和系统异常，方便异常捕获与排查
/// </summary>
public class CodecException : Exception
{
    public CodecException(string message) : base(message)
    {
    }

    public CodecException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
