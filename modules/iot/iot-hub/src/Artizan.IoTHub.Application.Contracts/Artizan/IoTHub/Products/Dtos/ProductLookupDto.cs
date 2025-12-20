using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoTHub.Products.Dtos;

public class ProductLookupDto
{
    public Guid Id { get; set; }
    public string ProductKey { get; set; }
    public string ProductName { get; set; }
}
