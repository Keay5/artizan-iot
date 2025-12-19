using Artizan.IoT.Products.Properties;
using JetBrains.Annotations;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace Artizan.IoTHub.Products.Dtos
{
    public class ProductDto : FullAuditedEntityDto<Guid>, IHasConcurrencyStamp
    {
        [NotNull]
        public string ProductKey { get; set; }

        [NotNull]
        public string ProductSecret { get; set; }

        [NotNull]
        public string ProductName { get; set; }

        [NotNull]
        public ProductCategory Category { get; set; }

        [NotNull]
        public string CategoryName { get; set; }

        [NotNull]
        public ProductNodeTypes NodeType { get; set; }

        [CanBeNull]
        public ProductNetworkingModes? NetworkingMode { get; set; }

        [CanBeNull]
        public ProductAccessGatewayProtocol? AccessGatewayProtocol { get; set; }

        [NotNull]
        public ProductDataFormat DataFormat { get; set; }

        [NotNull]
        public ProductAuthenticationMode AuthenticationMode { get; set; }

        [NotNull]
        public bool IsUsingPrivateCACertificate { get; set; }

        [NotNull]
        public bool IsEnableDynamicRegistration { get; set; }

        [NotNull]
        public ProductStatus ProductStatus { get; set; }

        [CanBeNull]
        public string? Description { get; set; }
        public string ConcurrencyStamp { get; set; }
    }
}
