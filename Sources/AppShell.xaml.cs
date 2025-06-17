using MedicalScanner.Views;

namespace MedicalScanner
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(TemperaturePage), typeof(TemperaturePage));
        }
    }
}
