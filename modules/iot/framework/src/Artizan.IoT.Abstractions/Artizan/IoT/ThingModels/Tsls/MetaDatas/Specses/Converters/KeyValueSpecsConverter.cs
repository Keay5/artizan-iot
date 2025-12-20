using Artizan.IoT.ThingModels.Tsls.DataObjects.Specs;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Specses.Converters;

/// <summary>
/// 键值类型（bool/enum）的转换策略
/// </summary>
public class KeyValueSpecsConverter : ISpecsConverter<KeyValueSpecsDo, KeyValueSpecs>
{
    public KeyValueSpecs Convert(KeyValueSpecsDo specsDo)
    {
        var specs = new KeyValueSpecs();
        foreach (var kv in specsDo.Values)
        {
            specs.SetValue(kv.Key, kv.Value);
        }
        return specs;
    }

    public ISpecs Convert(ISpecsDo param)
    {
        return Convert((KeyValueSpecsDo)param);
    }
}
