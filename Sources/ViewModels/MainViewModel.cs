using Plugin.BLE;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MedicalScanner.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAdapter _adapter;
    private readonly List<IDevice> _devices;

    private string _outputText = "Press Scan to start.";
    public string OutputText
    {
        get => _outputText;
        set => SetProperty(ref _outputText, value);
    }

    private bool _isScanning;
    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            if (SetProperty(ref _isScanning, value))
                OnPropertyChanged(nameof(ScanButtonText));
        }
    }

    public string ScanButtonText => IsScanning ? "⏹ Stop Scanning" : "🔍 Scan Devices";

    public IRelayCommand ScanCommand { get; }

    public MainViewModel()
    {
        _devices = [];
        _adapter = CrossBluetoothLE.Current.Adapter;
        _adapter.DeviceDiscovered += OnDeviceDiscovered;
        _adapter.ScanTimeoutElapsed += (_, _) => StopScanUIUpdate();
        ScanCommand = new RelayCommand(async () => await ToggleScanAsync());
    }

    private async Task ToggleScanAsync()
    {
        if (IsScanning)
        {
            await _adapter.StopScanningForDevicesAsync();
            _devices.Clear();
            StopScanUIUpdate();
            return;
        }

        if (!CrossBluetoothLE.Current.IsOn)
        {
            await Shell.Current.DisplayAlert("Bluetooth Disabled", "Please enable Bluetooth to scan for devices.", "OK");
            return;
        }

        if (!await CheckPermissionsAsync())
            return;

        _devices.Clear();
        OutputText = "Scanning for Low-Energy devices...";
        IsScanning = true;

        await _adapter.StartScanningForDevicesAsync();
    }

    private void StopScanUIUpdate()
    {
        IsScanning = false;
        OnPropertyChanged(nameof(ScanButtonText));
    }

    private static async Task<bool> CheckPermissionsAsync()
    {
        var location = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (location != PermissionStatus.Granted)
        {
            location = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (location != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Permission Required", "Location permission is needed for Bluetooth scanning.", "OK");
                return false;
            }
        }

        var bt = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
        if (bt != PermissionStatus.Granted)
        {
            bt = await Permissions.RequestAsync<Permissions.Bluetooth>();
            if (bt != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Permission Required", "Bluetooth permission is needed for Bluetooth scanning.", "OK");
                return false;
            }
        }

        return true;
    }

    private void OnDeviceDiscovered(object? sender, DeviceEventArgs args)
    {
        if (_devices.Any(x => x.Id == args.Device.Id))
            return;

        _devices.Add(args.Device);

        StringBuilder sb = new();
        foreach (var device in _devices.OrderByDescending(x => x.Rssi))
        {
            string name = string.IsNullOrWhiteSpace(device.Name) ? "[Unnamed]" : device.Name;
            sb.AppendLine($"{GetSignalLevel(device.Rssi)}: {name}");
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            OutputText = sb.ToString();
        });
    }
    private static string GetSignalLevel(int rssi)
    {
        if (rssi >= -60)
        {
            return "◉◉◉◉◉";
        }
        if (rssi >= -70)
        {
            return "◉◉◉◉○";
        }
        if (rssi >= -80)
        {
            return "◉◉◉○○";
        }
        if (rssi >= -90)
        {
            return "◉◉○○○";
        }
        if (rssi >= -100)
        {
            return "◉○○○○";
        }
        return "○○○○○";
    }
}
