# 参考资料

平台：https://www.huaweicloud.com/product/iot.html

IoTDA ：https://www.huaweicloud.com/product/iothub.html





# IoTDA 功能

![1763077861228](images/Artizan-IoTDA-Design/1763077861228.png)



# IoTDA 帮助中心

https://support.huaweicloud.com/devg-iothub/iot_02_0170.html



# IoTDA 实例

## 创建

![1763078011713](images/Artizan-IoTDA-Design/1763078011713.png)



## 实例页面

![1763078084299](images/Artizan-IoTDA-Design/1763078084299.png)

点击具体实例，进入IoTDA 实例平台：

![1763078282214](images/Artizan-IoTDA-Design/1763078282214.png)



# 产品

## 帮助中心

https://support.huaweicloud.com/devg-iothub/iot_01_0058.html



## 产品主页

![1763078326280](images/Artizan-IoTDA-Design/1763078326280.png)



### 创建

点击产品主页的【创建产品】按钮，弹出【创建产品弹框】，如下图所示：

<img src="images/Artizan-IoTDA-Design/1763083101450.png" alt="1763083101450" style="zoom: 50%;" />

### 所属资源空间

资料：https://console.huaweicloud.com/iotdm/?region=cn-north-4#/dm-portal/resource?instanceId=a65ead22-b0f2-466c-8e88-3b86b451a5c4

【暂时不做】



### 产品名称

<img src="images/Artizan-IoTDA-Design/1763085477741.png" alt="1763085477741" style="zoom:50%;" />

 长度不超过64个字符，只允许中文、字母、数字、以及_?'#().,&%@!-等字符的组合。 

Op

### 协议类型

CoAP协议的设备需要完善数据解析，将设备上报的二进制数据转换为平台上的JSON数据格式

MQTT协议的设备，若采用二进制码流的数据格式，则需要完善数据解析，将设备上报的二进制数据转换为平台上的JSON数据格式

![](images/Artizan-IoTDA-Design/1763085612635.png)

![1763085667745](images/Artizan-IoTDA-Design/1763085667745.png)



### 数据格式

数据格式为二进制码流时，该产品下需要进行编解码插件开发。

数据格式为JSON时，该产品下不需要进行编解码插件开发。

![1763085926506](images/Artizan-IoTDA-Design/1763085926506.png)

### 创建成功提示

<img src="images/Artizan-IoTDA-Design/1763099527767.png" alt="1763099527767" style="zoom: 50%;" />

点击【定义产品模型】，进入如下页面：

https://console.huaweicloud.com/iotdm/?region=cn-north-4#/dm-dev/all-product/33d2ecec1d7c455b92b7a4a38258d710/product-detail/6916c363f69b1239b07e3b88/basicInfo?instanceId=a65ead22-b0f2-466c-8e88-3b86b451a5c4

![1763099615564](images/Artizan-IoTDA-Design/1763099615564.png)



## 产品模型

### 资料

什么是产品模型：https://support.huaweicloud.com/devg-iothub/iot_01_0017.html



## 什么是产品模型

 产品模型用于描述设备具备的能力和特性。开发者通过定义产品模型，在物联网平台构建一款设备的抽象模型，使平台理解该款设备支持的服务、属性、命令等信息，如开关等。当定义完一款产品模型后，在进行[注册设备](https://support.huaweicloud.com/usermanual-iothub/iot_01_0031.html)时，就可以使用在控制台上定义的产品模型。 



## 开发产品模型

https://support.huaweicloud.com/devg-iothub/iot_02_0005.html

## 创建服务

![1763105118937](images/Artizan-IoTDA-Design/1763105118937.png)





# 设备



## 注册单个设备

https://support.huaweicloud.com/devg-iothub/iot_01_0031.html