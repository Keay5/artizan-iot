using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.ThingModels.Tsls.Builders.Exceptions;

/// <summary>
/// 规格(Specs)参数类型与数据类型不匹配异常
/// </summary>
public class SpecsParamTypeMismatchException : Exception
{
    public SpecsParamTypeMismatchException(DataTypes dataType, Type specsType)
        : base($"数据类型[{dataType}]与规格(Specs)参数类型[{specsType.Name}]不匹配") { }
}
