using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Text;

namespace MedicalScanner
{
    public partial class MainPage : ContentPage
    {
        private readonly IAdapter _adapter;
        private readonly List<IDevice> _devices;

        public MainPage()
        {
            InitializeComponent();
            _devices = [];
            _adapter = CrossBluetoothLE.Current.Adapter;
            _adapter.DeviceDiscovered += OnDeviceDiscovered;
        }

        private async void OnToggleBtnClicked(object? sender, EventArgs e)
        {
            if (_adapter.IsScanning)
            {
                await _adapter.StopScanningForDevicesAsync();
                _devices.Clear();
            }
            if (!CrossBluetoothLE.Current.IsOn)
            {
                await DisplayAlert("Bluetooth Disabled", "Please enable Bluetooth to scan for devices.", "OK");
                return;
            }
            if (_devices.Count == 0)
            {
                OutputLabel.Text = "Scanning for Low-Energy devices...";
            }
            if (!await CheckPermissionsAsync())
            {
                return;
            }
            await _adapter.StartScanningForDevicesAsync();
        }

        private async Task<bool> CheckPermissionsAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Required", "Location permission is needed for Bluetooth scanning.", "OK");
                    return false;
                }
            }
            status = await Permissions.CheckStatusAsync<Permissions.NearbyWifiDevices>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.NearbyWifiDevices>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Required", "Bluetooth permission is needed for Bluetooth scanning.", "OK");
                    return false;
                }
            }
            return true;
        }

        private void OnDeviceDiscovered(object? sender, DeviceEventArgs args)
        {
            if (_devices.Any(x => x.Id == args.Device.Id))
            {
                return;
            }
            _devices.Add(args.Device);

            StringBuilder sb = new();
            foreach (var device in _devices.OrderByDescending(x => x.Rssi))
            {
                string name = string.IsNullOrWhiteSpace(device.Name) ? "[Unnamed]" : device.Name;
                sb.AppendLine($"{GetSignalLevel(device.Rssi)}: {name}");
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OutputLabel.Text = sb.ToString();
            });
        }

        private static string GetSignalLevel(int rssi)
        {
            if (rssi >= -60)
            {
                return "◉◉◉◉◉";
            }
            else if (rssi >= -70)
            {
                return "◉◉◉◉○";
            }
            else if (rssi >= -80)
            {
                return "◉◉◉○○";
            }
            else if (rssi >= -90)
            {
                return "◉◉○○○";
            }
            else if (rssi >= -100)
            {
                return "◉○○○○";
            }
            else
            {
                return "○○○○○";
            }
        }
    }
}
