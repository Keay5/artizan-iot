using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Artizan.IoT.Results;
using Artizan.IoT.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;
using Volo.Abp;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Localization;

namespace Artizan.IoT.Exceptions;

/// <summary>
/// IoTResult 中的异常
/// 
/// 使用参见 <see cref="IoTResultExtensions.CheckErrors(IoTResult)"/>
/// </summary>

[Serializable]
internal class IoTResultException : BusinessException, ILocalizeErrorMessage
{
    public IoTResult IoTTResult { get; }

    public IoTResultException([NotNull] IoTResult iotResult)
        : base(
            code: $"{iotResult.Errors.First().Code}",
            message: iotResult.Errors?.Select(err => err.Description ?? string.Empty).JoinAsString(", ") ?? string.Empty
        )
    {
        IoTTResult = Check.NotNull(iotResult, nameof(iotResult));
    }

    // public IoTResultException(SerializationInfo serializationInfo, StreamingContext context)
    //     : base(serializationInfo, context)
    // {

    // }

    public virtual string LocalizeMessage(LocalizationContext context)
    {
        var localizer = context.LocalizerFactory.Create<IoTResource>();

        SetData(localizer);

        return IoTTResult.LocalizeErrors(localizer);
    }

    protected virtual void SetData(IStringLocalizer localizer)
    {
        var values = IoTTResult.GetValuesFromErrorMessage(localizer);

        for (var index = 0; index < values.Length; index++)
        {
            Data[index.ToString()] = values[index];
        }
    }
}
