using System;
using System.Linq;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Services;

public static class ServiceMethodGenerator
{
    // 基础模板常量
    public const string BuiltInPropertySet = "thing.service.property.set";
    public const string BuiltInPropertyGet = "thing.service.property.get";
    public const string CustomMethodTemplate = "thing.service.<identifier>";

    /// <summary>
    /// 获取内置属性上报事件方法:thing.service.property.get
    /// </summary>
    public static string  GetBuiltInPropertySetMethod()
    {
        return BuiltInPropertySet;
    }

    /// <summary>
    /// 获取内置属性上报事件方法:thing.service.property.get
    /// </summary>
    public static string GetBuiltInPropertyGetMethod()
    {
        return  BuiltInPropertyGet;
    }

    /// <summary>
    /// 直接传入标识符生成自定义方法（语法糖）
    /// </summary>
    /// <param name="identifier">自定义标识符</param>
    /// <returns></returns>
    public static string GenerateCustomServiceMethod(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("自定义方法标识符不能为空或空白", nameof(identifier));
        }

        // 替换 <identifier> 占位符
        return CustomMethodTemplate.Replace("<identifier>", identifier);
    }
}