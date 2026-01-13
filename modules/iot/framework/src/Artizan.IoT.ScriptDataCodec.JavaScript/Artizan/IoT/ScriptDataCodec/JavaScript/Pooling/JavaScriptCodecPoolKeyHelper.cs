using System;
using System.Text;

namespace Artizan.IoT.ScriptDataCodec.JavaScript.Pooling
{
    /// <summary>
    /// JS编解码器对象池键辅助类
    /// 设计思路：
    /// 1. 遵循「单一职责原则」，集中管理池键的生成、解析、字段提取逻辑，避免散落在业务代码中；
    /// 2. 采用「结构化字段标识」设计，兼容字段值含_/:等特殊字符，保证池键生成/解析的双向一致性；
    /// 3. 提供「全量解析+单一字段获取」双接口，适配不同调用场景，兼顾灵活性与易用性；
    /// 设计理念：
    /// - 约定优于配置：统一池键格式规范，减少团队协作的沟通成本；
    /// - 鲁棒性优先：兼容字段值含特殊字符，避免边界场景解析失败；
    /// - 可维护性：集中管理字段标识、前缀等常量，修改时无需全局替换硬编码；
    /// 设计模式：
    /// - 工具类模式（静态辅助类）：无状态、纯函数设计，适合通用功能封装；
    /// - 开闭原则：新增字段/规则时仅需扩展常量，核心解析逻辑无需大幅修改。
    /// </summary>
    //[System.Runtime.CompilerServices.CompilerGenerated]
    //[System.Runtime.CompilerServices.BeforeFieldInit] // 禁用静态构造函数，提升高并发初始化性能
    public static class JavaScriptCodecPoolKeyHelper
    {
        #region 常量定义（集中管理，便于维护）
        /// <summary>
        /// JS编解码器池键前缀（全局统一配置）
        /// 设计考量：
        /// 1. 末尾带下划线，与业务字段形成视觉分隔，提升日志/监控中的可读性；
        /// 2. 符合IoT平台「组件标识_业务内容」的命名规范，便于跨模块识别；
        /// 3. 避免与其他版本组件（如JavaScriptDataCodecV2）的前缀冲突；
        /// 4. 前缀采用完整组件名，明确标识池类型，便于问题排查。
        /// </summary>
        public const string PoolKeyPrefix = "JavaScriptDataCodec_";

        /// <summary>
        /// 字段标识常量（带分隔符，用于精准定位字段位置）
        /// 设计考量：
        /// 1. 标识格式为「字段缩写+冒号」，与字段值形成明确分隔；
        /// 2. 分隔符使用冒号，避免与下划线冲突（下划线用于字段间分隔）；
        /// 3. 集中管理标识常量，修改时仅需调整此处，无需修改解析逻辑。
        /// </summary>
        private const string ProductKeyFlag = "pk:";
        private const string ScriptNameFlag = "sn:";
        private const string VersionFlag = "v:";

        /// <summary>
        /// 字段间分隔符（用于分隔不同字段）
        /// 设计考量：
        /// 1. 使用下划线作为字段间分隔，符合通用命名规范；
        /// 2. 与字段标识的冒号形成双重区分，避免解析歧义。
        /// </summary>
        private const string FieldSeparator = "_";
        #endregion

        #region 核心方法：生成标准化池键
        /// <summary>
        /// 生成标准化的JS编解码器池键
        /// 池键格式：{PoolKeyPrefix}{ProductKeyFlag}{productKey}[{FieldSeparator}{ScriptNameFlag}{scriptName}][{FieldSeparator}{VersionFlag}{scriptVersion}]
        /// 示例1（全字段）：JavaScriptDataCodec_pk:prod_001:test_sn:script:name_v1_v:v2.0:beta
        /// 示例2（仅产品Key）：JavaScriptDataCodec_pk:product001
        /// 示例3（产品Key+版本）：JavaScriptDataCodec_pk:product001_v:1.0.2
        /// </summary>
        /// <param name="productKey">产品唯一标识（必填，支持含_/:等特殊字符）</param>
        /// <param name="scriptName">脚本名称（可选，支持含_/:等特殊字符）</param>
        /// <param name="scriptVersion">脚本版本号（可选，支持含_/:等特殊字符）</param>
        /// <returns>标准化池键</returns>
        /// <exception cref="ArgumentNullException">产品Key为空时抛出</exception>
        /// <exception cref="ArgumentException">产品Key仅含空白字符时抛出</exception>
        public static string GeneratePoolKey(string productKey, string? scriptName = null, string? scriptVersion = null)
        {
            // 严格校验必填参数，提前拦截无效输入
            if (productKey == null)
            {
                throw new ArgumentNullException(nameof(productKey), "产品Key不能为空");
            }

            if (string.IsNullOrWhiteSpace(productKey))
            {
                throw new ArgumentException("产品Key不能仅包含空白字符", nameof(productKey));
            }

            // 使用StringBuilder提升高并发下的拼接性能，减少GC压力
            var keyBuilder = new StringBuilder(PoolKeyPrefix);

            // 拼接必选字段：产品Key
            keyBuilder.Append(ProductKeyFlag);
            keyBuilder.Append(productKey);

            // 拼接可选字段：脚本名（非空时拼接，避免冗余字符）
            if (!string.IsNullOrWhiteSpace(scriptName))
            {
                keyBuilder.Append(FieldSeparator);
                keyBuilder.Append(ScriptNameFlag);
                keyBuilder.Append(scriptName);
            }

            // 拼接可选字段：版本号（非空时拼接，避免冗余字符）
            if (!string.IsNullOrWhiteSpace(scriptVersion))
            {
                keyBuilder.Append(FieldSeparator);
                keyBuilder.Append(VersionFlag);
                keyBuilder.Append(scriptVersion);
            }

            var poolKey = keyBuilder.ToString();

            // 日志提示（调试用，生产环境可替换为ILogger）
            System.Diagnostics.Debug.WriteLine($"生成JS编解码器池键：{poolKey}");

            return poolKey;
        }
        #endregion

