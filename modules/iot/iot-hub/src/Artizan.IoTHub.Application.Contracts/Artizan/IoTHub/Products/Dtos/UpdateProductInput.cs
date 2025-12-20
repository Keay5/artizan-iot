using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace Artizan.IoTHub.Products.Dtos;

public class UpdateProductInput : IHasConcurrencyStamp
{
    [Required]
    public string ProductName { get; set; }

    public string Description { get; set; }

    public string ConcurrencyStamp { get; set; }
}
