using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Web.Models;

namespace OnlineAssessment.Web.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<User> Users { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<AnswerOption> AnswerOptions { get; set; }
        public DbSet<TestCase> TestCases { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<Pricing> Pricings { get; set; }
        public DbSet<OrganizationSubscription> OrganizationSubscriptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ✅ Store UserRole Enum as a string in the database
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            // ✅ Seed pricing plans
            modelBuilder.Entity<Pricing>().HasData(
                new Pricing
                {
                    Id = 1,
                    Name = "Basic MCQ",
                    Price = 12,
                    MaxStudents = 50,
                    IncludesMcq = true,
                    IncludesCoding = false,
                    Description = "Perfect for organizations that need only MCQ tests"
                },
                new Pricing
                {
                    Id = 2,
                    Name = "Basic Coding",
                    Price = 49.99M,
                    MaxStudents = 50,
                    IncludesMcq = false,
                    IncludesCoding = true,
                    Description = "Ideal for organizations focusing on coding assessments"
                },
                new Pricing
                {
                    Id = 3,
                    Name = "Premium",
                    Price = 79.99M,
                    MaxStudents = 100,
                    IncludesMcq = true,
                    IncludesCoding = true,
                    Description = "Complete solution with both MCQ and coding tests"
                }
            );

            base.OnModelCreating(modelBuilder);
        }
    }
}
