using System;

namespace Artizan.IoT.ThingModels.Tsls.Builders.Exceptions;

/// <summary>
/// 不支持的规格(Specs)数据对象型异常
/// </summary>
public class UnsupportedSpecsTypeException : Exception
{
    public UnsupportedSpecsTypeException(Type specsType)
        : base($"不支持的规格(Specs)数据对象类型：{specsType.Name}") { }
}
