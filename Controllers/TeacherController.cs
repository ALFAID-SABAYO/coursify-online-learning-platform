using Microsoft.AspNetCore.Mvc;
using Coursify.Models;
using Microsoft.EntityFrameworkCore;
using coursify;
using System.IO;

namespace Coursify.Controllers
{
    public class TeacherController : Controller
    {
        private readonly CoursifyDbContext _context;

        public TeacherController(CoursifyDbContext context)
        {
            _context = context;
        }

        // Teacher Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var courses = await _context.Courses
                .Where(c => c.TeacherId == userId)
                .ToListAsync();

            var totalStudents = await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.Course.TeacherId == userId)
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();

            var totalIncome = await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.Course.TeacherId == userId)
                .SumAsync(e => e.AmountPaid);

            ViewBag.Courses = courses;
            ViewBag.TotalStudents = totalStudents;
            ViewBag.TotalIncome = totalIncome;

            return View();
        }

        // Create Course
        public IActionResult CreateCourse()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse(Course course, IFormFile thumbnail)
        {
            course.TeacherId = HttpContext.Session.GetInt32("UserId").Value;
            course.CreatedDate = DateTime.UtcNow;

            if (thumbnail != null)
            {
                var uploadsPath = Path.Combine("wwwroot", "uploads");
                Directory.CreateDirectory(uploadsPath);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(thumbnail.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await thumbnail.CopyToAsync(stream);
                }

                course.ThumbnailUrl = $"/uploads/{fileName}";
            }

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return RedirectToAction("MyCourses");
        }

        // My Courses (Teacher)
        public async Task<IActionResult> MyCourses()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var courses = await _context.Courses
                .Where(c => c.TeacherId == userId)
                .ToListAsync();

            return View(courses);
        }

        // Edit Course
        public async Task<IActionResult> EditCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            return View(course);
        }

        [HttpPost]
        public async Task<IActionResult> EditCourse(Course course, IFormFile thumbnail)
        {
            if (thumbnail != null)
            {
                var uploadsPath = Path.Combine("wwwroot", "uploads");
                Directory.CreateDirectory(uploadsPath);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(thumbnail.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await thumbnail.CopyToAsync(stream);
                }

                course.ThumbnailUrl = $"/uploads/{fileName}";
            }

            _context.Courses.Update(course);
            await _context.SaveChangesAsync();

            return RedirectToAction("MyCourses");
        }

        // Add Module
        public IActionResult AddModule(int courseId)
        {
            ViewBag.CourseId = courseId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddModule(Module module)
        {
            module.CreatedDate = DateTime.UtcNow;
            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            return RedirectToAction("CourseDetails", new { id = module.CourseId });
        }

        // Add Lesson
        public IActionResult AddLesson(int moduleId)
        {
            ViewBag.ModuleId = moduleId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddLesson(Lesson lesson)
        {
            lesson.CreatedDate = DateTime.UtcNow;
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            var module = await _context.Modules.FindAsync(lesson.ModuleId);
            return RedirectToAction("CourseDetails", new { id = module.CourseId });
        }

        // View Course Students
        public async Task<IActionResult> CourseStudents(int courseId)
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Progresses)
                .Where(e => e.CourseId == courseId)
                .ToListAsync();

            ViewBag.Course = await _context.Courses.FindAsync(courseId);
            return View(enrollments);
        }

        // Course Statistics
        public async Task<IActionResult> CourseStats(int courseId)
        {
            var enrollments = await _context.Enrollments
                .Where(e => e.CourseId == courseId)
                .ToListAsync();

            var totalStudents = enrollments.Count;
            var completedStudents = enrollments.Count(e => e.CompletedDate != null);
            var totalIncome = enrollments.Sum(e => e.AmountPaid);

            ViewBag.Course = await _context.Courses.FindAsync(courseId);
            ViewBag.TotalStudents = totalStudents;
            ViewBag.CompletedStudents = completedStudents;
            ViewBag.TotalIncome = totalIncome;

            return View();
        }
    }
}