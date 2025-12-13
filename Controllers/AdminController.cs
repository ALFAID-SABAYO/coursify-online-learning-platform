using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using coursify;

namespace Coursify.Controllers
{
    public class AdminController : Controller
    {
        private readonly CoursifyDbContext _context;

        public AdminController(CoursifyDbContext context)
        {
            _context = context;
        }

        // Admin Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalStudents = await _context.Users.CountAsync(u => u.Role == "Student");
            var totalTeachers = await _context.Users.CountAsync(u => u.Role == "Teacher");
            var totalCourses = await _context.Courses.CountAsync();
            var totalEnrollments = await _context.Enrollments.CountAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalStudents = totalStudents;
            ViewBag.TotalTeachers = totalTeachers;
            ViewBag.TotalCourses = totalCourses;
            ViewBag.TotalEnrollments = totalEnrollments;

            return View();
        }

        // Manage Users
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // Delete User
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageUsers");
        }

        // Manage Courses
        public async Task<IActionResult> ManageCourses()
        {
            var courses = await _context.Courses
                .Include(c => c.Teacher)
                .ToListAsync();

            return View(courses);
        }

        // Delete Course
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageCourses");
        }

        // View All Statistics
        public async Task<IActionResult> ViewStats()
        {
            var totalRevenue = await _context.Enrollments.SumAsync(e => e.AmountPaid);
            var monthlyRevenue = await _context.Enrollments
                .Where(e => e.PurchaseDate.Month == DateTime.Now.Month)
                .SumAsync(e => e.AmountPaid);

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.MonthlyRevenue = monthlyRevenue;

            return View();
        }

        // Toggle Course Status
        public async Task<IActionResult> ToggleCourseStatus(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                course.IsPublished = !course.IsPublished;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageCourses");
        }

        // Toggle User Status
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageUsers");
        }
    }
}