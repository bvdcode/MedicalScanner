using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;

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
    private float temperatureFValue;

    [ObservableProperty]
    private DateTime lastUpdateTime = DateTime.Now;

    public TemperatureViewModel(IDevice device, IService temperatureService,
                               ICharacteristic temperatureCharacteristic, IAdapter adapter)
    {
        _device = device;
        _temperatureService = temperatureService;
        _temperatureCharacteristic = temperatureCharacteristic;
        _adapter = adapter;

        DeviceName = device.Name ?? "Unknown Device";
        DeviceId = device.Id.ToString();

        // Subscribe to value updates
        _temperatureCharacteristic.ValueUpdated += OnTemperatureUpdated;

        // Start notifications
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Force read the current value to verify connection
                var initialValue = await _temperatureCharacteristic.ReadAsync();
                Debug.WriteLine($"Initial characteristic value: {BitConverter.ToString(initialValue.data)}");

                // Enable notifications
                await _temperatureCharacteristic.StartUpdatesAsync();
                ConnectionStatus = "Monitoring temperature...";
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Error: {ex.Message}";
                Debug.WriteLine($"Error starting updates: {ex}");
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
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectionStatus = $"Device connection lost: {_device.State}";
            });
        }
    }

    private void OnDeviceDisconnected(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
    {
        if (e.Device?.Id == _device.Id)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectionStatus = $"Device disconnected: {_device.State}";
            });
        }
    }

    private void OnTemperatureUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
    {
        var data = e.Characteristic.Value;
        Debug.WriteLine($"Received temperature data: {BitConverter.ToString(data)}");

        try
        {
            float temperature;

            // Depending on your device's format, use one of these parsing approaches:

            // Option 1: Standard IEEE-11073 format (as in original code)
            if (data.Length >= 5)
            {
                temperature = BitConverter.ToSingle(data, 1);
            }
            // Option 2: Direct 4-byte IEEE-754 float
            else if (data.Length >= 4)
            {
                temperature = BitConverter.ToSingle(data, 0);
            }
            // Option 3: Two byte integer with scaling (common for many BLE sensors)
            else if (data.Length >= 2)
            {
                short rawTemp = BitConverter.ToInt16(data, 0);
                temperature = rawTemp / 100.0f; // Scale factor depends on your device
            }
            // Option 4: Single byte integer
            else if (data.Length >= 1)
            {
                temperature = data[0];
            }
            else
            {
                Debug.WriteLine("Temperature data too short");
                return;
            }

            Debug.WriteLine($"Parsed temperature: {temperature}°C");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TemperatureValue = temperature;
                TemperatureFValue = (temperature * 9 / 5) + 32; // Convert to Fahrenheit
                LastUpdateTime = DateTime.Now;
                ConnectionStatus = "Receiving data";
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error parsing temperature data: {ex}");
        }
    }

    [RelayCommand]
    private async Task Disconnect()
    {
        // Create a cancellation token source with 5 second timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            // Try to perform a proper disconnect with timeout
            Task disconnectTask = DisconnectDeviceAsync(cts.Token);

            // Either complete normally or timeout
            await Task.WhenAny(disconnectTask, Task.Delay(5000, cts.Token));

            // Navigate regardless of disconnect result
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Disconnect error: {ex.Message}");
            // Navigate anyway even if there was an error
            await Shell.Current.GoToAsync("..");
        }
    }

    private async Task DisconnectDeviceAsync(CancellationToken token)
    {
        try
        {
            // Stop notifications first
            await _temperatureCharacteristic.StopUpdatesAsync().WaitAsync(token);

            // Disconnect from device
            await _adapter.DisconnectDeviceAsync(_device).WaitAsync(token);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Disconnect operation timed out");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during disconnect: {ex.Message}");
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
            MainThread.BeginInvokeOnMainThread(async () =>
            {
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