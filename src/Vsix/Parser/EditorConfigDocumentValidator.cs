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

        private void ValidateSection(ParseItem item)
        {
            if (ParseItems.Exists(p => p.ItemType == ItemType.Section && p.Span.Start < item.Span.Start && p.Text == item.Text))
            {
                item.AddError(string.Format(Resources.Text.ValidationDuplicateSection, item.Text));
            }
        }

        private void ValidateValue(ParseItem item)
        {
            var comp = Property.FromName(item.Prev?.Text);

            if (comp != null &&
                !comp.Values.Contains(item.Text, StringComparer.OrdinalIgnoreCase) &&
                !(int.TryParse(item.Text, out int intValue) && intValue > 0))
            {
                item.AddError(string.Format(Resources.Text.InvalidValue, item.Text, comp.Text));
            }

            if (item.Text.Equals("true", StringComparison.OrdinalIgnoreCase) && comp != null && comp.SupportsSeverity)
            {
                if (item.Next == null || item.Next.ItemType != ItemType.Severity)
                {
                    item.AddError(Resources.Text.ValidationMissingSeverity);
                }
            }
        }

        private void ValidateSeverity(ParseItem item)
        {
            var prop = Property.FromName(item.Prev?.Prev?.Text);

            if (prop != null && !prop.SupportsSeverity)
            {
                item.AddError(string.Format("The \"{0}\" property does not support a severity suffix", prop.Text));
            }
            else if (!Constants.Severities.ContainsKey(item.Text))
            {
                item.AddError(string.Format(Resources.Text.ValidationInvalidSeverity, string.Join(", ", Constants.Severities.Keys)));
            }
        }

        private void ValidateKeyword(ParseItem item)
        {
            if (Property.FromName(item.Text) == null)
            {
                item.AddError(string.Format(Resources.Text.ValidateUnknownKeyword, item.Text));
            }
            else if (item.Text.Equals(Property.Root, StringComparison.OrdinalIgnoreCase) && item != ParseItems.First(p => p.ItemType != ItemType.Comment))
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
