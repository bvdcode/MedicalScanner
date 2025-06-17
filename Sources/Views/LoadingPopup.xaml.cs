using CommunityToolkit.Maui.Views;

namespace MedicalScanner.Views;

public partial class LoadingPopup : Popup
{
    public LoadingPopup(string message)
    {
        InitializeComponent();
        MessageLabel.Text = message;
    }
}