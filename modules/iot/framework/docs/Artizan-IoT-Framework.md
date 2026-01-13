# Artizan-IoT 解决方案

## 创建

创建 Artizan-IoT.sln 解决方案，然后在其子目录下创建 src 和 test 目录，分别存放代码和测试代码。

再执行如下命令，以便能使用后续使用 ABP CLI 创建项目。

```bash
iot\framework> abp init-solution
```

命令执行完成后，会在根目录（即：framework）下自动生成如下文件：

- Artizan.IoT.abpmdl
- Artizan.IoT.abpsln



## common.props

新建 common.props 文件，内容如下：

```xml
<Project>
  <Import Project="..\..\..\common.moudle.props" />

</Project>
```

以便后续新建的项目统一引用。



## Artizan.IoT.Core

创建类库：Artizan.IoT.Core，**在 `*.abpmdl`  （ABP Module 文件）所在目录**执行如下命令，创建类库。

```bash
iot\framework> abp new-package --name Artizan.IoT.Core --template lib.class-library --solution-name Artizan.IoT.sln --folder src --version 9.3.6
```



## Artizan.IoT.Abstractions

创建类库：Artizan.IoT.Abstractions，在 `*.abpmdl`  （ABP Module 文件）所在目录执行如下命令，创建类库。

```bash
iot\framework> abp new-package --name Artizan.IoT.Abstractions --template lib.class-library --solution-name Artizan.IoT.sln --folder src --version 9.3.6
```



# Artizan.IoT.Mqtt

创建类库：Artizan.IoT.Mqtt，**在 `*.abpmdl`  （ABP Module 文件）所在目录**执行如下命令，创建类库。

```bash
iot\framework> abp new-package --name Artizan.IoT.Mqtt --template lib.class-library --solution-name Artizan.IoT.sln --folder src --version 9.3.6
```



# Artizan.IoT.TimeSeries.Abstractions

创建类库：Artizan.IoT.TimeSeries.Abstractions，**在 `*.abpmdl`  （ABP Module 文件）所在目录**执行如下命令，创建类库。

```bash
iot\framework> abp new-package --name Artizan.IoT.TimeSeries.Abstractions --template lib.class-library --solution-name Artizan.IoT.sln --folder src --version 9.3.6
```



# Artizan.IoT.TimeSeries.InfluxDB2

创建类库：Artizan.IoT.TimeSeries.InfluxDB2，**在 `*.abpmdl`  （ABP Module 文件）所在目录**执行如下命令，创建类库。

```bash
iot\framework> abp new-package --name Artizan.IoT.TimeSeries.InfluxDB2 --template lib.class-library --solution-name Artizan.IoT.sln --folder src --version 9.3.6
```



# Artizan.IoT.Thing

创建类库，**在 `*.abpmdl`  （ABP Module 文件）所在目录**执行如下命令，创建类库。

```bash
iot\framework> abp new-package --name Artizan.IoT.Thing --template lib.class-library --solution-name Artizan.IoT.sln --folder src --version 9.3.6
```



# Artizan.IoT.ScriptDataCodec

创建类库，**在 `*.abpmdl`  （ABP Module 文件）所在目录**执行如下命令，创建类库。

```bash
iot\framework> abp new-package --name Artizan.IoT.ScriptDataCodec --template lib.class-library --solution-name Artizan.IoT.sln --folder src --version 9.3.6
```



# Artizan.IoT.ScriptDataCodec.JavaScript

创建类库，**在 `*.abpmdl`  （ABP Module 文件）所在目录**执行如下命令，创建类库。

```bash
iot\framework> abp new-package --name Artizan.IoT.ScriptDataCodec.JavaScript --template lib.class-library --solution-name Artizan.IoT.sln --folder src --version 9.3.6
```



# Artizan.IoT.ScriptDataCodec.Python

创建类库，**在 `*.abpmdl`  （ABP Module 文件）所在目录**执行如下命令，创建类库。

```bash
iot\framework> abp new-package --name Artizan.IoT.ScriptDataCodec.Python --template lib.class-library --solution-name Artizan.IoT.sln --folder src --version 9.3.6
```



