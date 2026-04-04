using Microsoft.EntityFrameworkCore;
using TaskManager.API.Models;

namespace TaskManager.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<BoardColumn> BoardColumns { get; set; }
        public DbSet<Board> Boards { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserBoard> UserBoards { get; set; }
        
        // 💡 НОВА ТАБЛИЦЯ: Історія дій
        public DbSet<BoardActivity> BoardActivities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserBoard>()
                .HasKey(ub => new { ub.UserId, ub.BoardId }); 

            modelBuilder.Entity<UserBoard>()
                .HasOne(ub => ub.User)
                .WithMany(u => u.SharedBoards)
                .HasForeignKey(ub => ub.UserId);

            modelBuilder.Entity<UserBoard>()
                .HasOne(ub => ub.Board)
                .WithMany(b => b.SharedWithUsers)
                .HasForeignKey(ub => ub.BoardId);

            modelBuilder.Entity<Board>()
                .HasOne(b => b.Owner)
                .WithMany(u => u.OwnedBoards)
                .HasForeignKey(b => b.OwnerId)
                .OnDelete(DeleteBehavior.Restrict); 
        }
    }
}