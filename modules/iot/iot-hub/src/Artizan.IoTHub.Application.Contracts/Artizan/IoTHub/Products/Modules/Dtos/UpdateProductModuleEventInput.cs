using Artizan.IoT.ThingModels.Tsls.DataObjects;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities;

namespace Artizan.IoTHub.Products.Modules.Dtos;

public class UpdateProductModuleEventInput :  IHasConcurrencyStamp
{
    public string Name { get; set; }
    public string Identifier { get; set; }
    public EventTypes EventType { get; set; }
    public bool Required { get; set; }

    public List<OutputParamDo> OutputDatas { get; set; }

    public string? Description { get; set; }
    public string ConcurrencyStamp { get; set; }
}