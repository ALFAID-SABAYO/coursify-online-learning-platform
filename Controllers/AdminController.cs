using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using coursify;
using Coursify.Models;

namespace Coursify.Controllers
{
    public class AdminController : Controller
    {
        private readonly CoursifyDbContext _context;

        public AdminController(CoursifyDbContext context)
        {
            _context = context;
        }

        // Check if user is admin
        private bool IsAdmin()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole == "Admin";
        }

        // Admin Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var totalUsers = await _context.Users.CountAsync();
            var totalStudents = await _context.Users.CountAsync(u => u.Role == "Student");
            var totalTeachers = await _context.Users.CountAsync(u => u.Role == "Teacher");
            var totalCourses = await _context.Courses.CountAsync();
            var totalEnrollments = await _context.Enrollments.CountAsync();

            // Calculate total revenue and admin commission
            var totalRevenue = await _context.Enrollments.SumAsync(e => e.AmountPaid);
            var adminCommission = await _context.Transactions
                .Where(t => t.RecipientRole == "Admin")
                .SumAsync(t => t.Amount);

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalStudents = totalStudents;
            ViewBag.TotalTeachers = totalTeachers;
            ViewBag.TotalCourses = totalCourses;
            ViewBag.TotalEnrollments = totalEnrollments;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.AdminCommission = adminCommission;

            return View();
        }

        // Manage Users
        public async Task<IActionResult> ManageUsers()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // Ban/Unban Teacher
        public async Task<IActionResult> BanTeacher(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var teacher = await _context.Users.FindAsync(id);
            if (teacher != null && teacher.Role == "Teacher")
            {
                teacher.IsActive = !teacher.IsActive;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageUsers");
        }

        // Delete User
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // Prevent deleting admin users
                if (user.Role == "Admin")
                {
                    TempData["Error"] = "Cannot delete admin users.";
                    return RedirectToAction("ManageUsers");
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageUsers");
        }

        // Manage Courses
        public async Task<IActionResult> ManageCourses()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var courses = await _context.Courses
                .Include(c => c.Teacher)
                .ToListAsync();

            return View(courses);
        }

        // Delete Course
        public async Task<IActionResult> DeleteCourse(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var course = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course != null)
            {
                // Delete video files
                foreach (var module in course.Modules)
                {
                    foreach (var lesson in module.Lessons)
                    {
                        if (!string.IsNullOrEmpty(lesson.VideoUrl))
                        {
                            var videoPath = Path.Combine("wwwroot", lesson.VideoUrl.TrimStart('/'));
                            if (System.IO.File.Exists(videoPath))
                            {
                                try
                                {
                                    System.IO.File.Delete(videoPath);
                                }
                                catch
                                {
                                    // Log error but continue deletion
                                }
                            }
                        }
                    }
                }

                // Delete thumbnail if exists
                if (!string.IsNullOrEmpty(course.ThumbnailUrl))
                {
                    var thumbPath = Path.Combine("wwwroot", course.ThumbnailUrl.TrimStart('/'));
                    if (System.IO.File.Exists(thumbPath))
                    {
                        try
                        {
                            System.IO.File.Delete(thumbPath);
                        }
                        catch
                        {
                            // Log error but continue deletion
                        }
                    }
                }

                // Cascading deletes are handled by EF Core configuration
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Course deleted successfully with all related files.";
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
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

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
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

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