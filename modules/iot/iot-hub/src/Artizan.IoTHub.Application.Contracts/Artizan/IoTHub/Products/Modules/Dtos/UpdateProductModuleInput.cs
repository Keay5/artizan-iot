using Volo.Abp.Domain.Entities;

namespace Artizan.IoTHub.Products.Modules.Dtos;

public class UpdateProductModuleInput : IHasConcurrencyStamp
{
    public string Name { get; set; }
    public string Identifier { get; set; }
    public string? Description { get; set; }
    public string ConcurrencyStamp { get; set; }
}
