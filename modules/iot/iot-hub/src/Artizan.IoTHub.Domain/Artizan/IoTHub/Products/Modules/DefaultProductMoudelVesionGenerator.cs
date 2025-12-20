using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace Artizan.IoTHub.Products.Modules;

public class DefaultProductMoudelVesionGenerator : IProductMoudelVesionGenerator, ITransientDependency
{
    public DefaultProductMoudelVesionGenerator()
    { 
    }

    public string Create()
    {
        DateTimeOffset timestamp = DateTimeOffset.Now;
        string version = timestamp.ToUnixTimeSeconds().ToString(); // 将时间戳转换为 Unix 时间戳格式的字符串
       
        return version;
    }
}
