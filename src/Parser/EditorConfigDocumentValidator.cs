using System;
using System.Linq;

namespace EditorConfig
{
    partial class EditorConfigDocument
    {
        private DateTime _lastRequestForValidation;
        private const int _validationDelay = 1000;

        private async System.Threading.Tasks.Task ValidateAsync()
        {
            _lastRequestForValidation = DateTime.Now;
            await System.Threading.Tasks.Task.Delay(_validationDelay);

            if (DateTime.Now.AddMilliseconds(-_validationDelay) < _lastRequestForValidation)
                return;

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
                    case ItemType.Unknown:
                        ValidateUnknown(item);
                        break;
                }
            }

            Validated?.Invoke(this, EventArgs.Empty);
        }

        private void ValidateUnknown(ParseItem item)
        {
            item.AddError("Syntax error. Element not valid at current location");
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
            if (!SchemaCatalog.TryGetProperty(item.Prev?.Text, out Keyword comp))
                return;

            if (!comp.Values.Any(v => v.Name.Equals(item.Text, StringComparison.OrdinalIgnoreCase)) &&
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
            if (SchemaCatalog.TryGetProperty(item.Prev?.Prev?.Text, out Keyword prop) && !prop.SupportsSeverity)
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
            if (!SchemaCatalog.TryGetProperty(item.Text, out Keyword prop))
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

        public event EventHandler Validated;
    }
}
