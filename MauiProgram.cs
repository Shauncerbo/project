using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using project.Data;
using project.Pages;
using project.Services;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace project
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureMauiHandlers(handlers =>
                {
                    handlers.AddHandler(typeof(CameraBarcodeReaderView), typeof(CameraBarcodeReaderViewHandler));
                    handlers.AddHandler(typeof(CameraBarcodeGeneratorView), typeof(CameraBarcodeGeneratorViewHandler));
                    handlers.AddHandler(typeof(CameraView), typeof(CameraViewHandler));
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddMauiBlazorWebView();

#if WINDOWS
            Microsoft.Maui.Handlers.ViewHandler.ViewMapper.AppendToMapping("CameraPermission", (handler, view) =>
            {
                if (handler.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 webView2)
                {
                    webView2.CoreWebView2Initialized += (sender, args) =>
                    {
                        try
                        {
                            if (webView2.CoreWebView2 != null)
                            {
                                webView2.CoreWebView2.Settings.IsWebMessageEnabled = true;
                                webView2.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                                webView2.CoreWebView2.Settings.IsStatusBarEnabled = false;

                                webView2.CoreWebView2.PermissionRequested += (s, e) =>
                                {
                                    if (e.PermissionKind == Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Camera ||
                                        e.PermissionKind == Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Microphone)
                                    {
                                        e.State = Microsoft.Web.WebView2.Core.CoreWebView2PermissionState.Allow;
                                        System.Diagnostics.Debug.WriteLine($"✅ WebView2 Permission granted: {e.PermissionKind}");
                                    }
                                };

                                System.Diagnostics.Debug.WriteLine("🎯 WebView2 camera permission handler configured successfully");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ WebView2 configuration error: {ex.Message}");
                        }
                    };

                    try
                    {
                        var _ = webView2.CoreWebView2;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ WebView2 initialization note: {ex.Message}");
                    }
                }
            });
#endif

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAttendanceService, AttendanceService>();
            builder.Services.AddScoped<IMemberService, MemberService>();
            builder.Services.AddSingleton<INativeNavigationService, NativeNavigationService>();
            builder.Services.AddTransient<AttendanceScannerPage>();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer("Data Source=LAPTOP-3VCGD3TV\\SQLEXPRESS;Initial Catalog=GymCRM_DB;Integrated Security=True;Trust Server Certificate=True"));

            return builder.Build();
        }
    }
}


