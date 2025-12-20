using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artizan.IoT.Alinks.DataObjects.Commons;

/// <summary>
/// 校验结果封装
/// 【设计考量】：统一校验返回格式，简化错误处理逻辑
/// </summary>
public class ValidateResult
{
    public bool IsValid { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;

    private ValidateResult()
    {
    }

    public static ValidateResult Success()
    {
        return new ValidateResult { IsValid = true };
    }

    public static ValidateResult Failed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentNullException(nameof(errorMessage), "错误信息不能为空");
        }
        return new ValidateResult { IsValid = false, ErrorMessage = errorMessage };
    }
}
