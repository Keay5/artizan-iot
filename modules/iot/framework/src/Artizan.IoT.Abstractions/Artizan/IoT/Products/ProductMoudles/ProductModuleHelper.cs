using System;

namespace Artizan.IoT.Products.ProductMoudles;

public static class ProductModuleHelper
{
    /// <summary>
    /// 校验是否为默认模块（辅助方法）
    /// </summary>
    public static bool IsDefaultModule(string? moduleIdentifier)
    {
        if (string.IsNullOrWhiteSpace(moduleIdentifier))
        {
            return true;
        }

        return moduleIdentifier.Equals(ProductModuleConsts.DefaultModuleIdentifier, StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryParseModuleFromPropertyKey(string propertyKey, out string? moduleIdentifier, out string purePropertyKey)
    {
        moduleIdentifier = null;
        purePropertyKey = propertyKey;

        if (string.IsNullOrWhiteSpace(propertyKey)) return false;
        var splitIndex = propertyKey.IndexOf(':');
        if (splitIndex <= 0 || splitIndex == propertyKey.Length - 1) return false;

        moduleIdentifier = propertyKey[..splitIndex];
        purePropertyKey = propertyKey[(splitIndex + 1)..];
        return true;
    }
}
