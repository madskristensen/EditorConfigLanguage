using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Linq;

namespace EditorConfig
{
    internal sealed class NamingEntityTooltip : ITooltip
    {
        internal NamingEntityTooltip(NamingEntity entity, int referenceCount)
        {
            Name = $"{GetDisplayName(entity.Kind)} {entity.Name}";
            string members = string.Join(
                "\n",
                entity.Members
                    .OrderBy(member => member.Key)
                    .Select(member => $"{member.Key} = {member.Value.Last().Value?.Text}"));
            Description = referenceCount == 1
                ? $"{members}\nReferenced once."
                : $"{members}\nReferenced {referenceCount} times.";
        }

        public string Name { get; }

        public string Description { get; }

        public ImageMoniker Moniker => KnownMonikers.Property;

        public bool IsSupported => true;

        private static string GetDisplayName(NamingEntityKind kind)
        {
            return kind switch
            {
                NamingEntityKind.Rule => "Naming rule",
                NamingEntityKind.Symbols => "Symbol group",
                NamingEntityKind.Style => "Naming style",
                _ => "Naming entity",
            };
        }
    }
}
