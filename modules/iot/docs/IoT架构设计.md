## 设备接入认证

### 参考资料

 一机一密

资料：https://help.aliyun.com/zh/iot/user-guide/unique-certificate-per-device-verification?spm=a2c4g.11186623.help-menu-30520.d_2_2_1_1_1.51451ddfmszATl&scm=20140722.H_74005._.OR_help-T_cn~zh-V_1

一型一密（一型一密认证支持两种使用方式：一型一密免预注册、一型一密预注册：）,
资料：https://help.aliyun.com/zh/iot/user-guide/unique-certificate-per-product-verification?spm=a2c4g.11186623.help-menu-30520.d_2_2_1_1_2.66656898HKi75Y 



在阿里云 IoT 平台的 MQTT 协议接入场景中，平台与设备需分别保存不同字段 / 参数以完成签名（sign）验证与通信，具体需保存的内容需结合**一机一密**和**一型一密（预注册 / 免预注册）** 两种认证方式的差异区分，以下是详细拆解：

### 一、核心前提：MQTT 签名（sign）的关键逻辑

阿里云 IoT 平台的 MQTT 签名本质是通过**设备证书 + 动态生成的时间戳 / 随机数**等参数，生成唯一签名串（sign），平台通过校验签名串的合法性确认设备身份。签名计算依赖的核心参数需在设备端预存、平台端预配置 / 动态生成，二者需保持一致才能通过认证。

### 二、分认证方式：平台与设备需保存的字段 / 参数

#### 1. 一机一密认证（直连设备 / 网关子设备通用）

一机一密的核心是**每个设备拥有唯一不可重复的设备证书**，签名依赖设备专属证书，无动态下发参数，字段保存逻辑简单。

| 角色       | 需保存的核心字段 / 参数                                      | 说明                                                         |
| ---------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **设备端** | 1. 产品证书：`ProductKey`（产品唯一标识，同一产品下所有设备相同）2. 设备证书：`DeviceName`（设备唯一名称，每个设备不同）3. 设备证书：`DeviceSecret`（设备密钥，每个设备唯一，用于签名计算）4. 接入域名（如`${ProductKey}.iot-as-mqtt.${regionId}.aliyuncs.com`） | - 所有参数需在**产线烧录阶段预写入设备**，不可丢失或泄露- `DeviceSecret`是签名核心，需加密存储，禁止明文传输或暴露 |
| **平台端** | 1. 产品信息：`ProductKey` + 产品状态（如是否启用）2. 设备信息：`DeviceName` + `DeviceSecret`（与设备端烧录的一一对应）3. 设备状态（未激活 / 已激活 / 禁用） | - 平台在 “添加设备” 时生成`DeviceName`和`DeviceSecret`并存储，用于校验设备签名- 认证时通过`ProductKey`定位产品，再通过`DeviceName`匹配对应的`DeviceSecret`验证签名 |

#### 2. 一型一密认证（分预注册 / 免预注册）

一型一密的核心是**同一产品下所有设备共享产品证书**，设备需通过动态注册获取专属通信参数，字段保存逻辑分 “初始预存” 和 “动态获取后保存” 两阶段。

##### （1）一型一密预注册（直连设备 / 网关子设备通用）

预注册需在平台提前创建`DeviceName`并生成`DeviceSecret`，设备激活时需校验预存的`DeviceSecret`。

