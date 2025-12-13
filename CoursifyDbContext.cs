using Coursify.Models;
using Microsoft.EntityFrameworkCore;

namespace coursify
{
    public class CoursifyDbContext : DbContext
    {
        public CoursifyDbContext(DbContextOptions<CoursifyDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Progress> Progresses { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<Lesson> Lessons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // sab users ki emails alag honi chahye
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // teacher our course relation
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Teacher)
                .WithMany(u => u.Courses)
                .HasForeignKey(c => c.TeacherId);

            // Enrollment relation
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.StudentId);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId);

            // Progress unique constraint
            modelBuilder.Entity<Progress>()
                .HasIndex(p => new { p.EnrollmentId, p.LessonId })
                .IsUnique();

            // One-to-one: Enrollment-Certificate
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Certificate)
                .WithOne(c => c.Enrollment)
                .HasForeignKey<Certificate>(c => c.EnrollmentId);
        }
    }
}