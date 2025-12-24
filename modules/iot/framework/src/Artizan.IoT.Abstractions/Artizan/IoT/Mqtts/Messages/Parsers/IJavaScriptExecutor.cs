using System.Threading.Tasks;

namespace Artizan.IoT.Mqtts.Messages.Parsers;

public interface IJavaScriptExecutor
{
    Task<string> Execute(string javaScript, byte[] rawData);
}
