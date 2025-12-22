using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using Volo.Abp.Text.Formatting;
using Volo.Abp;
using Artizan.IoT.Errors;
using Artizan.IoT.Exceptions;

namespace Artizan.IoT.Results;

public static class IoTResultExtensions
{
    // private static readonly Dictionary<string, string> IdentityStrings = new Dictionary<string, string>();

    static IoTResultExtensions()
    {
        //var identityResourceManager = new ResourceManager("Microsoft.Extensions.Identity.Core.Resources", typeof(UserManager<>).Assembly);
        //var resourceSet = identityResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false);
        //if (resourceSet == null)
        //{
        //    throw new AbpException("Can't get the ResourceSet of Identity.");
        //}

        //var iterator = resourceSet.GetEnumerator();
        //while (true)
        //{
        //    if (!iterator.MoveNext())
        //    {
        //        break;
        //    }

        //    var key = iterator.Key?.ToString();
        //    var value = iterator.Value?.ToString();
        //    if (key != null && value != null)
        //    {
        //        IdentityStrings.Add(key, value);
        //    }
        //}

        //if (!IdentityStrings.Any())
        //{
        //    throw new AbpException("ResourceSet values of Identity is empty.");
        //}
    }

    public static void CheckErrors(this IoTResult iotResult)
    {
        if (iotResult.Succeeded)
        {
            return;
        }

        if (iotResult.Errors == null)
        {
            throw new ArgumentException("iotResult.Errors should not be null.");
        }

        throw new IoTResultException(iotResult);
    }

    public static string[] GetValuesFromErrorMessage(this IoTResult iotResult, IStringLocalizer localizer)
    {
        if (iotResult.Succeeded)
        {
            throw new ArgumentException(
                "iotResult.Succeeded should be false in order to get values from error.");
        }

        if (iotResult.Errors == null)
        {
            throw new ArgumentException("iotResult.Errors should not be null.");
        }

        return Array.Empty<string>();
    }

    public static string LocalizeErrors(this IoTResult iotResult, IStringLocalizer localizer)
    {
        if (iotResult.Succeeded)
        {
            throw new ArgumentException("iotResult.Succeeded should be false in order to localize errors.");
        }

        if (iotResult.Errors == null)
        {
            throw new ArgumentException("iotResult.Errors should not be null.");
        }

        return iotResult.Errors.Select(err => err.LocalizeErrorMessage(localizer)).JoinAsString(", ");
    }

    public static string LocalizeErrorMessage(this IoTError error, IStringLocalizer localizer)
    {
        var key = $"{error.Code}";

        var localizedString = localizer[key];

        //if (!localizedString.ResourceNotFound)
        //{
        //    var englishString = IdentityStrings.GetOrDefault(error.Code);
        //    if (englishString != null)
        //    {
        //        if (FormattedStringValueExtracter.IsMatch(error.Description, englishString, out var values))
        //        {
        //            return string.Format(localizedString.Value, values.Cast<object>().ToArray());
        //        }
        //    }
        //}

        if (!localizedString.ResourceNotFound)
        {
            return $@"{localizedString.Value}{error.Description ?? ""}";
        }

        return localizer[IoTErrorCodes.DefaultError];
    }
}


