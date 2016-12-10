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
                        ValidateSection(item);
                        break;
                    case ItemType.Property:
                        ValidateProperty(item);
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

        private void ValidateSection(ParseItem item)
        {
            if (ParseItems.Exists(p => p.ItemType == ItemType.Section && p.Span.Start < item.Span.Start && p.Text == item.Text))
            {
                item.AddError(string.Format(Resources.Text.ValidationDuplicateSection, item.Text));
            }
        }

        private void ValidateValue(ParseItem item)
        {
            if (!SchemaCatalog.TryGetProperty(item.Prev?.Text, out Property comp))
                return;

            if (!comp.Values.Contains(item.Text, StringComparer.OrdinalIgnoreCase) &&
                !(int.TryParse(item.Text, out int intValue) && intValue > 0))
            {
                item.AddError(string.Format(Resources.Text.InvalidValue, item.Text, comp.Name));
            }

            if (item.Text.Equals("true", StringComparison.OrdinalIgnoreCase) && comp.SupportsSeverity)
            {
                if (item.Next == null || item.Next.ItemType != ItemType.Severity)
                {
                    item.AddError(Resources.Text.ValidationMissingSeverity);
                }
            }
        }

        private void ValidateSeverity(ParseItem item)
        {
            if (SchemaCatalog.TryGetProperty(item.Prev?.Prev?.Text, out Property prop) && !prop.SupportsSeverity)
            {
                item.AddError(string.Format("The \"{0}\" property does not support a severity suffix", prop.Name));
            }
            else if (!SchemaCatalog.TryGetSeverity(item.Text, out Severity severity))
            {
                item.AddError(string.Format(Resources.Text.ValidationInvalidSeverity, item.Text));
            }
        }

        private void ValidateProperty(ParseItem item)
        {
            if (!SchemaCatalog.TryGetProperty(item.Text, out Property prop))
            {
                item.AddError(string.Format(Resources.Text.ValidateUnknownKeyword, item.Text));
            }
            else if (item.Text.Equals(SchemaCatalog.Root, StringComparison.OrdinalIgnoreCase) && item != ParseItems.First(p => p.ItemType != ItemType.Comment))
            {
                item.AddError(Resources.Text.ValidationRootInSection);
            }
            else if (item.Parent != null)
            {
                var children = item.Parent.Children.Where(c => c.Span.Start < item.Span.Start);
                if (children.Any(c => c.Text.Equals(item.Text, StringComparison.OrdinalIgnoreCase)))
                {
                    item.AddError(Resources.Text.ValidationDuplicateProperty);
                }
            }
        }
    }
}
