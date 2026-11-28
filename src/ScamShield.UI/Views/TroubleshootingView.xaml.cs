using System.Windows.Controls;
using ScamShield.UI.ViewModels;

namespace ScamShield.UI.Views
{
    public partial class TroubleshootingView : UserControl
    {
        public TroubleshootingView()
        {
            InitializeComponent();
            DataContext = new TroubleshootingViewModel();
        }
    }
}