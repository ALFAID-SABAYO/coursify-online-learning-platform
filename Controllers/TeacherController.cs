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
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Use AsNoTracking for read-only operation
            var courses = await _context.Courses
                .AsNoTracking()
                .Where(c => c.TeacherId == userId)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            return View(courses);
        }

        // Edit Course
        public async Task<IActionResult> EditCourse(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Use AsNoTracking for read-only operation to avoid timeout
            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == userId);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, Course course, IFormFile thumbnail, CancellationToken cancellationToken = default)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch existing course with minimal data
            var existingCourse = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == userId, cancellationToken);

            if (existingCourse == null)
            {
                return NotFound();
            }

            // Validate model
            if (!ModelState.IsValid)
            {
                return View(existingCourse);
            }

            // Attach and update - only fetch what we need
            var courseToUpdate = new Course { Id = id, TeacherId = userId.Value };
            _context.Courses.Attach(courseToUpdate);

            // Update only changed properties
            courseToUpdate.Title = course.Title;
            courseToUpdate.Description = course.Description;
            courseToUpdate.Price = course.Price;
            courseToUpdate.Level = course.Level;
            courseToUpdate.Language = course.Language;
            courseToUpdate.PreviewVideoUrl = course.PreviewVideoUrl;
            courseToUpdate.IsPublished = course.IsPublished;

            // Handle thumbnail upload asynchronously with proper cancellation
            if (thumbnail != null && thumbnail.Length > 0)
            {
                try
                {
                    var uploadsPath = Path.Combine("wwwroot", "uploads");
                    Directory.CreateDirectory(uploadsPath);
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(thumbnail.FileName);
                    var filePath = Path.Combine(uploadsPath, fileName);

                    // Use async file operations with cancellation token
                    using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    {
                        await thumbnail.CopyToAsync(stream, cancellationToken);
                    }

                    courseToUpdate.ThumbnailUrl = $"/uploads/{fileName}";
                }
                catch (OperationCanceledException)
                {
                    ModelState.AddModelError("", "Upload was cancelled.");
                    return View(existingCourse);
                }
            }

            try
            {
                // Set command timeout to prevent timeout errors
                _context.Database.SetCommandTimeout(120); // 2 minutes
                await _context.SaveChangesAsync(cancellationToken);
                TempData["Success"] = "Course updated successfully!";
                return RedirectToAction("MyCourses");
            }
            catch (OperationCanceledException)
            {
                ModelState.AddModelError("", "Operation was cancelled due to timeout.");
                return View(existingCourse);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error saving course: {ex.Message}");
                return View(existingCourse);
            }
        }
        public async Task<IActionResult> CourseDetails(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var course = await _context.Courses
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == userId);

            if (course == null)
                return NotFound();

            return View(course);
        }


        // GET: show AddModule form
        public IActionResult AddModule(int courseId)
        {
            ViewBag.CourseId = courseId;
            return View(new Module { CourseId = courseId });
        }

        // POST: save module
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddModule(Module module)
        {
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"{state.Key}: {error.ErrorMessage}");
                    }
                }

                ViewBag.CourseId = module.CourseId;
                return View(module);
            }

            _context.Modules.Add(module);
            await _context.SaveChangesAsync();
            return RedirectToAction("CourseDetails", "Teacher", new { id = module.CourseId });
        }

        // Add Lesson
        public IActionResult AddLesson(int moduleId)
        {
            ViewBag.ModuleId = moduleId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLesson(Lesson lesson, IFormFile videoFile)
        {
            ViewBag.ModuleId = lesson.ModuleId; // Keep module info if validation fails

            // ✅ Ensure either YouTube link or uploaded video is provided
            if (string.IsNullOrEmpty(lesson.VideoUrl) && (videoFile == null || videoFile.Length == 0))
            {
                ModelState.AddModelError("VideoUrl", "Either upload a video or provide a YouTube link.");
            }

            // ✅ Validate model
            if (!ModelState.IsValid)
            {
                return View(lesson);
            }

            // ✅ Handle uploaded video
            if (videoFile != null && videoFile.Length > 0)
            {
                if (videoFile.Length > 500 * 1024 * 1024)
                {
                    ModelState.AddModelError("videoFile", "Video file size must be less than 500MB.");
                    return View(lesson);
                }

                var allowedExtensions = new[] { ".mp4", ".webm", ".ogg", ".mov", ".avi" };
                var ext = Path.GetExtension(videoFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("videoFile", "Invalid video format. Allowed: MP4, WebM, OGG, MOV, AVI.");
                    return View(lesson);
                }

                var videosPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "videos");
                Directory.CreateDirectory(videosPath);

                var fileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(videosPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await videoFile.CopyToAsync(stream);
                }

                lesson.VideoUrl = $"/videos/{fileName}";
            }

            // ✅ Set UTC creation date
            lesson.CreatedDate = DateTime.UtcNow;

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            var module = await _context.Modules.FindAsync(lesson.ModuleId);
            if (module == null)
            {
                ModelState.AddModelError("", "Module not found.");
                return View(lesson);
            }

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