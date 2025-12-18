using System;

namespace Artizan.IoTHub.Products.Modules.Dtos;

public class CreateProductModuleInput
{
    public Guid ProductId { get; set; }
    public string Name { get; set; }
    public string Identifier { get; set; }
    public string? Description { get; set; }
}
