using Artizan.IoTHub.Products.ProductMoudles;
using JetBrains.Annotations;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace Artizan.IoTHub.Products.Modules.Dtos;

public class ProductModuleDto : FullAuditedEntityDto<Guid>, IHasConcurrencyStamp
{
    [NotNull]
    public virtual Guid ProductId { get; protected set; }

    [NotNull]
    public virtual string Name { get; protected set; }

    [NotNull]
    public virtual string Identifier { get; protected set; }

    [NotNull]
    public virtual bool IsDefault { get; protected set; }

    [NotNull]
    public ProductModuleStatus Status { get; set; }

    [CanBeNull]
    public string? Version { get; set; }

    [NotNull]
    public bool IsCurrentVersion { get; protected set; }

    [CanBeNull]
    public virtual string? Description { get; protected set; }

    [NotNull]
    public virtual string ProductModuleTsl { get; protected set; }

    public string ConcurrencyStamp { get; set; }
}
