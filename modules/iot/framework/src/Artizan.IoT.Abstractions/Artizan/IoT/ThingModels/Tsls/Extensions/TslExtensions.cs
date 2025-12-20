using Artizan.IoT.ThingModels.Tsls.Serializations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.ThingModels.Tsls.Extensions;

public static class TslExtensions
{
    public static string ToJson(this Tsl tsl, JsonSerializerSettings? settings = null)
        => TslSerializer.SerializeObject(tsl, settings);

    // 移除FromJson（不符合扩展方法语义，建议直接调用TslHelper.Deserialize）
}
