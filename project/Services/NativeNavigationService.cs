using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using project.Pages;

namespace project.Services;

public class NativeNavigationService : INativeNavigationService
{
    public event EventHandler? ScannerClosed;

    public async Task ShowNativeScannerAsync()
    {
        try
        {
            var scannerPage = Application.Current?.Handler?.MauiContext?.Services?.GetService<AttendanceScannerPage>();
            if (scannerPage != null)
            {
                var navigation = Application.Current?.MainPage?.Navigation;
                if (navigation != null)
                {
                    await navigation.PushAsync(scannerPage);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Warning: AttendanceScannerPage service not found. Make sure it's registered in MauiProgram.cs");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing native scanner: {ex.Message}");
        }
    }

    public void NotifyScannerClosed()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ScannerClosed?.Invoke(this, EventArgs.Empty);
        });
    }
}

