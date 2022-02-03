using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitHub.UI.Controls
{
    public class OcticonImage : UserControl
    {
        public static readonly StyledProperty<Octicon> IconProperty =
            OcticonPath.IconProperty.AddOwner<OcticonImage>();

        public OcticonImage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public Octicon Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
    }
}
