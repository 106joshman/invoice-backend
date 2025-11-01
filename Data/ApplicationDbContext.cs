using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // PREDEFINED DATABASE TABLES STRUCTURE
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<PaymentInfo> PaymentInfo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Additional configuration can be added here if needed
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

                // DEFINE 1-TO-1 RELATIONSHIP WITH PAYMENT INFO
            modelBuilder.Entity<User>()
                .HasOne(u => u.PaymentInfo)
                .WithOne(p => p.User)
                .HasForeignKey<PaymentInfo>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}