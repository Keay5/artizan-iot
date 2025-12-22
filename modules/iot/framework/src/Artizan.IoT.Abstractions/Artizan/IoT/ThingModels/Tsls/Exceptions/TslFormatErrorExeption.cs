using Volo.Abp;

namespace Artizan.IoT.ThingModels.Tsls.Exceptions;

public class TslFormatErrorExeption : BusinessException
{
    public TslFormatErrorExeption(string errors)
        : base(IoTAbstractionsErrorCodes.Tsls.TSLFormatError)
    {
        WithData("Errors", errors);
    }
}
