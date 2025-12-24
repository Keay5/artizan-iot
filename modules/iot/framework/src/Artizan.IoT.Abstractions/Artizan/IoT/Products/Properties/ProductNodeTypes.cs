using System;
using System.Collections.Generic;
using System.Text;

namespace Artizan.IoT.Products.Properties;

/// <summary>
/// 节点类型
/// 参见：https://help.aliyun.com/zh/iot/user-guide/gateways-and-sub-devices?spm=a2c4g.11186623.help-menu-30520.d_2_2_3_1_0.cdec7f6fS37pAK
/// </summary>
public enum ProductNodeTypes
{
    /// <summary>
    /// 直连设备
    /// 具有IP地址，可直接连接物联网平台，且不能挂载子设备，但可作为子设备挂载到网关设备下。
    /// </summary>
    DirectConnectionEquipment,

    /// <summary>
    /// 网关设备
    /// 不直接连接物联网平台，而是作为网关的子设备，由网关代理连接物联网平台。
    /// </summary>
    GatewayEquipment,

    /// <summary>
    /// 网关子设备
    /// 以挂载子设备的直连设备，下文简称网关。网关具有子设备管理模块，可以维持子设备的拓扑关系，将与子设备的拓扑关系同步到云端。
    /// </summary>
    GatewaySubEquipment
}
