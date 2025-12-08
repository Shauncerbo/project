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
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IDatabaseSyncService, DatabaseSyncService>();
            builder.Services.AddHttpClient<IPayMongoService, PayMongoService>();
            builder.Services.AddSingleton<IToastService, ToastService>();
            builder.Services.AddSingleton<INativeNavigationService, NativeNavigationService>();
            builder.Services.AddTransient<AttendanceScannerPage>();
            
            // Email notification services
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddHostedService<ExpirationNotificationService>();

            // Database configuration - Read from SecureStorage (defaults to local if not set)
            // Local SQL Server Express Database
            const string localConnectionString = 
                "Data Source=LAPTOP-3VCGD3TV\\SQLEXPRESS;Initial Catalog=GymCRM_DB;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";

            // MonsterASP.net Cloud Database (for production/online)
            // Remote access connection string from MonsterASP.net dashboard
            const string monsterAspConnectionString =
                "Server=db32884.public.databaseasp.net;Database=db32884;User Id=db32884;Password=P_y79xY!kQ%6;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connection Timeout=60;";

            // Get database type from SecureStorage (defaults to local)
            string connectionString;
            try
            {
                var dbType = SecureStorage.Default.GetAsync("database_type").GetAwaiter().GetResult();
                connectionString = (dbType == "online") ? monsterAspConnectionString : localConnectionString;
            }
            catch
            {
                // Default to local if SecureStorage is not available or not set
                connectionString = localConnectionString;
            }

            // Register EF Core DbContext (scoped) and factory (for background/off-thread usage)
            void ConfigureDbContext(DbContextOptionsBuilder options)
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });
            }

            builder.Services.AddDbContext<AppDbContext>(ConfigureDbContext);
            builder.Services.AddDbContextFactory<AppDbContext>(ConfigureDbContext);

            

            var app = builder.Build();

            // DISABLED: Automatic database initialization on startup
            // Database and tables must be created manually using SQL scripts
            /*
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
                    // Run synchronously with timeout to prevent hanging
                    var initTask = initializer.InitializeAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                    var completedTask = Task.WhenAny(initTask, timeoutTask).GetAwaiter().GetResult();
                    
                    if (completedTask == initTask)
                    {
                        initTask.GetAwaiter().GetResult(); // Task completed within timeout
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ Database initialization timed out, continuing anyway...");
                    }
                }
            }
            catch (Exception ex)
            {
                // Show error in console and debug output
                System.Diagnostics.Debug.WriteLine($"❌ Failed to initialize database: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                // Continue app startup even if database init fails
            }
            */

            return app;
        }
    }
}
