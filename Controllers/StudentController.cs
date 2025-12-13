using Microsoft.AspNetCore.Mvc;
using Coursify.Models;
using Microsoft.EntityFrameworkCore;
using coursify;

namespace Coursify.Controllers
{
    public class StudentController : Controller
    {
        private readonly CoursifyDbContext _context;

        public StudentController(CoursifyDbContext context)
        {
            _context = context;
        }

        // Student Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Teacher)
                .Where(e => e.StudentId == userId)
                .ToListAsync();

            var certificates = await _context.Certificates
                .Include(c => c.Enrollment)
                .ThenInclude(e => e.Course)
                .Where(c => c.Enrollment.StudentId == userId)
                .ToListAsync();

            ViewBag.Enrollments = enrollments;
            ViewBag.Certificates = certificates;

            return View();
        }

        // Browse Courses
        public async Task<IActionResult> BrowseCourses()
        {
            var courses = await _context.Courses
                .Include(c => c.Teacher)
                .Where(c => c.IsPublished)
                .ToListAsync();

            return View(courses);
        }

        // Course Details
        public async Task<IActionResult> CourseDetails(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Teacher)
                .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync(c => c.Id == id);

            var userId = HttpContext.Session.GetInt32("UserId");
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseId == id && e.StudentId == userId);

            ViewBag.IsEnrolled = isEnrolled;
            return View(course);
        }

        // Purchase Course
        public async Task<IActionResult> PurchaseCourse(int courseId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var course = await _context.Courses.FindAsync(courseId);

            // Create enrollment
            var enrollment = new Enrollment
            {
                StudentId = userId.Value,
                CourseId = courseId,
                AmountPaid = course.Price,
                PurchaseDate = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return RedirectToAction("MyCourses");
        }

        // My Courses
        public async Task<IActionResult> MyCourses()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Teacher)
                .Include(e => e.Course.Modules)
                .ThenInclude(m => m.Lessons)
                .Where(e => e.StudentId == userId)
                .ToListAsync();

            return View(enrollments);
        }

        // Watch Lesson
        public async Task<IActionResult> WatchLesson(int enrollmentId, int lessonId)
        {
            var progress = new Progress
            {
                EnrollmentId = enrollmentId,
                LessonId = lessonId,
                IsCompleted = true,
                CompletedDate = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow
            };

            _context.Progresses.Add(progress);
            await _context.SaveChangesAsync();

            // Update enrollment progress
            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Modules)
                .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            var totalLessons = enrollment.Course.Modules
                .SelectMany(m => m.Lessons)
                .Count();

            var completedLessons = await _context.Progresses
                .Where(p => p.EnrollmentId == enrollmentId && p.IsCompleted)
                .CountAsync();

            enrollment.ProgressPercentage = (double)completedLessons / totalLessons * 100;

            // Check if course completed
            if (enrollment.ProgressPercentage >= 100)
            {
                enrollment.CompletedDate = DateTime.UtcNow;
                enrollment.HasCertificate = true;

                // Generate certificate
                var certificate = new Certificate
                {
                    EnrollmentId = enrollmentId,
                    CertificateUrl = $"/certificates/{Guid.NewGuid()}.pdf",
                    IssuedDate = DateTime.UtcNow
                };

                _context.Certificates.Add(certificate);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("MyCourses");
        }

        // View Certificate
        public async Task<IActionResult> ViewCertificate(int enrollmentId)
        {
            var certificate = await _context.Certificates
                .Include(c => c.Enrollment)
                .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(c => c.EnrollmentId == enrollmentId);

            return View(certificate);
        }
    }
}