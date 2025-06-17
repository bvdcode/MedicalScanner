using MedicalScanner.Views;
using CommunityToolkit.Maui.Extensions;

namespace MedicalScanner.Services;

public class LoadingService
{
    private LoadingPopup? _loadingPopup;
    private bool _isShowing = false;

    public void ShowLoading(string message)
    {
        if (_isShowing)
        {
            return;
        }
        _isShowing = true;

        MainThread.InvokeOnMainThreadAsync(() =>
        {
            _loadingPopup = new LoadingPopup(message);
            Application.Current?.MainPage?.ShowPopup(_loadingPopup);
        });
    }

    public void HideLoading()
    {
        _isShowing = false;

        MainThread.InvokeOnMainThreadAsync(() =>
        {
            _loadingPopup?.CloseAsync();
            _loadingPopup = null;
        });
    }
}