using Artizan.IoT.ThingModels.Tsls.MetaDatas.Enums;
using Artizan.IoT.ThingModels.Tsls.MetaDatas.InputParams;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.ThingModels.Tsls.MetaDatas.Services;

public class Service
{
    public string Identifier { get; set; }
    public string Name { get; set; }
    public string? Desc { get; set; }
    public bool Required { get; set; }
    public ServiceCallTypes CallType { get; set; }
    public string Method { get; set; }

    // 关键：使用接口类型+集合转换器
    [JsonConverter(typeof(InputParamListConverter))]
    public List<IInputParam>? InputData { get; set; } 

    public List<OutputParam>? OutputData { get; set; }
}
