using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WoWDatabaseEditorCore.Services.Processes
{
    public static class ProcessServiceExtensions
    {
        public static async Task<T?> RunAndGetJson<T>(this IProcessService service, string path, string arguments,
            string? workingDirectory)
        {
            var output = await service.RunAndGetOutput(path, arguments, workingDirectory);
            if (output == null)
                return default;

            T? t = JsonConvert.DeserializeObject<T>(output);
            return t;
        }
        
        public static async Task<string?> RunAndGetOutput(this IProcessService service, string path, string arguments, string? workingDirectory)
        {
            StringBuilder sb = new();
            bool any = false;
            await service.Run(default, path, arguments, workingDirectory, s =>
            {
                sb.AppendLine(s);
                any = true;
            }, null);
            if (any)
                return sb.ToString();
            return null;
        }
        
        public static async Task<string?> RunAndGetOutputAndError(this IProcessService service, string path, string arguments, string? workingDirectory)
        {
            StringBuilder sb = new();
            bool any = false;
            await service.Run(default, path, arguments, workingDirectory, s =>
            {
                sb.Append(s);
                any = true;
            }, s =>
            {
                sb.Append(s);
                any = true;
            });
            if (any)
                return sb.ToString();
            return null;
        }
    }
}