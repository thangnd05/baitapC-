using Microsoft.EntityFrameworkCore;
using Week2.School.Api.Models;

namespace Week2.School.Api.Data;

public sealed class SchoolDbContext(DbContextOptions<SchoolDbContext> options) : DbContext(options)
{
    public DbSet<Programme> Programmes => Set<Programme>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Programme>(entity =>
        {
            entity.ToTable("programme");
            entity.HasKey(x => x.ProgrammeId);
            entity.Property(x => x.ProgrammeId).HasColumnName("programme_id");
            entity.Property(x => x.ProgrammeCode).HasColumnName("programme_code").HasMaxLength(20).IsRequired();
            entity.Property(x => x.ProgrammeName).HasColumnName("programme_name").HasMaxLength(150).IsRequired();
            entity.Property(x => x.DegreeLevel).HasColumnName("degree_level").HasMaxLength(30).IsRequired();
            entity.Property(x => x.DurationYears).HasColumnName("duration_years");
            entity.HasIndex(x => x.ProgrammeCode).IsUnique().HasDatabaseName("ix_programme_code");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("course");
            entity.HasKey(x => x.CourseId);
            entity.Property(x => x.CourseId).HasColumnName("course_id");
            entity.Property(x => x.CourseCode).HasColumnName("course_code").HasMaxLength(20).IsRequired();
            entity.Property(x => x.CourseName).HasColumnName("course_name").HasMaxLength(150).IsRequired();
            entity.Property(x => x.Credits).HasColumnName("credits");
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.HasIndex(x => x.CourseCode).IsUnique().HasDatabaseName("ix_course_code");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("student");
            entity.HasKey(x => x.StudentId);
            entity.Property(x => x.StudentId).HasColumnName("student_id");
            entity.Property(x => x.ProgrammeId).HasColumnName("programme_id");
            entity.Property(x => x.StudentCode).HasColumnName("student_code").HasMaxLength(20).IsRequired();
            entity.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
            entity.Property(x => x.DateOfBirth).HasColumnName("date_of_birth").HasColumnType("date");
            entity.Property(x => x.YearOfEntry).HasColumnName("year_of_entry");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.StudentCode).IsUnique().HasDatabaseName("ix_student_code");
            entity.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ix_student_email");

            entity.HasOne(x => x.Programme).WithMany(x => x.Students)
                .HasForeignKey(x => x.ProgrammeId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
