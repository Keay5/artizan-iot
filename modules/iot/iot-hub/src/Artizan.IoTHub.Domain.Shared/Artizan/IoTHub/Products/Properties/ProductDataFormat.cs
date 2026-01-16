using System;
using System.Collections.Generic;
using System.Text;

namespace Artizan.IoTHub.Products.Properties;

/// <summary>
/// Alink 协议是针对物联网开发领域设计的一种数据交换规范，
/// 是物联网平台为开发者提供的设备与云端的数据交换协议，采用JSON格式,
/// 用于设备端和云端的双向通信，
/// 更便捷地实现和规范了设备端和云端之间的业务数据交互，
/// 协议格式详见 文档:
/// https://help.aliyun.com/zh/iot/user-guide/alink-protocol-1?spm=5176.11485173.help.dexternal.705f59afDq1Fkn
/// </summary>
public enum ProductDataFormat
{
    /// <summary>
    /// ICA 标准数据格式（Alink JSON） 
    /// </summary>
    ICAStandardDataFormat,

    /// <summary>
    /// 透传/自定义
    /// </summary>
    PassThroughOrCustom
}
