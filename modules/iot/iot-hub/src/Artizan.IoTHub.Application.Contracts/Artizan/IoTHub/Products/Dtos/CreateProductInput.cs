
using Artizan.IoTHub.Products.Properties;

namespace Artizan.IoTHub.Products.Dtos;

public class CreateProductInput
{
    public string ProductName { get; set; }
    public ProductCategory Category { get; set; }
    public string CategoryName { get; set; }
    public ProductNodeTypes NodeType { get; set; }
    public ProductNetworkingModes? NetworkingMode { get; set; }
    public ProductAccessGatewayProtocol? AccessGatewayProtocol { get; set; }
    public ProductDataFormat DataFormat { get; set; }
    public ProductAuthenticationMode AuthenticationMode { get; set; }
    public bool IsEnableDynamicRegistration { get; set; }
    public bool IsUsingPrivateCACertificate { get; set; }
    public string? Description { get; set; }
}
