using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Artizan.IoT.Errors;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Artizan.IoT.Results;

/*
 /// 使用示例：
/// 1）.IoTResult 循环套用, errors 重新汇总，最终返回新的IoTResult:
/// protected virtual async Task<IoTResult> VerifyClientCanConnecteAsync(Device device, ...)
/// {
///    List<iotError>? errors = null;
///    IoTResult ioTResult;
///  
///    ioTResult = DeviceManager.VerifyDeviceEnable(device.IsEnable);
///                                |
///                                |- public virtual IoTResult VerifyDeviceEnable(bool isEnable)
///                                   {
///                                       List<iotError>? errors = null;
///                                       if (!isEnable)
///                                       {
///                                           errors ??= new List<iotError>();
///                                           errors.Add(new iotError()
///                                           {   
///                                               Code = IoTErrorCodes.DeviceDisabled // 错误码
///                                           });
///                                       }
///                                       
///                                       return errors?.Count > 0 ? IoTResult.Failed(errors) : IoTResult.Success;
///                                    }
///     ......
///     
///     // errors 重新汇总
///     if (ioTResult.Errors.Any()) 
///     {
///        errors ??= [];
///        errors.AddRange(ioTResult.Errors);
///     }
///     
///     iotResult = DeviceManager.VerifyDevicePassword(productKey, deviceName, device.DeviceSecret, password, timestamp);
///                               |
///                               |- public virtual IoTResult VerifyDevicePassword(string productKey, string deviceName, string deviceSecret, string password, long timestamp)
///                                 {
///                                     List<IoTError>? errors = null;
///                                     var mqttSign = new MqttSign();
///                                     
///                                     mqttSign.Calculate(productKey, deviceName, deviceSecret, timestamp);
///                             
///                                     var parsedPassword = mqttSign.Password;
///                                     if (parsedPassword != password)
///                                     {
///                                         errors ??= new List<IoTError>();
///                                         errors.Add(new IoTError()
///                                         {
///                                             Code = IoTErrorCodes.AuthenticationFailed,
///                                             // IoTError错误的本地化描述
///                                             Description = Localizer[
///                                                             IoTErrorCodes.DeviceAuthenticationFailed,
///                                                             deviceName
///                                                           ]
///                                          });
///                                       }
///                             
///                                      return errors?.Count > 0 ? MqttResult.Failed(errors) : MqttResult.Success;
///                                   }
///     // errors 重新汇总
///     if (iotResult.Errors.Any()) 
///     {
///         errors ??= [];
///         errors.AddRange(iotResult.Errors);
///     }
///     
///     // errors 重新汇总，最终返回新的IoTResult
///     return errors?.Count > 0 ? IoTResult.Failed(errors) : IoTResult.Success;
///   }
///   
/// 2）.其它地方调用:
/// 
///    var ioTResult = await VerifyClientCanConnecteAsync(device, ...);
///    
///    ioTResult.CheckErrors(); // 失败则抛出 IoTResultException 异常
///    或者
///    if (!ioTResult.Succeeded)
///    {
///         .....
///    }
 */
/// <summary>
/// 使用
/// 定义参考：
/// https://github.com/dotnet/aspnetcore/blob/cf6266cccb7f112ff4a40db00704eb773a3c6833/src/Identity/Extensions.Core/src/IdentityResult.cs
/// 使用参考：
/// UserValidator:
/// https://github.com/dotnet/aspnetcore/blob/cf6266cccb7f112ff4a40db00704eb773a3c6833/src/Identity/Extensions.Core/src/UserValidator.cs#L17
/// UserManager.cs->ValidateUserAsync()
/// https://github.com/dotnet/aspnetcore/blob/cf6266cccb7f112ff4a40db00704eb773a3c6833/src/Identity/Extensions.Core/src/UserManager.cs#L2299
/// </summary>
public class IoTResult
{
    private static readonly IoTResult _success = new IoTResult
    {
        Succeeded = true
    };

    private readonly List<IoTError> _errors = new List<IoTError>();

    public bool Succeeded { get; protected set; }

    public IEnumerable<IoTError> Errors => _errors;

    public static IoTResult Success => _success;

    public static IoTResult Failed(params IoTError[] errors)
    {
        var result = new IoTResult { Succeeded = false };
        if (errors != null)
        {
            result._errors.AddRange(errors);
        }
        return result;
    }

    public static IoTResult Failed(List<IoTError>? errors)
    {
        var result = new IoTResult { Succeeded = false };
        if (errors != null)
        {
            result._errors.AddRange(errors);
        }
        return result;
    }

    public override string ToString()
    {
        if (!Succeeded)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} : {1}", "Failed", string.Join(",", Errors.Select((IoTError x) => x.Code).ToList()));
        }

        return "Succeeded";
    }
}