        #region 核心方法：解析池键（兼容特殊字符，带详细步骤注释）
        /// <summary>
        /// 解析JS编解码器池键，提取核心字段
        /// 设计思路：
        /// 1. 放弃传统的字符拆分方式，改为「基于字段标识的精准定位」，兼容字段值含_/:等特殊字符；
        /// 2. 分步骤定位每个字段的起始/结束位置，确保字段值完整提取；
        /// 3. 严格校验池键格式，抛出明确的异常信息，便于问题排查；
        /// 4. 空字段返回null，与生成逻辑保持语义一致。
        /// </summary>
        /// <param name="poolKey">待解析的池键</param>
        /// <returns>包含产品Key、脚本名、脚本版本的元组（可选字段无值时返回null）</returns>
        /// <exception cref="ArgumentException">池键为空、无前缀、格式不合法时抛出</exception>
        public static (string productKey, string? scriptName, string? scriptVersion) ParsePoolKey(string poolKey)
        {
            #region Step 1: 基础校验（非空+前缀校验）
            // 1.1 校验池键非空
            // 示例：空字符串/Null → 抛出异常："池键不能为空"
            // 示例："   " → 触发此校验（string.IsNullOrEmpty包含全空白场景）
            if (string.IsNullOrEmpty(poolKey))
            {
                throw new ArgumentException("池键不能为空", nameof(poolKey));
            }

            // 1.2 校验池键前缀（确保是JS编解码器的池键）
            // 示例1：合法前缀 → JavaScriptDataCodec_pk:product001 → 校验通过
            // 示例2：非法前缀 → PythonDataCodec_pk:product001 → 抛出异常："池键格式不合法，必须以[JavaScriptDataCodec_]为前缀"
            // 示例3：前缀拼接错误 → JavaScriptDataCodepk:product001 → 抛出异常（无前缀下划线）
            if (!poolKey.StartsWith(PoolKeyPrefix, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"池键格式不合法，必须以[{PoolKeyPrefix}]为前缀 | 传入池键：{poolKey}",
                    nameof(poolKey));
            }

            // 1.3 移除前缀，获取核心内容（仅保留字段标识和值）
            // 示例1：完整池键 → JavaScriptDataCodec_pk:product_001:test_sn:scriptA_v:1.0 → 核心内容：pk:product_001:test_sn:scriptA_v:1.0
            // 示例2：仅产品Key → JavaScriptDataCodec_pk:product001 → 核心内容：pk:product001
            // 示例3：前缀后无内容 → JavaScriptDataCodec_ → 核心内容为空 → 抛出异常
            var coreContent = poolKey.Substring(PoolKeyPrefix.Length);
            if (string.IsNullOrEmpty(coreContent))
            {
                throw new ArgumentException(
                    $"池键格式不合法，移除前缀后无有效内容 | 传入池键：{poolKey}",
                    nameof(poolKey));
            }
            #endregion

            #region Step 2: 解析产品Key（必选字段，兼容特殊字符）
            // 2.1 定位产品Key标识的起始位置（必须在核心内容开头）
            // 示例1：合法核心内容 → pk:product001 → 标识位置=0 → 校验通过
            // 示例2：标识位置错误 → sn:scriptA_pk:product001 → 标识位置≠0 → 抛出异常
            var productKeyFlagPos = coreContent.IndexOf(ProductKeyFlag, StringComparison.Ordinal);
            if (productKeyFlagPos != 0)
            {
                throw new ArgumentException(
                    $"池键格式不合法，核心内容必须以[{ProductKeyFlag}]开头 | 核心内容：{coreContent} | 传入池键：{poolKey}",
                    nameof(poolKey));
            }

            // 2.2 计算产品Key值的起始位置（跳过"pk:"标识）
            // 示例：核心内容=pk:product_001:test → 起始位置=3（"pk:"长度为3）
            var productKeyValueStart = productKeyFlagPos + ProductKeyFlag.Length;

            // 2.3 定义完整字段标识（字段分隔符+标识，用于定位字段边界）
            // 设计考量：用"_sn:"/"_v:"作为完整标识，避免与字段值中的"sn:"/"v:"混淆
            var scriptNameFullFlag = $"{FieldSeparator}{ScriptNameFlag}";
            var versionFullFlag = $"{FieldSeparator}{VersionFlag}";

            // 2.4 定位后续字段标识的位置（用于确定产品Key的结束位置）
            // 示例1：完整核心内容 → pk:product_001:test_sn:scriptA_v:1.0 → scriptNameFlagPos=16，versionFlagPos=26
            // 示例2：仅产品Key+版本 → pk:product001_v:1.0 → scriptNameFlagPos=-1，versionFlagPos=11
            // 示例3：仅产品Key → pk:product001 → scriptNameFlagPos=-1，versionFlagPos=-1
            var scriptNameFlagPos = coreContent.IndexOf(scriptNameFullFlag, StringComparison.Ordinal);
            var versionFlagPos = coreContent.IndexOf(versionFullFlag, StringComparison.Ordinal);

            // 2.5 确定产品Key的结束位置（核心逻辑：兼容多字段/单字段场景）
            // 示例1：同时有脚本名和版本 → 取较小索引（scriptNameFlagPos）→ 结束位置=16
            // 示例2：仅版本 → 结束位置=versionFlagPos=11
            // 示例3：仅产品Key → 结束位置=核心内容长度=9
            int productKeyEndIndex;
            if (scriptNameFlagPos > 0 && versionFlagPos > 0)
            {
                productKeyEndIndex = Math.Min(scriptNameFlagPos, versionFlagPos);
            }
            else if (scriptNameFlagPos > 0)
            {
                productKeyEndIndex = scriptNameFlagPos;
            }
            else if (versionFlagPos > 0)
            {
                productKeyEndIndex = versionFlagPos;
            }
            else
            {
                productKeyEndIndex = coreContent.Length;
            }

            // 2.6 截取产品Key值（兼容含_/:等特殊字符）
            // 示例1：核心内容=pk:product_001:test_sn:scriptA → 截取3到16位 → product_001:test
            // 示例2：核心内容=pk:product001 → 截取3到9位 → product001
            // 示例3：产品Key为空 → pk:_sn:scriptA → 截取后为空 → 抛出异常
            var productKey = coreContent.Substring(productKeyValueStart, productKeyEndIndex - productKeyValueStart);
            if (string.IsNullOrWhiteSpace(productKey))
            {
                throw new ArgumentException(
                    $"池键格式不合法，产品Key不能为空 | 传入池键：{poolKey}",
                    nameof(poolKey));
            }
            #endregion

            #region Step 3: 解析脚本名（可选字段，兼容特殊字符）
            // 3.1 仅当存在脚本名标识时解析（无标识则返回null）
            // 示例1：有脚本名 → coreContent=pk:product001_sn:script:name_v1 → scriptNameFlagPos=11 → 解析
            // 示例2：无脚本名 → coreContent=pk:product001_v:1.0 → scriptNameFlagPos=-1 → 返回null
            string? scriptName = null;
            if (scriptNameFlagPos > 0)
            {
                // 3.2 计算脚本名值的起始位置（跳过"_sn:"完整标识）
                // 示例：scriptNameFlagPos=11 → 起始位置=11+4=15（"_sn:"长度为4）
                var scriptNameValueStart = scriptNameFlagPos + scriptNameFullFlag.Length;

                // 3.3 确定脚本名的结束位置（到版本号标识或核心内容末尾）
                // 示例1：有版本号 → versionFlagPos=25 → 结束位置=25
                // 示例2：无版本号 → 结束位置=核心内容长度=21
                int scriptNameEndIndex = versionFlagPos > 0 ? versionFlagPos : coreContent.Length;

                // 3.4 截取脚本名值（兼容含_/:等特殊字符）
                // 示例：核心内容=pk:product001_sn:script:name_v1_v:1.0 → 截取15到25位 → script:name_v1
                var scriptNameValue = coreContent.Substring(scriptNameValueStart, scriptNameEndIndex - scriptNameValueStart);

                // 3.5 空字符串转为null（与生成逻辑保持语义一致）
                // 示例：脚本名为空 → _sn: → 截取后为空 → scriptName=null
                scriptName = string.IsNullOrWhiteSpace(scriptNameValue) ? null : scriptNameValue;
            }
            #endregion

            #region Step 4: 解析版本号（可选字段，兼容特殊字符）
            // 4.1 仅当存在版本号标识时解析（无标识则返回null）
            // 示例1：有版本号 → coreContent=pk:product001_v:v2.0:beta → versionFlagPos=11 → 解析
            // 示例2：无版本号 → coreContent=pk:product001_sn:scriptA → versionFlagPos=-1 → 返回null
            string? scriptVersion = null;
            if (versionFlagPos > 0)
            {
                // 4.2 计算版本号值的起始位置（跳过"_v:"完整标识）
                // 示例：versionFlagPos=26 → 起始位置=26+3=29（"_v:"长度为3）
                var versionValueStart = versionFlagPos + versionFullFlag.Length;

                // 4.3 截取版本号值（到核心内容末尾，兼容含_/:等特殊字符）
                // 示例：核心内容=pk:product001_sn:scriptA_v:v2.0:beta → 截取29到末尾 → v2.0:beta
                var versionValue = coreContent.Substring(versionValueStart);

                // 4.4 空字符串转为null（与生成逻辑保持语义一致）
                // 示例：版本号为空 → _v: → 截取后为空 → scriptVersion=null
                scriptVersion = string.IsNullOrWhiteSpace(versionValue) ? null : versionValue;
            }
            #endregion

            #region Step 5: 日志提示（可选，便于调试）
            // 示例输出：解析JS编解码器池键完成 | 池键：JavaScriptDataCodec_pk:product_001:test_sn:script:name_v1_v:v2.0:beta | 产品Key：product_001:test | 脚本名：script:name_v1 | 版本号：v2.0:beta
            System.Diagnostics.Debug.WriteLine(
                $"解析JS编解码器池键完成 | 池键：{poolKey} | 产品Key：{productKey} | 脚本名：{scriptName ?? "无"} | 版本号：{scriptVersion ?? "无"}");
            #endregion

            return (productKey, scriptName, scriptVersion);
        }
        #endregion

