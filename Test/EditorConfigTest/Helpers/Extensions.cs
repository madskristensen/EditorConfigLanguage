using EditorConfig;
using System.Threading.Tasks;

namespace EditorConfigTest
{
    public static class Extensions
    {
        public static async Task WaitForParsingCompleteAsync(this EditorConfigDocument document)
        {
            await document.ParsingTask;
        }
    }
}
