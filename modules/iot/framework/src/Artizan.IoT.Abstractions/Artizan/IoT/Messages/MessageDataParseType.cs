namespace Artizan.IoT.Messages;

/// <summary>
/// 数据解析类型（适配你的3种解析场景）
/// </summary>
public enum MessageDataParseType
{
    Unknown,
    AlinkJson,       // 标准物模型Alink JSON
    PassThrough,     // 穿透格式（字节流→Alink JSON）
    CustomDataFomat  // 自定义数据格式（非Alink）

    /*
    Unknown,
    Json,
    Binary,
    Text
    */
}