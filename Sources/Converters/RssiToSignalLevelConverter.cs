using System.Globalization;

namespace MedicalScanner.Converters;

public class RssiToSignalLevelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int rssi)
        {
            return $"{GetSignalLevel(rssi)} {rssi} dBm";
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static string GetSignalLevel(int rssi)
    {
        const int totalBars = 5;
        const int maxRssi = -60;
        const int minRssi = -100;
        const string full = "◉";
        const string empty = "○";
        int clamped = Math.Clamp(rssi, minRssi, maxRssi);
        int filledBars = (int)Math.Round(((double)(clamped - minRssi) / (maxRssi - minRssi)) * totalBars);
        return new string(full[0], filledBars) + new string(empty[0], totalBars - filledBars);
    }
}