        #region 便捷方法：单一字段获取（适配不同调用场景）
        /// <summary>
        /// 从池键中提取产品Key（便捷方法）
        /// 设计思路：
        /// 1. 复用全量解析逻辑，避免重复代码，保证规则统一；
        /// 2. 提供单一字段获取接口，简化调用方代码（无需处理元组）。
        /// </summary>
        /// <param name="poolKey">待解析的池键</param>
        /// <returns>产品唯一标识</returns>
        /// <exception cref="ArgumentException">池键格式不合法时抛出</exception>
        public static string GetProductKeyFromPoolKey(string poolKey)
        {
            var (productKey, _, _) = ParsePoolKey(poolKey);
            return productKey;
        }

        /// <summary>
        /// 从池键中提取脚本名称（便捷方法）
        /// 设计思路：
        /// 1. 复用全量解析逻辑，保证解析规则一致；
        /// 2. 空值返回null，符合.NET空值处理规范。
        /// </summary>
        /// <param name="poolKey">待解析的池键</param>
        /// <returns>脚本名称（无则返回null）</returns>
        /// <exception cref="ArgumentException">池键格式不合法时抛出</exception>
        public static string? GetScriptNameFromPoolKey(string poolKey)
        {
            var (_, scriptName, _) = ParsePoolKey(poolKey);
            return scriptName;
        }

