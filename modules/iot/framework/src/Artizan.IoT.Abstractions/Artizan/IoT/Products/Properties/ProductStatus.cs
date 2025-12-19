using System.Collections.Generic;
using System.Text;

namespace Artizan.IoT.Products.Properties;

/// <summary>
/// 产品状态
/// </summary>
public enum ProductStatus
{
    /// <summary>
    /// 开发中: 创建产品后的状态
    /// </summary>
    Developing,
    /// <summary>
    /// 已发布:产品发布后的状态，不支持修改和删除操作。
    /// </summary>
    Published
}
