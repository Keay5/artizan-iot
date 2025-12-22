using Newtonsoft.Json;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.InputParams;

/// <summary>
/// 仅包含标识符的参数（用于service的get方法的InputData）
/// </summary>
[JsonConverter(typeof(PropertyIdentifierParamConverter))] 
public class PropertyIdentifierParam : IInputParam
{
    public string Identifier { get; set; }
}