| 角色       | 需保存的核心字段 / 参数                                      | 说明                                                         |
| ---------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **设备端** | 【初始烧录】1. 产品证书：`ProductKey`、`ProductSecret`（同一产品下所有设备相同）2. 设备预注册信息：`DeviceName`（平台提前创建，每个设备不同，如 MAC/IMEI）【动态获取后保存】3. `DeviceSecret`（平台激活时下发，与`DeviceName`一一对应）4. 接入域名 | - 初始仅烧录`ProductKey`、`ProductSecret`、`DeviceName`，`DeviceSecret`需激活后从平台获取并持久化存储- 后续通信签名需使用`DeviceSecret`，而非初始的`ProductSecret` |
| **平台端** | 1. 产品信息：`ProductKey`、`ProductSecret` + 动态注册开关状态2. 设备预注册信息：`DeviceName` + 预生成的`DeviceSecret` + 设备状态（未激活 / 已激活）3. 接入域名配置 | - 平台提前存储`DeviceName`与`DeviceSecret`的对应关系，激活时校验设备携带的`ProductKey`、`ProductSecret`、`DeviceName`，通过后下发`DeviceSecret`确认身份 |

##### （2）一型一密免预注册（仅支持直连设备 MQTT 协议）

免预注册无需提前在平台创建`DeviceName`，设备动态生成`DeviceName`，激活时平台下发临时通信参数。

| 角色       | 需保存的核心字段 / 参数                                      | 说明                                                         |
| ---------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **设备端** | 【初始烧录】1. 产品证书：`ProductKey`、`ProductSecret`（同一产品下所有设备相同）2. 动态生成的`DeviceName`（如设备 MAC/IMEI，无需提前在平台注册）【动态获取后保存】3. `ClientID`（平台下发，设备唯一标识）4. `DeviceToken`（平台下发，用于后续签名计算）5. 接入域名 | - 初始仅烧录`ProductKey`、`ProductSecret`，`DeviceName`由设备本地生成（需保证唯一性）- 激活后必须持久化存储`ClientID`和`DeviceToken`，后续通信签名需使用`DeviceToken`，而非`ProductSecret` |
| **平台端** | 1. 产品信息：`ProductKey`、`ProductSecret` + 动态注册开关状态2. 设备动态注册记录：`DeviceName`（设备上报）→ 关联下发的`ClientID`、`DeviceToken` + 设备连接状态3. 风险设备监控数据（如同一`DeviceName`对应的多个`ClientID`） | - 平台无需提前存储`DeviceName`，激活时校验`ProductKey`、`ProductSecret`合法性，通过后生成并下发`ClientID`、`DeviceToken`，同时记录二者与`DeviceName`的关联关系- 支持同一`DeviceName`对应最多 5 个`ClientID`（不同物理设备），需监控风险并提供切换 / 清除功能 |

### 三、共性与差异总结

#### 1. 共性字段

- **设备端必存**：`ProductKey`（所有认证方式通用，定位产品）、接入域名（固定，由产品所属地域决定）。
- **平台端必存**：`ProductKey`、产品对应的密钥（一机一密无`ProductSecret`，一型一密有`ProductSecret`）、动态注册开关状态。

#### 2. 核心差异（签名依赖的密钥）

| 认证方式         | 设备端签名核心密钥         | 平台端校验核心依据                     |
| ---------------- | -------------------------- | -------------------------------------- |
| 一机一密         | 设备专属`DeviceSecret`     | 预存的`DeviceName`→`DeviceSecret`映射  |
| 一型一密预注册   | 激活后下发的`DeviceSecret` | 预存的`DeviceName`→`DeviceSecret`映射  |
| 一型一密免预注册 | 激活后下发的`DeviceToken`  | 动态生成的`ClientID`→`DeviceToken`映射 |

### 四、关键注意事项

1. **密钥安全**：设备端需加密存储`DeviceSecret`（一机一密 / 一型一密预注册）或`DeviceToken`（一型一密免预注册），禁止明文传输或暴露，避免被伪造认证。
2. **动态注册开关**：一型一密两种方式均需在平台 “产品详情页” 开启动态注册开关，否则设备激活请求会被拒绝（已激活设备不受影响）。
3. **SDK 版本要求**：一型一密免预注册需使用阿里云 IoT **4.X 版 C Link SDK**（含 DAS 设备取证服务），否则平台不承担安全风险。



## MQTT-TLS连接通信