        /// <summary>
        /// 从池键中提取脚本版本号（便捷方法）
        /// 设计思路：
        /// 1. 复用全量解析逻辑，避免重复维护解析规则；
        /// 2. 适配仅需版本号的业务场景，提升代码可读性。
        /// </summary>
        /// <param name="poolKey">待解析的池键</param>
        /// <returns>脚本版本号（无则返回null）</returns>
        /// <exception cref="ArgumentException">池键格式不合法时抛出</exception>
        public static string? GetScriptVersionFromPoolKey(string poolKey)
        {
            var (_, _, scriptVersion) = ParsePoolKey(poolKey);
            return scriptVersion;
        }
        #endregion

        #region 扩展方法：池键合法性校验
        /// <summary>
        /// 校验池键是否合法（新增扩展方法，提升易用性）
        /// 设计思路：
        /// 1. 封装校验逻辑，便于调用方快速判断池键有效性；
        /// 2. 捕获解析异常，返回布尔值，适配无需抛出异常的场景。
        /// </summary>
        /// <param name="poolKey">待校验的池键</param>
        /// <param name="errorMessage">校验失败时的错误信息（输出参数）</param>
        /// <returns>合法返回true，否则返回false</returns>
        public static bool IsValidPoolKey(string poolKey, out string? errorMessage)
        {
            errorMessage = null;
            try
            {
                ParsePoolKey(poolKey);
                return true;
            }
            catch (ArgumentException ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
        #endregion
    }
}