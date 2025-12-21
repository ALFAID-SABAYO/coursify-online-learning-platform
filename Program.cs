using Microsoft.EntityFrameworkCore;
using Coursify.Models;
using coursify;

var builder = WebApplication.CreateBuilder(args);

// Database Connection with timeout configuration
builder.Services.AddDbContext<CoursifyDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(120); // 2 minutes timeout
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
    
    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});

// Baaki code jo pehle tha...
builder.Services.AddDistributedMemoryCache(); // session ke liye in-memory store
builder.Services.AddSession();
builder.Services.AddControllersWithViews();

var app = builder.Build();
// ... baaki code

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configure static files with MIME types for videos
var staticFileOptions = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.Name.ToLowerInvariant();
        if (path.EndsWith(".mp4"))
        {
            ctx.Context.Response.ContentType = "video/mp4";
        }
        else if (path.EndsWith(".webm"))
        {
            ctx.Context.Response.ContentType = "video/webm";
        }
        else if (path.EndsWith(".ogg"))
        {
            ctx.Context.Response.ContentType = "video/ogg";
        }
        else if (path.EndsWith(".mov"))
        {
            ctx.Context.Response.ContentType = "video/quicktime";
        }
        else if (path.EndsWith(".avi"))
        {
            ctx.Context.Response.ContentType = "video/x-msvideo";
        }
    }
};

app.UseStaticFiles(staticFileOptions);
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Apply migrations and create admin
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<CoursifyDbContext>();
        
        // Test database connection
        if (db.Database.CanConnect())
        {
            db.Database.Migrate();

            // Create admin if not exists
            if (!db.Users.Any(u => u.Role == "Admin"))
            {
                var admin = new User
                {
                    Name = "Admin",
                    Email = "admin@coursify.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role = "Admin",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                db.Users.Add(admin);
                db.SaveChanges();
            }
        }
        else
        {
            Console.WriteLine("Warning: Cannot connect to database. Please check your connection string in appsettings.json");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization error: {ex.Message}");
        Console.WriteLine("Please verify your PostgreSQL connection string in appsettings.json");
        Console.WriteLine("Connection string format: Host=your-host;Port=5432;Database=your-db;Username=your-user;Password=your-password;SslMode=Require;");
    }
}

app.Run();