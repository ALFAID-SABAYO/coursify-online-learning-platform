using Microsoft.AspNetCore.Mvc;
using Coursify.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using coursify;

namespace Coursify.Controllers
{
    public class AccountController : Controller
    {
        private readonly CoursifyDbContext _context;

        public AccountController(CoursifyDbContext context)
        {
            _context = context;
        }

        // Register
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user, string password, string role)
        {
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Role");

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var emailExists = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (emailExists)
            {
                ViewBag.Error = "Email already registered. Please log in instead.";
                return View(user);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.Role = role;
            user.CreatedDate = DateTime.UtcNow;
            user.IsActive = true;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Set session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.Name);

            // Redirect based on role
            if (user.Role == "Student")
                return RedirectToAction("Dashboard", "Student");
            else if (user.Role == "Teacher")
                return RedirectToAction("Dashboard", "Teacher");
            else
                return RedirectToAction("Index", "Home");
        }

        // Login
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("UserName", user.Name);

                if (user.Role == "Student")
                    return RedirectToAction("Dashboard", "Student");
                else if (user.Role == "Teacher")
                    return RedirectToAction("Dashboard", "Teacher");
                else if (user.Role == "Admin")
                    return RedirectToAction("Dashboard", "Admin");
            }

            ViewBag.Error = "Invalid email or password";
            return View();
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}