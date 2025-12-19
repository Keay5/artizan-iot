using Artizan.IoT.Products.MessageParsings;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace Artizan.IoTHub.Products.MessageParsings;

/// <summary>
/// 产品消息解析器
/// JavaScript脚本解示例：
/// https://iot.console.aliyun.com/product/productDetail/a1BsG4cqhIt?current=4
/// </summary>
public class ProductMessageParser : AggregateRoot<Guid>
{
    [NotNull]
    public virtual Guid ProductId { get; set; }

    [NotNull]
    public virtual ProuctMessageParserScriptLanguage ScriptLanguage { get; set; }

    /// <summary>
    /// 特别注意：一个产品Product 名下只能有一个状态为：Pulished 的消息解析器
    /// </summary>
    [NotNull]
    public virtual ProductMessageParserStatus Status { get; set; }

    [NotNull]
    public virtual string Script { get; protected set; }

    protected ProductMessageParser()
    {
    }

    public ProductMessageParser(
        Guid id,
        Guid productId,
        ProuctMessageParserScriptLanguage scriptLanguage,
        ProductMessageParserStatus status,
        string script)
    {
        Id = id;
        ProductId = productId;
        ScriptLanguage = scriptLanguage;
        Status = status;
        SetScript(script);
    }

    public ProductMessageParser SetScript(string script)
    {
        Check.NotNullOrWhiteSpace(script, nameof(script));

        Script = script;

        return this;
    }
}
