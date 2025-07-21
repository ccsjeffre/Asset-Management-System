
using Microsoft.EntityFrameworkCore;
using Asset_Management_System.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Borrower> Borrowers { get; set; }
    public DbSet<Hardware> Hardwares { get; set; }
    public DbSet<Inventory> Inventorys { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Deployment> Deployments { get; set; }
    public DbSet<BorrowedHardware> BorrowedHardwares { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hardware>()
            .HasKey(h => h.HardId);

        modelBuilder.Entity<BorrowedHardware>()
        .HasKey(bh => bh.BorrowedHardwareId);

        modelBuilder.Entity<BorrowedHardware>()
       .HasOne(bh => bh.Borrower)
       .WithMany(b => b.BorrowedHardwares)
       .HasForeignKey(bh => bh.BorrowersId)
       .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BorrowedHardware>()
        .HasOne(bh => bh.Hardware)
        .WithMany()
        .HasForeignKey(bh => bh.HardId)
        .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Hardware>()
            .Property(h => h.HardType)
            .IsRequired()
            .HasColumnName("HardType");

        modelBuilder.Entity<Inventory>()
            .HasOne(i => i.Hardware)
            .WithMany()
            .HasForeignKey(i => i.HardId);

        modelBuilder.Entity<User>()
        .HasIndex(u => u.SchoolID)
        .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}