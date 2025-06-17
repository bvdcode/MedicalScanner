using MedicalScanner.ViewModels;

namespace MedicalScanner.Views;

public partial class TemperaturePage : ContentPage
{
    private readonly TemperatureViewModel _viewModel;

    public TemperaturePage(TemperatureViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Dispose();
    }
}