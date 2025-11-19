using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using project.Pages;

namespace project.Services
{
    public class NativeNavigationService : INativeNavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        public event EventHandler? ScannerClosed;

        public NativeNavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task ShowNativeScannerAsync()
        {
#if WINDOWS || ANDROID || IOS || MACCATALYST
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var scannerPage = _serviceProvider.GetRequiredService<AttendanceScannerPage>();

                if (Application.Current?.MainPage is not NavigationPage navigationPage)
                {
                    var current = Application.Current?.MainPage;
                    if (current == null)
                    {
                        return;
                    }

                    Application.Current.MainPage = new NavigationPage(current);
                    navigationPage = Application.Current.MainPage as NavigationPage;
                }

                if (navigationPage == null)
                {
                    return;
                }

                // Avoid stacking multiple scanner pages
                if (navigationPage.Navigation.NavigationStack.OfType<AttendanceScannerPage>().Any())
                {
                    return;
                }

                await navigationPage.Navigation.PushAsync(scannerPage);
            });
#else
            return Task.CompletedTask;
#endif
        }

        public void NotifyScannerClosed()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ScannerClosed?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}


