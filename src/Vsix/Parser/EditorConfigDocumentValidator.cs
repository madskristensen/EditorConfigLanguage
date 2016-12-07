using System;
using System.Linq;

namespace EditorConfig
{
    partial class EditorConfigDocument
    {
        public void Validate()
        {
            foreach (var item in ParseItems)
            {
                switch (item.ItemType)
                {
                    case ItemType.Section:
                        break;
                    case ItemType.Keyword:
                        ValidateKeyword(item);
                        break;
                    case ItemType.Value:
                        ValidateValue(item);
                        break;
                    case ItemType.Severity:
                        ValidateSeverity(item);
                        break;
                }
            }
        }

        private void ValidateValue(ParseItem item)
        {
            var prev = ParseItems.LastOrDefault(p => p.Span.Start < item.Span.Start);
            var comp = Keyword.GetCompletionItem(prev?.Text);

            if (comp != null &&
                !comp.Values.Contains(item.Text, StringComparer.OrdinalIgnoreCase) &&
                !(int.TryParse(item.Text, out int intValue) && intValue > 0))
            {
                item.AddError(string.Format(Resources.Text.InvalidValue, item.Text, comp.Name));
            }

            if (item.Text.Equals("true", StringComparison.OrdinalIgnoreCase) &&
               (prev.Text.StartsWith("csharp_") || prev.Text.StartsWith("dotnet_")))
            {
                var next = ParseItems.FirstOrDefault(p => p.Span.Start > item.Span.Start);

                if (next == null || next.ItemType != ItemType.Severity)
                {
                    item.AddError(Resources.Text.ValidationMissingSeverity);
                }
            }
        }

        private void ValidateSeverity(ParseItem item)
        {
            if (!Constants.Severity.Contains(item.Text))
            {
                item.AddError(string.Format(Resources.Text.ValidationInvalidSeverity, string.Join(", ", Constants.Severity)));
            }
        }

        private void ValidateKeyword(ParseItem item)
        {
            if (Keyword.GetCompletionItem(item.Text) == null)
            {
                item.AddError(string.Format(Resources.Text.ValidateUnknownKeyword, item.Text));
            }
            else if (item.Text.Equals("root", StringComparison.OrdinalIgnoreCase) && item != ParseItems.First(p => p.ItemType != ItemType.Comment))
            {
                item.AddError(Resources.Text.KeywordRoot);
            }
        }
    }
}
