# IoTHub Module

## 创建

手动创建一个名为：**iot-hub** 的文件夹,，并在该文件中打开 Powershell ，执行如下命令创建 Moudle：

```bash
iot\iot-hub> abp new-module Artizan.IoTHub --template module:ddd --database-provider ef,mongodb --ui-framework mvc,blazor --version 9.3.6
```

注意：执行命令前，确保目录下没有任何文件，否则创建失败。

说明：

- ABP Cli创建Module，参见：https://abp.io/docs/latest/cli#new-module

- --version 9.3.6： 指定Abp包版本；



## common.props

修改 common.props 为如下内容：

```xml
<Project>
    <Import Project="..\..\..\common.moudle.props" />

</Project>
```



# Demo项目

## 添加数据迁移

在 `.EntityFrameworkCore`  项目的根目录下执行如下命令，添加数据迁移

```bash
dotnet ef migrations add Initial
```
