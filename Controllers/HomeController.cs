using coursify;
using Coursify.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coursify.Controllers
{
    public class HomeController : Controller
    {
        private readonly CoursifyDbContext _context;

        public HomeController(CoursifyDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId != null && userRole != null)
            {
                // Redirect based on role
                if (userRole == "Student")
                    return RedirectToAction("Dashboard", "Student");
                else if (userRole == "Teacher")
                    return RedirectToAction("Dashboard", "Teacher");
                else if (userRole == "Admin")
                    return RedirectToAction("Dashboard", "Admin");
            }

            // Show public courses for non-logged in users
            var courses = _context.Courses
                .Where(c => c.IsPublished)
                .Take(6)
                .ToList();

            return View(courses);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }
    }
}