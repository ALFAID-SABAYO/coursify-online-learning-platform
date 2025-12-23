using Microsoft.AspNetCore.Mvc;
using Coursify.Models;
using Microsoft.EntityFrameworkCore;
using coursify;
using QuestPDF.Fluent;
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

            // Get all published courses for display (exclude banned teachers)
            // Use AsNoTracking for read-only operation
            var allCourses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Teacher)
                .Where(c => c.IsPublished && c.Teacher.IsActive)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            ViewBag.Enrollments = enrollments;
            ViewBag.Certificates = certificates;
            ViewBag.AllCourses = allCourses;

            return View(allCourses);
        }

        // Browse Courses
        public async Task<IActionResult> BrowseCourses()
        {
            // Use AsNoTracking for read-only operation to improve performance
            var courses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Teacher)
                .Where(c => c.IsPublished && c.Teacher.IsActive)
                .OrderByDescending(c => c.CreatedDate)
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

            if (course == null)
            {
                return NotFound();
            }

            // Hide course if teacher is banned or course is not published
            if (!course.IsPublished || !course.Teacher.IsActive)
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            Enrollment enrollment = null;
            if (userId.HasValue)
            {
                enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.CourseId == id && e.StudentId == userId);
            }
            
            var isEnrolled = enrollment != null;
            ViewBag.IsEnrolled = isEnrolled;
            ViewBag.Enrollment = enrollment;
            return View(course);
        }

        // Purchase Course - Redirect to Checkout
        public async Task<IActionResult> PurchaseCourse(int courseId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if already enrolled
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseId == courseId && e.StudentId == userId);
            
            if (isEnrolled)
            {
                return RedirectToAction("MyCourses");
            }

            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
            {
                return NotFound();
            }

            return RedirectToAction("Checkout", new { courseId = courseId });
        }

        // Checkout Page
        [HttpGet]
        public async Task<IActionResult> Checkout(int courseId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var course = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null || !course.IsPublished || !course.Teacher.IsActive)
            {
                return NotFound();
            }

            // Check if already enrolled
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseId == courseId && e.StudentId == userId);
            
            if (isEnrolled)
            {
                return RedirectToAction("MyCourses");
            }

            var model = new CheckoutViewModel
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                Price = course.Price,
                TeacherName = course.Teacher.Name
            };

            ViewBag.Course = course;
            return View(model);
        }

        // Process Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Validate expiry date
            if (model.ExpiryYear < DateTime.Now.Year || 
                (model.ExpiryYear == DateTime.Now.Year && model.ExpiryMonth < DateTime.Now.Month))
            {
                ModelState.AddModelError("ExpiryMonth", "Card has expired");
            }

            if (!ModelState.IsValid)
            {
                var course = await _context.Courses
                    .Include(c => c.Teacher)
                    .FirstOrDefaultAsync(c => c.Id == model.CourseId);
                
                if (course != null)
                {
                    model.CourseTitle = course.Title;
                    model.TeacherName = course.Teacher.Name;
                }
                return View(model);
            }

            // Check if already enrolled
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseId == model.CourseId && e.StudentId == userId);
            
            if (isEnrolled)
            {
                return RedirectToAction("MyCourses");
            }

            var courseToEnroll = await _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == model.CourseId);

            if (courseToEnroll == null || !courseToEnroll.IsPublished || !courseToEnroll.Teacher.IsActive)
            {
                return NotFound();
            }

            // Create enrollment
            var enrollment = new Enrollment
            {
                StudentId = userId.Value,
                CourseId = model.CourseId,
                AmountPaid = model.Price,
                PurchaseDate = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // Create transactions for 5% admin and 95% teacher split
            var adminAmount = model.Price * 0.05m;
            var teacherAmount = model.Price * 0.95m;

            // Get admin user
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            if (admin != null)
            {
                var adminTransaction = new Transaction
                {
                    EnrollmentId = enrollment.Id,
                    RecipientId = admin.Id,
                    RecipientRole = "Admin",
                    Amount = adminAmount,
                    Percentage = 5.0m,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Transactions.Add(adminTransaction);
            }

            // Teacher transaction
            var teacherTransaction = new Transaction
            {
                EnrollmentId = enrollment.Id,
                RecipientId = courseToEnroll.TeacherId,
                RecipientRole = "Teacher",
                Amount = teacherAmount,
                Percentage = 95.0m,
                CreatedDate = DateTime.UtcNow
            };
            _context.Transactions.Add(teacherTransaction);

            await _context.SaveChangesAsync();
            var user = await _context.Users.FindAsync(userId.Value);
            if (user != null)
            {
                var emailService = new EmailService();
                string subject = "Course Purchase Confirmation";
                string body = $@"
              <h2>Hello {user.Name},</h2>
              <p>You have successfully purchased the course: <b>{courseToEnroll.Title}</b>.</p>
              <p>Purchase Date: {DateTime.Now:d}</p>
              <p>Happy Learning!</p>
               ";

                try
                {
                    emailService.SendEmail(user.Email, subject, body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Email failed: " + ex.Message);
                }
            }

            // Redirect to watch course video directly after purchase
            if (!string.IsNullOrEmpty(courseToEnroll.PreviewVideoUrl))
            {
                return RedirectToAction("WatchCourse", new { enrollmentId = enrollment.Id });
            }
            
            return RedirectToAction("MyCourses");
        }

        // My Courses
        //public async Task<IActionResult> MyCourses()
        //{
        //    var userId = HttpContext.Session.GetInt32("UserId");
        //    if (userId == null)
        //    {
        //        return RedirectToAction("Login", "Account");
        //    }

        //    var enrollments = await _context.Enrollments
        //        .Include(e => e.Course)
        //        .ThenInclude(c => c.Teacher)
        //        .Include(e => e.Course.Modules)
        //        .ThenInclude(m => m.Lessons)
        //        .Include(e => e.Progresses)
        //        .Where(e => e.StudentId == userId)
        //        .OrderByDescending(e => e.PurchaseDate)
        //        .ToListAsync();

        //    return View(enrollments);
        //}
        public async Task<IActionResult> MyCourses()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Load enrollments including course, modules, lessons, and progresses
            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                    .ThenInclude(c => c.Teacher)
                .Include(e => e.Course.Modules)
                    .ThenInclude(m => m.Lessons)
                .Include(e => e.Progresses)
                .Where(e => e.StudentId == userId)
                .OrderByDescending(e => e.PurchaseDate)
                .ToListAsync();

            return View(enrollments);
        }

        // ✅ Add GenerateCertificate here alfaid add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateCertificate(int enrollmentId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Certificate)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.StudentId == userId);

            if (enrollment == null)
            {
                return NotFound();
            }

            // Alfaid: generate certificate even if progress is 0
            if (!enrollment.HasCertificate)
            {
                enrollment.CompletedDate = DateTime.UtcNow;
                enrollment.HasCertificate = true;

                if (enrollment.Certificate == null)
                {
                    var certificate = new Certificate
                    {
                        EnrollmentId = enrollmentId,
                        CertificateUrl = $"/certificates/{Guid.NewGuid()}.pdf",
                        IssuedDate = DateTime.UtcNow
                    };
                    _context.Certificates.Add(certificate);
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MyCourses");
        }

        // Watch Course Video (Direct video player for courses without modules)
        public async Task<IActionResult> WatchCourse(int enrollmentId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Teacher)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.StudentId == userId);

            if (enrollment == null)
            {
                TempData["Error"] = "You must enroll in this course to access the video content.";
                return RedirectToAction("BrowseCourses");
            }

            // Verify course is published and teacher is active
            if (!enrollment.Course.IsPublished || !enrollment.Course.Teacher.IsActive)
            {
                TempData["Error"] = "This course is no longer available.";
                return RedirectToAction("MyCourses");
            }

            ViewBag.EnrollmentId = enrollmentId;
            ViewBag.Enrollment = enrollment;

            return View(enrollment.Course);
        }

        // Course Content View (for enrolled students)
        public async Task<IActionResult> CourseContent(int enrollmentId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Teacher)
                .Include(e => e.Course.Modules)
                .ThenInclude(m => m.Lessons)
                .Include(e => e.Progresses)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.StudentId == userId);

            if (enrollment == null)
            {
                return NotFound();
            }

            var completedLessons = enrollment.Progresses
                .Where(p => p.IsCompleted)
                .Select(p => p.LessonId)
                .ToHashSet();

            ViewBag.EnrollmentId = enrollmentId;
            ViewBag.Enrollment = enrollment;
            ViewBag.CompletedLessons = completedLessons;

            return View(enrollment.Course);
        }

        // Watch Lesson
        public async Task<IActionResult> WatchLesson(int enrollmentId, int lessonId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Verify enrollment belongs to user and get full enrollment data
            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Teacher)
                .Include(e => e.Course)
                .ThenInclude(c => c.Modules)
                .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.StudentId == userId);
            
            if (enrollment == null)
            {
                TempData["Error"] = "You must enroll in this course to access the video content.";
                // Get course ID from lesson if enrollment not found
                var lessonEntity = await _context.Lessons
        .Include(l => l.Module)
        .ThenInclude(m => m.Course)
        .FirstOrDefaultAsync(l => l.Id == lessonId);


                if (lessonEntity != null)
                {
                    return RedirectToAction("CourseDetails", new { id = lessonEntity.Module.CourseId });
                }
                return RedirectToAction("BrowseCourses");
            }

            // Verify course is published and teacher is active
            if (!enrollment.Course.IsPublished || !enrollment.Course.Teacher.IsActive)
            {
                TempData["Error"] = "This course is no longer available.";
                return RedirectToAction("MyCourses");
            }

            var lesson = enrollment.Course.Modules
                .SelectMany(m => m.Lessons)
                .FirstOrDefault(l => l.Id == lessonId);

            if (lesson == null)
            {
                return NotFound();
            }

            // Get completion status
            var progress = await _context.Progresses
                .FirstOrDefaultAsync(p => p.EnrollmentId == enrollmentId && p.LessonId == lessonId);
            
            var isCompleted = progress != null && progress.IsCompleted;

            // Get all lessons for navigation
            var allLessons = enrollment.Course.Modules
                .SelectMany(m => m.Lessons)
                .OrderBy(l => l.Module.OrderNo)
                .ThenBy(l => l.OrderNo)
                .ToList();

            var currentIndex = allLessons.FindIndex(l => l.Id == lessonId);
            var nextLesson = currentIndex < allLessons.Count - 1 ? allLessons[currentIndex + 1] : null;
            var prevLesson = currentIndex > 0 ? allLessons[currentIndex - 1] : null;

            // Get completed lesson IDs
            var completedLessons = (await _context.Progresses
     .Where(p => p.EnrollmentId == enrollmentId && p.IsCompleted)
     .Select(p => p.LessonId)
     .ToListAsync()).ToHashSet();

            ViewBag.EnrollmentId = enrollmentId;
            ViewBag.Course = enrollment.Course;
            ViewBag.IsCompleted = isCompleted;
            ViewBag.NextLesson = nextLesson;
            ViewBag.PrevLesson = prevLesson;
            ViewBag.CompletedLessons = completedLessons;

            return View(lesson);
        }

        // Mark Lesson as Completed
        [HttpPost]
        public async Task<IActionResult> MarkCompleted(int enrollmentId, int lessonId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Verify enrollment belongs to user
            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Modules)
                .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.StudentId == userId);
            
            if (enrollment == null)
            {
                return NotFound();
            }

            // Check if progress already exists
            var existingProgress = await _context.Progresses
                .FirstOrDefaultAsync(p => p.EnrollmentId == enrollmentId && p.LessonId == lessonId);

            if (existingProgress == null)
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
            }
            else
            {
                existingProgress.IsCompleted = true;
                existingProgress.CompletedDate = DateTime.UtcNow;
                existingProgress.LastAccessed = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();

            // Update enrollment progress
            var totalLessons = enrollment.Course.Modules
                .SelectMany(m => m.Lessons)
                .Count();

            var completedLessons = await _context.Progresses
                .Where(p => p.EnrollmentId == enrollmentId && p.IsCompleted)
                .CountAsync();

            enrollment.ProgressPercentage = (double)completedLessons / totalLessons * 100;

            // Check if course completed
            //if (enrollment.ProgressPercentage >= 100 && !enrollment.HasCertificate)
            //{
            //    enrollment.CompletedDate = DateTime.UtcNow;
            //    enrollment.HasCertificate = true;

            //    // Generate certificate if it doesn't exist
            //    var existingCertificate = await _context.Certificates
            //        .FirstOrDefaultAsync(c => c.EnrollmentId == enrollmentId);

            //    if (existingCertificate == null)
            //    {
            //        var certificate = new Certificate
            //        {
            //            EnrollmentId = enrollmentId,
            //            CertificateUrl = $"/certificates/{Guid.NewGuid()}.pdf",
            //            IssuedDate = DateTime.UtcNow
            //        };

            //        _context.Certificates.Add(certificate);
            //    }
            //}
            //if (enrollment.ProgressPercentage >= 100 && !enrollment.HasCertificate)//alfaid commented it
            if (!enrollment.HasCertificate)
            {
                enrollment.CompletedDate = DateTime.UtcNow;
                enrollment.HasCertificate = true;

                var existingCertificate = await _context.Certificates
                    .FirstOrDefaultAsync(c => c.EnrollmentId == enrollmentId);

                if (existingCertificate == null)
                {
                    var certificate = new Certificate
                    {
                        EnrollmentId = enrollmentId,
                        CertificateUrl = $"/certificates/{Guid.NewGuid()}.pdf",
                        IssuedDate = DateTime.UtcNow
                    };

                    _context.Certificates.Add(certificate);
                }
            }


            await _context.SaveChangesAsync();

            return RedirectToAction("WatchLesson", new { enrollmentId, lessonId });
        }

        // View Certificate
        public async Task<IActionResult> ViewCertificate(int enrollmentId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var certificate = await _context.Certificates
                .Include(c => c.Enrollment)
                .ThenInclude(e => e.Course)
                .ThenInclude(c => c.Teacher)
                .Include(c => c.Enrollment)
                .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(c => c.EnrollmentId == enrollmentId && c.Enrollment.StudentId == userId);

            if (certificate == null)
            {
                return NotFound();
            }

            return View(certificate);
        }

        // Download Certificate as PDF
        public async Task<IActionResult> DownloadCertificate(int enrollmentId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Teacher)
                .Include(e => e.Student)
                .Include(e => e.Certificate)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.StudentId == userId);

            if (enrollment == null || !enrollment.HasCertificate || enrollment.Certificate == null)
            {
                return NotFound();
            }

            // Generate PDF using QuestPDF
            var pdfBytes = GenerateCertificatePdf(enrollment.Student.Name, enrollment.Course.Title, enrollment.Certificate.IssuedDate, enrollment.Certificate.UniqueCode);

            return File(pdfBytes, "application/pdf", $"Certificate_{enrollment.Course.Title.Replace(" ", "_")}_{enrollment.Certificate.UniqueCode}.pdf");
        }

        private byte[] GenerateCertificatePdf(string studentName, string courseName, DateTime issuedDate, string uniqueCode)
        {
            using var stream = new MemoryStream();
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(50);

                    page.Content()
                        .Column(column =>
                        {
                            column.Spacing(20);

                            // Header
                            column.Item().AlignCenter().Text("Certificate of Completion").FontSize(32).Bold();
                            column.Item().PaddingVertical(10);

                            // Body
                            column.Item().AlignCenter().Text("This is to certify that").FontSize(16);
                            column.Item().PaddingVertical(5);
                            column.Item().AlignCenter().Text(studentName).FontSize(24).Bold();
                            column.Item().PaddingVertical(5);
                            column.Item().AlignCenter().Text("has successfully completed the course").FontSize(16);
                            column.Item().PaddingVertical(5);
                            column.Item().AlignCenter().Text(courseName).FontSize(20).Bold();
                            column.Item().PaddingVertical(10);

                            // Date
                            column.Item().AlignCenter().Text($"Issued on: {issuedDate:MMMM dd, yyyy}").FontSize(14);
                            column.Item().PaddingVertical(10);

                            // Unique Code
                            column.Item().AlignCenter().Text($"Certificate ID: {uniqueCode}").FontSize(12).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                        });
                });
            });

            document.GeneratePdf(stream);
            return stream.ToArray();
        }
    }
}