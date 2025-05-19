using Plugin.BLE;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MedicalScanner.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public string ScanButtonText => IsScanning ? "⏹ Stop Scanning" : "🔍 Scan Devices";
    public ObservableCollection<IDevice> Devices { get; } = [];
    public IRelayCommand ScanCommand { get; }
    private readonly IAdapter _adapter;
    private bool _isScanning;


    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            if (SetProperty(ref _isScanning, value))
            {
                OnPropertyChanged(nameof(ScanButtonText));
            }
        }
    }

    public MainViewModel()
    {
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
            StopScanUIUpdate();
            return;
        }

        if (!CrossBluetoothLE.Current.IsOn)
        {
            await Shell.Current.DisplayAlert("Bluetooth Disabled", "Please enable Bluetooth to scan for devices.", "OK");
            return;
        }

        if (!await CheckPermissionsAsync())
        {
            return;
        }

        Devices.Clear();
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
        if (Devices.Any(x => x.Id == args.Device.Id))
        {
            return;
        }
        var device = args.Device;
        int insertIndex = 0;
        while (insertIndex < Devices.Count && Devices[insertIndex].Rssi > device.Rssi)
        {
            insertIndex++;
        }
        Devices.Insert(insertIndex, device);
    }
}
