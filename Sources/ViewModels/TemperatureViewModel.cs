using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System.ComponentModel;
using System.Data;

namespace MedicalScanner.ViewModels;

public partial class TemperatureViewModel : ObservableObject, IDisposable
{
    private readonly IDevice _device;
    private readonly ICharacteristic _temperatureCharacteristic;
    private readonly IService _temperatureService;
    private readonly IAdapter _adapter;
    private bool _disposed;

    [ObservableProperty]
    private string deviceName;

    [ObservableProperty]
    private string deviceId;

    [ObservableProperty]
    private string connectionStatus = "Connected";

    [ObservableProperty]
    private float temperatureValue;

    [ObservableProperty]
    private DateTime lastUpdateTime = DateTime.Now;

    public TemperatureViewModel(IDevice device, IService temperatureService,
                               ICharacteristic temperatureCharacteristic, IAdapter adapter)
    {
        _device = device;
        _temperatureService = temperatureService;
        _temperatureCharacteristic = temperatureCharacteristic;
        _adapter = adapter;

        deviceName = device.Name ?? "Unknown Device";
        deviceId = device.Id.ToString();

        // Subscribe to value updates
        _temperatureCharacteristic.ValueUpdated += OnTemperatureUpdated;

        // Start notifications
        MainThread.BeginInvokeOnMainThread(async () => {
            try
            {
                await _temperatureCharacteristic.StartUpdatesAsync();
                connectionStatus = "Monitoring temperature...";
            }
            catch (Exception ex)
            {
                connectionStatus = $"Error: {ex.Message}";
            }
        });

        // Subscribe to connection state changes
        _adapter.DeviceConnectionLost += OnDeviceConnectionLost;
        _adapter.DeviceDisconnected += OnDeviceDisconnected;
    }

    private void OnDeviceConnectionLost(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceErrorEventArgs e)
    {
        if (e.Device?.Id == _device.Id)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                connectionStatus = $"Device connection lost: {_device.State}";
            });
        }
    }

    private void OnDeviceDisconnected(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
    {
        if (e.Device?.Id == _device.Id)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                connectionStatus = $"Device disconnected: {_device.State}";
            });
        }
    }

    private void OnTemperatureUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
    {
        var data = e.Characteristic.Value;
        if (data.Length >= 5) // IEEE-11073 32-bit float
        {
            // Extract temperature (IEEE-11073 format)
            // Skip first byte (flags) and read temperature value
            float temperature = BitConverter.ToSingle(data, 1);

            MainThread.BeginInvokeOnMainThread(() => {
                temperatureValue = temperature;
                lastUpdateTime = DateTime.Now;
                connectionStatus = "Receiving data";
            });
        }
    }

    [RelayCommand]
    private async Task Disconnect()
    {
        try
        {
            // Stop notifications first
            await _temperatureCharacteristic.StopUpdatesAsync();

            // Disconnect from device
            await _adapter.DisconnectDeviceAsync(_device);

            // Navigate back to main page
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to disconnect: {ex.Message}", "OK");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        // Clean up subscriptions and connection
        _temperatureCharacteristic.ValueUpdated -= OnTemperatureUpdated;
        _adapter.DeviceConnectionLost -= OnDeviceConnectionLost;
        _adapter.DeviceDisconnected -= OnDeviceDisconnected;

        // Stop notifications if still connected
        if (_device.State == Plugin.BLE.Abstractions.DeviceState.Connected)
        {
            MainThread.BeginInvokeOnMainThread(async () => {
                try
                {
                    await _temperatureCharacteristic.StopUpdatesAsync();
                }
                catch { /* Ignore exceptions during cleanup */ }
            });
        }

        _disposed = true;
    }
}