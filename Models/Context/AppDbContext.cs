using Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Rtm.Models.Entities;

namespace Rtm.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Tab> Tabs { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<TaskDependency> TaskDependencies { get; set; }
        public DbSet<HistoryLog> HistoryLogs { get; set; }
       public DbSet<TaskStatistic> TaskStatistics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Унікальний Username
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Конфігурація TaskDependency для уникнення каскадного видалення
            modelBuilder.Entity<TaskDependency>()
                .HasOne(td => td.ParentTask)
                .WithMany(t => t.DependentTasks)
                .HasForeignKey(td => td.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict); // Заборона каскадного видалення

            modelBuilder.Entity<TaskDependency>()
                .HasOne(td => td.ChildTask)
                .WithMany(t => t.PrerequisiteTasks)
                .HasForeignKey(td => td.ChildTaskId)
                .OnDelete(DeleteBehavior.Restrict); // Заборона каскадного видалення

            // (Опціонально) Заборона каскадного видалення задачі, якщо є історія
            modelBuilder.Entity<HistoryLog>()
                .HasOne(hl => hl.Task)
                .WithMany(t => t.HistoryLogs)
                .HasForeignKey(hl => hl.TaskId)
                .OnDelete(DeleteBehavior.SetNull); 
        }
    }
}