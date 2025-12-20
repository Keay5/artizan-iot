using System;
using System.Collections.Generic;
using System.Text;

namespace Artizan.IoT.Products.Properties;

/// <summary>
/// 产品所属品类
/// ----
/// 选择的品类定义了该类设备的物模型，
/// 您可以直接选择并快速完成产品的功能定义，
/// 您也可以根据实际需要进行扩展和自定义完成产品的功能定义。
/// 品类的物模型标准来源于 ICA 联盟：
/// https://www.ica-alliance.org/dataindex?spm=5176.11485173.0.0.705f59afDq1Fkn
/// 了解或申请新品类
/// </summary>
public enum ProductCategory
{
    CustomCategory,
    StandardCategory
}
