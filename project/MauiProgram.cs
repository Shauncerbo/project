using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;


using project.Data;

namespace project
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });


            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer("Data Source=LAPTOP-3VCGD3TV\\SQLEXPRESS;Initial Catalog=GymCRM_DB;Integrated Security=True;Trust Server Certificate=True"));

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
