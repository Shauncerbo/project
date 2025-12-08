using Microsoft.EntityFrameworkCore;

namespace project.Data.Context
{
    /// <summary>
    /// Online / MonsterASP SQL Server context (separate DbContext, optional use).
    /// Currently not used by the app, provided only to match your sample structure.
    /// </summary>
    public class OnlineAppContext : DbContext
    {
        public OnlineAppContext(DbContextOptions<OnlineAppContext> options)
            : base(options)
        {
        }
    }
}


