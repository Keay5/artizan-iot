using Artizan.IoT.ThingModels.Tsls.MetaDatas.DataTypes;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.InputParams;

/// <summary>
/// 通用的输入参数
/// </summary>
public class CommonInputParam : IInputParam
{
    public string Identifier { get; set; }
    public string Name { get; set; }
    public DataType DataType { get; set; }
    public bool Required { get; set; }
}
