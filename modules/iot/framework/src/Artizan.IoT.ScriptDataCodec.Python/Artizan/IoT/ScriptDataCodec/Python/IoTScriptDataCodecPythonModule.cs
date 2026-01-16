using Volo.Abp.Modularity;

namespace Artizan.IoT.ScriptDataCodec.Python;

[DependsOn(
    typeof(IoTScriptDataCodecModule)
)]
public class IoTScriptDataCodecPythonModule : AbpModule
{

}