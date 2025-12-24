using System.Collections.Generic;
using System.Threading.Tasks;

namespace Artizan.IoT.ThingModels.Tsls.Validators;

public interface ITslValidator
{
    Task<(bool IsValid, List<string> Errors)> ValidateAsync(Tsl tsl, bool validateJsonSchema = true);
    Task<(bool IsValid, List<string> Errors)> ValidateAsync(string tslJsonString, bool validateJsonSchema = true);
}
