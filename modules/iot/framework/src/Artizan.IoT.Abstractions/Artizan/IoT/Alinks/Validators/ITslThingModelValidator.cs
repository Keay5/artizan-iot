using Artizan.IoT.Alinks.DataObjects;
using Artizan.IoT.Alinks.Results;
using System.Threading.Tasks;

namespace Artizan.IoT.Alinks.Validators;

/// <summary>
/// TSL校验器接口（接口隔离）
/// </summary>
public interface ITslThingModelValidator
{
    Task<AlinkHandleResult> ValidateAsync(AlinkDataContext dataContext);
}