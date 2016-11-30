namespace EditorConfig
{
    public class Constants
    {
        public const string LanguageName = "EditorConfig";
        public const string FileName = ".editorconfig";
        public const string Homepage = "http://editorconfig.org";

        public static string[] Severity { get; } = { "none", "suggestion", "warning", "error" };
    }
}
