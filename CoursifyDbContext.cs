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
        public DbSet<Transaction> Transactions { get; set; }

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

            // Transaction relations
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Enrollment)
                .WithMany()
                .HasForeignKey(t => t.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Recipient)
                .WithMany()
                .HasForeignKey(t => t.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cascading deletes for Course
            modelBuilder.Entity<Course>()
                .HasMany(c => c.Enrollments)
                .WithOne(e => e.Course)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Course>()
                .HasMany(c => c.Modules)
                .WithOne(m => m.Course)
                .HasForeignKey(m => m.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cascading deletes for Module
            modelBuilder.Entity<Module>()
                .HasMany(m => m.Lessons)
                .WithOne(l => l.Module)
                .HasForeignKey(l => l.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cascading deletes for Enrollment
            modelBuilder.Entity<Enrollment>()
                .HasMany(e => e.Progresses)
                .WithOne(p => p.Enrollment)
                .HasForeignKey(p => p.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}