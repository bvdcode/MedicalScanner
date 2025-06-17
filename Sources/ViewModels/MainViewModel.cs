using Plugin.BLE;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using CommunityToolkit.Mvvm.ComponentModel;
using MedicalScanner.Views;

namespace MedicalScanner.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public string ScanButtonText => IsScanning ? "⏹ Stop Scanning" : "🔍 Scan Devices";
    public ObservableCollection<IDevice> Devices { get; } = [];
    public IRelayCommand<IDevice> ConnectCommand { get; }
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

    private readonly Services.LoadingService _loadingService;

    public MainViewModel(Services.LoadingService loadingService)
    {
        _loadingService = loadingService;
        _adapter = CrossBluetoothLE.Current.Adapter;
        _adapter.DeviceDiscovered += OnDeviceDiscovered;
        _adapter.ScanTimeoutElapsed += (_, _) => StopScanUIUpdate();
        ScanCommand = new RelayCommand(async () => await ToggleScanAsync());
        ConnectCommand = new RelayCommand<IDevice>(async (device) => await ConnectToDevice(device));
    }

    private async Task ConnectToDevice(IDevice? device)
    {
        if (device == null) return;

        // Cancel any ongoing scan
        if (IsScanning)
        {
            await _adapter.StopScanningForDevicesAsync();
            StopScanUIUpdate();
        }

        try
        {
            // Show loading indicator
            _loadingService.ShowLoading($"Connecting to {device.Name ?? "device"}...");

            // Connect with timeout
            var connectTask = _adapter.ConnectToDeviceAsync(device);

            // Create a timeout task
            var timeoutTask = Task.Delay(10000); // 10 seconds

            // Wait for either connection or timeout
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // Timed out
                await Shell.Current.DisplayAlert("Timeout", "Connection attempt timed out", "OK");
                return;
            }

            // Get services
            var services = await device.GetServicesAsync();

            // Health Thermometer service UUID
            const string HEALTH_THERMOMETER_SERVICE = "1809";
            const string TEMPERATURE_CHARACTERISTIC = "2A1C";

            // Look for the temperature service
            var temperatureService = services.FirstOrDefault(s =>
                s.Id.ToString().ToLowerInvariant().Contains(HEALTH_THERMOMETER_SERVICE.ToLowerInvariant()));

            if (temperatureService != null)
            {
                // Found temperature service!
                var characteristics = await temperatureService.GetCharacteristicsAsync();
                var tempCharacteristic = characteristics.FirstOrDefault(c =>
                    c.Id.ToString().ToLowerInvariant().Contains(TEMPERATURE_CHARACTERISTIC.ToLowerInvariant()));

                if (tempCharacteristic != null)
                {
                    // Create the temperature view model
                    var tempViewModel = new TemperatureViewModel(device, temperatureService, tempCharacteristic, _adapter);

                    // Create the page
                    var tempPage = new TemperaturePage(tempViewModel);

                    // Navigate to the temperature page
                    _loadingService.HideLoading();
                    await Shell.Current.Navigation.PushAsync(tempPage);
                    return;
                }
            }

            // If we get here, no temperature service was found or navigation failed
            // Just show generic device info
            StringBuilder sb = new();
            sb.AppendLine($"Name: {device.Name}");
            sb.AppendLine($"ID: {device.Id}");
            sb.AppendLine($"State: {device.State}");
            sb.AppendLine($"Rssi: {device.Rssi}");
            sb.AppendLine($"AdvertisementRecords: {device.AdvertisementRecords.Count}");

            if (services.Count > 0)
            {
                sb.AppendLine($"Device Services: {services.Count}");
                foreach (var service in services)
                {
                    sb.AppendLine($"Service UUID: {service.Id}");
                }
            }

            await Shell.Current.DisplayAlert("Device Info", sb.ToString(), "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Connection Error", $"Failed to connect: {ex.Message}", "OK");
        }
        finally
        {
            // Always hide the loading indicator when done
            _loadingService.HideLoading();
        }
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
        PermissionStatus location = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (location != PermissionStatus.Granted)
        {
            location = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (location != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Permission Required", "Location permission is needed for Bluetooth scanning.", "OK");
                return false;
            }
        }

        PermissionStatus bt = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
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
        IDevice device = args.Device;
        int insertIndex = 0;
        while (insertIndex < Devices.Count && Devices[insertIndex].Rssi > device.Rssi)
        {
            insertIndex++;
        }
        Devices.Insert(insertIndex, device);
    }
}
