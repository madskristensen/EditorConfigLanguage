using EditorConfig;
using System.Threading.Tasks;

namespace EditorConfigTest
{
    public static class Extensions
    {
        public static async Task WaitForParsingCompleteAsync(this EditorConfigDocument document)
        {
            while (document.IsParsing)
            {
                await Task.Delay(2);
            }
        }
    }
}
