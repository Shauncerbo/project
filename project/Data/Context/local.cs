using Microsoft.EntityFrameworkCore;

namespace project.Data.Context
{
    /// <summary>
    /// Local SQL Server context (separate DbContext, optional use).
    /// Currently not used by the app, provided only to match your sample structure.
    /// </summary>
    public class LocalAppContext : DbContext
    {
        public LocalAppContext(DbContextOptions<LocalAppContext> options)
            : base(options)
        {
        }
    }
}


