using Microsoft.EntityFrameworkCore;

namespace Inquiries.Data;

public class InquiriesDbContext : DbContext
{
    public InquiriesDbContext(DbContextOptions<InquiriesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Inquiry> Inquiries => Set<Inquiry>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<Priority> Priorities => Set<Priority>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Status>(entity =>
        {
            entity.ToTable("Statuses");
            entity.HasKey(status => status.StatusId);
            entity.Property(status => status.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(status => status.Name).IsUnique();
        });

        modelBuilder.Entity<Priority>(entity =>
        {
            entity.ToTable("Priorities");
            entity.HasKey(priority => priority.PriorityId);
            entity.Property(priority => priority.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(priority => priority.Name).IsUnique();
        });

        modelBuilder.Entity<Inquiry>(entity =>
        {
            entity.ToTable("Inquiries");
            entity.HasKey(inquiry => inquiry.InquiryId);
            entity.Property(inquiry => inquiry.Title).HasMaxLength(200).IsRequired();
            entity.Property(inquiry => inquiry.OrganizationName).HasMaxLength(200).IsRequired();
            entity.Property(inquiry => inquiry.CreatedAt).HasColumnType("datetime2");
            entity.Property(inquiry => inquiry.UpdatedAt).HasColumnType("datetime2");

            entity.HasIndex(inquiry => inquiry.StatusId);
            entity.HasIndex(inquiry => inquiry.PriorityId);
            entity.HasIndex(inquiry => inquiry.CreatedAt).IsDescending();
            entity.HasIndex(inquiry => inquiry.OrganizationName);

            entity.HasOne(inquiry => inquiry.Status)
                .WithMany(status => status.Inquiries)
                .HasForeignKey(inquiry => inquiry.StatusId)
                .HasConstraintName("FK_Inquiries_Status");

            entity.HasOne(inquiry => inquiry.Priority)
                .WithMany(priority => priority.Inquiries)
                .HasForeignKey(inquiry => inquiry.PriorityId)
                .HasConstraintName("FK_Inquiries_Priority");
        });
    }
}