https://help.aliyun.com/zh/iot/user-guide/establish-mqtt-connections-over-tcp?spm=a2c4g.11186623.0.0.7ce2476b5dA5aH

### 客户端连接与参数设置

1. **连接参考资源**：可参考 [开源 MQTT 客户端] 文档配置连接方式，MQTT 协议细节可查看 [MQTT 官方文档]；使用第三方代码时，阿里云不提供技术支持。

2. 核心连接参数：

   - **接入域名**：需根据公共实例 / 企业版实例，参考 [查看和配置实例终端节点信息（Endpoint）] 获取对应 MQTT 接入域名。

   - **Keep Alive（保活时间）**：CONNECT 指令必含参数，取值范围 30 秒～1200 秒，建议 300 秒以上；网络不稳定时可延长，不在此范围平台会拒绝连接，详细规则见 “MQTT 保活” 部分。

   - CONNECT 报文参数

     ：分两种认证方式，参数格式与计算规则不同：

     | 认证方式                                | 参数格式示例             | 关键说明                                             |                                                              |                                                              |
     | --------------------------------------- | ------------------------ | ---------------------------------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
     | 一机一密 / 一型一密预注册（用设备证书） | mqttClientId: clientId+' | securemode=3,signmethod=hmacsha1,timestamp=132323232 | '；mqttUsername: deviceName+'&'+productKey；mqttPassword: sign_hmac(deviceSecret,content) | 1. clientId：自定义，≤64 字符，建议用设备 MAC/SN 码；2. securemode：安全模式，2 为 TLS 直连、3 为 TCP 直连；3. signmethod：签名算法，支持 hmacmd5/hmacsha1/hmacsha256；4. timestamp：当前时间毫秒值，可选；5. content：按参数名首字母字典排序（productKey、deviceName 必填，timestamp、clientId 可选）后拼接参数值，再按 signmethod 加签；6. 若传 timestamp/clientId，需与 mqttClientId 中设置一致 |
     | 一型一密免预注册（用 DeviceToken）      | mqttClientId: clientId+' | securemode=-2,authType=connwl                        | '；mqttUsername: deviceName+'&'+productKey；mqttPassword: deviceToken | 1. clientId、DeviceToken：从设备动态注册获取（参考 [基于 MQTT 协议的设备动态注册]）；2. securemode 固定为 - 2，authType 固定为 connwl |



MQTT连接签名示例

https://help.aliyun.com/zh/iot/user-guide/examples-of-creating-signatures-for-mqtt-connections?spm=a2c4g.11186623.0.0.36146b18cHTWqq#concept-188639



### 使用示例资源

提供多种开源 MQTT 客户端接入示例，可直接参考实操，包括：

- [Paho-MQTT Go 接入示例]
- [Paho-MQTT C# 接入示例]
- [Paho-MQTT C（嵌入式版）接入示例]
- [Paho-MQTT Java 接入示例]
- [Paho-MQTT Android 接入示例]
- [使用 MQTT.fx 接入物联网平台]



### MQTT 保活机制

1. **保活核心要求**：设备端需在保活时间间隔内至少发送一次报文（含 ping 请求）。

2. 计时与断开规则

   ：

   - 平台发送 CONNACK 响应 CONNECT 消息时开始心跳计时，收到 PUBLISH、SUBSCRIBE、PING、PUBACK 消息时重置计时器。
   - 平台每 30 秒定时检测设备保活心跳，定义 “最大超时时间 = 保活心跳时间 ×1.5 + 定时检测等待时间”，超此时长未收到设备消息，服务器自动断开连接



## 签名算法

https://help.aliyun.com/zh/iot/user-guide/how-do-i-obtain-mqtt-parameters-for-authentication?spm=a2c4g.11186623.0.0.36146b18cHTWqq#task-2100587



## 基于MQTT协议的设备动态注册

https://help.aliyun.com/zh/iot/user-guide/mqtt-based-dynamic-registration?spm=a2c4g.11186623.0.0.4b9e2a48LKMaV3

