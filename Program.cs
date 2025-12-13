using Microsoft.EntityFrameworkCore;
using Coursify.Models;
using coursify;

var builder = WebApplication.CreateBuilder(args);

// Database Connection yahan add karein
builder.Services.AddDbContext<CoursifyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CoursifyDbContext>();
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

app.Run();