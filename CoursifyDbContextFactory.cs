using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace coursify
{
    public class CoursifyDbContextFactory : IDesignTimeDbContextFactory<CoursifyDbContext>
    {
        public CoursifyDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CoursifyDbContext>();

            optionsBuilder.UseNpgsql(
                "Host=ep-bitter-glade-ahtcfdiy-pooler.c-3.us-east-1.aws.neon.tech;" +
                "Database=neondb;" +
                "Username=neondb_owner;" +
                "Password=npg_8CeVZWMufb1S;" +
                "SSL Mode=Require;" +
                "Trust Server Certificate=true;"
            );

            return new CoursifyDbContext(optionsBuilder.Options);
        }
    }
}
