using Artizan.IoT.Alinks.Results;

namespace Artizan.IoT.Alinks.Parsers;

/// <summary>
/// AlinkJson解析器接口（接口隔离）
/// 【设计考量】：便于单元测试Mock，且可扩展不同版本的解析器（如AlinkJson V2）。
/// </summary>
public interface IAlinkJsonParser
{
    AlinkHandleResult Parse(string rawMessage, string rawTopic);
}
