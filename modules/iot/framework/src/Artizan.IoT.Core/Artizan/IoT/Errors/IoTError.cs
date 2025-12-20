using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Errors;

/// <summary>
/// IoT 错误信息
/// 
/// 用途：参见 <see cref="Results.IoTResult"/>   
/// </summary>
public class IoTError
{
    /// <summary>
    /// 错误码，参见 <see cref="IoTErrorCodes"/>
    /// </summary>
    public string Code { get; set; }
    public string? Description { get; set; }
}
