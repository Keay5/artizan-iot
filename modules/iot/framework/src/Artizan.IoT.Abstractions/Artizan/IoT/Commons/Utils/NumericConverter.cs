namespace Artizan.IoT.Commons.Utils;
/// <summary>
/// 数值类型安全转换工具类
/// 提供int/float/double类型的安全转换方法，避免溢出和格式错误
/// </summary>
public static class NumericConverter
{
    #region 类型安全转换方法
    /// <summary>
    /// 尝试将任意对象转换为int32类型
    /// </summary>
    /// <param name="value">待转换的值（支持int/float/double/string类型）</param>
    /// <param name="result">转换结果（转换失败时为0）</param>
    /// <returns>转换是否成功（值超出int范围时返回false）</returns>
    public static bool TryConvertToInt32(object value, out int result)
    {
        result = 0;

        // 空值直接返回失败
        if (value is null)
            return false;

        return value switch
        {
            // 直接匹配int类型
            int i => (result = i) == i,

            // float转int：先校验范围避免溢出
            float f when f >= int.MinValue && f <= int.MaxValue =>
                (result = (int)f) == (int)f,

            // double转int：先校验范围避免溢出
            double d when d >= int.MinValue && d <= int.MaxValue =>
                (result = (int)d) == (int)d,

            // 字符串类型：使用TryParse处理（适配系统默认文化格式）
            string s => int.TryParse(s, out result),

            // 其他不支持的类型
            _ => false
        };
    }

    /// <summary>
    /// 尝试将任意对象转换为float类型
    /// </summary>
    /// <param name="value">待转换的值（支持int/float/double/string类型）</param>
    /// <param name="result">转换结果（转换失败时为0）</param>
    /// <returns>转换是否成功（值超出float范围时返回false）</returns>
    public static bool TryConvertToFloat(object value, out float result)
    {
        result = 0;

        if (value is null)
            return false;

        return value switch
        {
            // 直接匹配float类型
            float f => (result = f) == f,

            // int转float：无精度丢失，直接转换
            int i => (result = i) == i,

            // double转float：先校验范围避免溢出
            double d when d >= float.MinValue && d <= float.MaxValue =>
                (result = (float)d) == (float)d,

            // 字符串类型：使用TryParse处理
            string s => float.TryParse(s, out result),

            _ => false
        };
    }

    /// <summary>
    /// 尝试将任意对象转换为double类型
    /// </summary>
    /// <param name="value">待转换的值（支持int/float/double/string类型）</param>
    /// <param name="result">转换结果（转换失败时为0）</param>
    /// <returns>转换是否成功</returns>
    public static bool TryConvertToDouble(object value, out double result)
    {
        result = 0;

        if (value is null)
            return false;

        return value switch
        {
            // 直接匹配double类型
            double d => (result = d) == d,

            // int转double：无精度丢失
            int i => (result = i) == i,

            // float转double：无精度丢失
            float f => (result = f) == f,

            // 字符串类型：使用TryParse处理
            string s => double.TryParse(s, out result),

            _ => false
        };
    } 
    #endregion
}
