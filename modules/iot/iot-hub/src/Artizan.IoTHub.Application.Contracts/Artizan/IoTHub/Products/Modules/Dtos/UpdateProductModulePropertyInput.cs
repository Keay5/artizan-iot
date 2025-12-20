using Artizan.IoT.ThingModels.Tsls.DataObjects;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using Volo.Abp.Domain.Entities;

namespace Artizan.IoTHub.Products.Modules.Dtos;


public class UpdateProductModulePropertyInput : DataTypeBaseDo, IHasConcurrencyStamp
{
    public string Name { get; set; }
    public string Identifier { get; set; }
    public AccessModes AccessMode { get; set; }
    public bool Required { get; set; }
    public string? Description { get; set; }
    public string ConcurrencyStamp { get; set; }
}