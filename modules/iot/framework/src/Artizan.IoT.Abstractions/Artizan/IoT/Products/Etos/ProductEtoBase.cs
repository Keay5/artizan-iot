using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Artizan.IoT.Products.Etos;

[Serializable]
public class ProductEtoBase
{
    public Guid ProductId { get; set; }

    public string ProductKey { get; set; }

    public string ProductName { get; set; }
}
