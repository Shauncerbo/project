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
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace project
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader()
                .ConfigureMauiHandlers(handlers =>
                {
                    handlers.AddHandler(typeof(CameraBarcodeReaderView), typeof(CameraBarcodeReaderViewHandler));
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddMauiBlazorWebView();

#if WINDOWS
            // Enhanced WebView2 Camera Permission Handler for newer WebView2 versions
            Microsoft.Maui.Handlers.ViewHandler.ViewMapper.AppendToMapping("CameraPermission", (handler, view) =>
            {
                if (handler.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 webView2)
                {
                    // For newer WebView2 versions, use CoreWebView2Initialized event
                    webView2.CoreWebView2Initialized += (sender, args) =>
                    {
                        try
                        {
                            if (webView2.CoreWebView2 != null)
                            {
                                // Enable all necessary WebView2 features
                                webView2.CoreWebView2.Settings.IsWebMessageEnabled = true;
                                webView2.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                                webView2.CoreWebView2.Settings.IsStatusBarEnabled = false;
                                
                                // Auto-grant camera and microphone permissions
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

                    // Handle WebView2 creation (for older versions compatibility)
                    try
                    {
                        // Ensure CoreWebView2 is initialized
                        var _ = webView2.CoreWebView2; // This might trigger initialization
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

            // Register services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAttendanceService, AttendanceService>();
            builder.Services.AddScoped<IMemberService, MemberService>();
            builder.Services.AddSingleton<INativeNavigationService, NativeNavigationService>();
            builder.Services.AddTransient<AttendanceScannerPage>();

            // Database configuration - Local SQL Server
            const string localConnectionString =
                "Data Source=LAPTOP-3VCGD3TV\\SQLEXPRESS;Initial Catalog=GymCRM_DB;Integrated Security=True;Trust Server Certificate=True";

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(localConnectionString));

            // Register Database Initializer
            builder.Services.AddScoped<DatabaseInitializer>();

            var app = builder.Build();

            // Initialize database on startup - wait for it to complete
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
                    // Run synchronously to ensure it completes before app starts
                    initializer.InitializeAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                // Show error in console and debug output
                System.Diagnostics.Debug.WriteLine($"❌ Failed to initialize database: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
                // Continue app startup even if database init fails
            }

            return app;
        }
    }
}
