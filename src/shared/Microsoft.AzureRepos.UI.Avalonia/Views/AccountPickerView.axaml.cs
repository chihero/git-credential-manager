using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using GitCredentialManager.UI.Controls;
using Microsoft.AzureRepos.UI.ViewModels;

namespace Microsoft.AzureRepos.UI.Views
{
    public class AccountPickerView : UserControl, IFocusable
    {
        private Button _continueButton;

        public AccountPickerView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _continueButton = this.FindControl<Button>("continueButton");
        }

        public void SetFocus()
        {
            if (!(DataContext is AccountPickerViewModel vm))
            {
                return;
            }

            // TODO: set focus
        }

        private const string MsaGeometryData = "M13.44,36h1.92a8.64,8.64,0,1,1,17.28,0h1.92a10.573,10.573,0,0,0-6.569-9.771,7.68,7.68,0,1,0-7.982,0A10.573,10.573,0,0,0,13.44,36Zm4.8-16.32A5.76,5.76,0,1,1,24,25.44,5.766,5.766,0,0,1,18.24,19.68Z";
        private const string AadGeometryData = "M32.5,14A1.492,1.492,0,0,1,34,15.5V38.5A1.494,1.494,0,0,1,32.5,40h-17A1.494,1.494,0,0,1,14,38.5v-23A1.494,1.494,0,0,1,15.5,14h4.873l-3-6h2.25l3,6h2.751l3-6h2.25l-3,6ZM32,16H23.623l1.266,2.546A1.13,1.13,0,0,1,25,19a1.009,1.009,0,0,1-1,1,1,1,0,0,1-.534-.149.974.974,0,0,1-.368-.4L21.375,16H16v22H32ZM20,26a3.92,3.92,0,0,1,.312-1.555,4.023,4.023,0,0,1,2.133-2.133,4.041,4.041,0,0,1,3.109,0,4.014,4.014,0,0,1,2.133,2.133A3.886,3.886,0,0,1,28,26a3.937,3.937,0,0,1-.288,1.485,3.987,3.987,0,0,1-.8,1.266A5.7,5.7,0,0,1,28.2,29.7a5.907,5.907,0,0,1,.968,1.251,6.388,6.388,0,0,1,.616,1.461A5.786,5.786,0,0,1,30,34H28a3.877,3.877,0,0,0-.312-1.554,4,4,0,0,0-2.133-2.133,4.011,4.011,0,0,0-3.109,0,4.023,4.023,0,0,0-2.133,2.133A3.912,3.912,0,0,0,20,33.995H18a5.786,5.786,0,0,1,.218-1.586,6.388,6.388,0,0,1,.616-1.461A5.933,5.933,0,0,1,19.8,29.7a5.694,5.694,0,0,1,1.288-.951,3.991,3.991,0,0,1-.8-1.267A3.945,3.945,0,0,1,20,26Zm6,0a1.92,1.92,0,0,0-.157-.781,2.039,2.039,0,0,0-1.061-1.062,2.024,2.024,0,0,0-1.563,0,2.048,2.048,0,0,0-1.061,1.062,2.021,2.021,0,0,0,0,1.562,2.042,2.042,0,0,0,1.061,1.061,2.024,2.024,0,0,0,1.563,0,2.032,2.032,0,0,0,1.061-1.061A1.927,1.927,0,0,0,26,26Z";
        private Geometry _aadIconGeometry;
        private Geometry _msaIconGeometry;
        public Geometry AadIconGeometry => _aadIconGeometry ??= PathGeometry.Parse(AadGeometryData);
        public Geometry MsaIconGeometry => _msaIconGeometry ??= PathGeometry.Parse(MsaGeometryData);
    }
}
