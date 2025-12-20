namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Events;

public static class EventMethodGenerator
{
    public const string BuiltInPropertyPost = "thing.service.property.post";
    public const string CustomMethodPostTemplate = "thing.service.<identifier>.post";

    /// <summary>
    /// 获取内置属性上报事件方法:thing.service.property.post
    /// </summary>
    public static (string identifier, string name, string method, string desc) GetBuiltInPropertyPostMethod()
    {
        var identifier = "post";
        var name = "post";
        var desc = "属性上报";
        return (identifier, name, BuiltInPropertyPost, desc);
    }

    /// <summary>
    /// 生成自定义POST类型事件方法（thing.event.<identifier>.post）
    /// </summary>
    /// <param name="identifier"></param>
    public static string GenerateCustomEventPostMethod(string identifier)
    {
        return CustomMethodPostTemplate.Replace("<identifier>", identifier);
    }
}

