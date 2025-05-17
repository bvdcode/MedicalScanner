using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace MedicalScanner
{
    public partial class MainPage : ContentPage
    {
        private readonly IAdapter _adapter;

        public MainPage()
        {
            InitializeComponent();

            _adapter = CrossBluetoothLE.Current.Adapter;
            _adapter.DeviceDiscovered += OnDeviceDiscovered;
        }

        private async void OnCounterClicked(object? sender, EventArgs e)
        {
            OutputLabel.Text = "Scanning for Low-Energy devices...";
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Required", "Location permission is needed for Bluetooth scanning.", "OK");
                    return;
                }
            }
            await _adapter.StartScanningForDevicesAsync();
        }

        private void OnDeviceDiscovered(object? sender, DeviceEventArgs args)
        {
            var name = string.IsNullOrWhiteSpace(args.Device.Name) ? "[Unnamed]" : args.Device.Name;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OutputLabel.Text += $"\n{name}";
            });
        }
    }
}
