using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Data;

public enum MqttPayloadDataType
{
    /// <summary>
    /// 纯文本：一种文件格式，不包含任何格式控制字符（如加粗、斜体等）或其他非文本元素（如图像、表格等）
    /// </summary>
    PlaintText,
    /// <summary>
    /// 16进制（数组）
    /// </summary>
    Hex,
    /// <summary>
    /// Json 字符串
    /// </summary>
    Json,
    /// <summary>
    /// base64编码
    /// </summary>
    Base64,
    /// <summary>
    /// （Concise Binary Object Representation）是一种简洁的二进制对象表示法，用于在网络中传输数据。
    /// 它是一种高效的数据序列化格式，旨在提高数据传输速度和降低传输数据的大小
    /// </summary>
    CBOR
}
