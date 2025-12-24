# IoTDA Module

## 创建

在 `iot-data-access` 目录中打开 Powershell ，执行如下命令创建 Moudle：

```bash
iot\iot-data-access> abp new-module Artizan.IoTDA --template module:ddd  --database-provider ef,mongodb --ui-framework mvc,blazor --version 9.3.6
```

注意：执行命令前，确保目录下没有任何文件，否则创建失败。

说明：

- ABP Cli创建Module，参见：https://abp.io/docs/latest/cli#new-module

- --version 9.3.6： 指定Abp包版本；



## 添加数据迁移

在 `.EntityFrameworkCore`  项目的根目录下执行如下命令，添加数据迁移

```bash
dotnet ef migrations add Initial
```

