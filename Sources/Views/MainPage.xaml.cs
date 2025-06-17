using MedicalScanner.Services;
using MedicalScanner.ViewModels;

namespace MedicalScanner.Views
{
    public partial class MainPage : ContentPage
    {
        public MainPage(LoadingService loadingService)
        {
            InitializeComponent();
            BindingContext = new MainViewModel(loadingService);
        }
    }
}
