using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace WDE.Common.Avalonia.Controls
{
    public class SettingsHeader : TemplatedControl
    {
        private string? text;
        public static readonly DirectProperty<SettingsHeader, string?> TextProperty = AvaloniaProperty.RegisterDirect<SettingsHeader, string?>("Text", o => o.Text, (o, v) => o.Text = v);

        public string? Text
        {
            get => text;
            set => SetAndRaise(TextProperty, ref text, value);
        }
    }
}