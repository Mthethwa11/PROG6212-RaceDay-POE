using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RaceDay.Models;

namespace RaceDay.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events => Set<Event>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<RaceRoute> Routes => Set<RaceRoute>();
        public DbSet<Enrolment> Enrolments => Set<Enrolment>();
        public DbSet<Result> Results => Set<Result>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Users -> Events (1..M). Restrict = no cascade, same as the Part 1 SQL script.
            builder.Entity<Event>(e =>
            {
                e.HasOne(x => x.Organiser)
                    .WithMany()
                    .HasForeignKey(x => x.OrganiserId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
                e.ToTable(t => t.HasCheckConstraint("CK_Events_EventType",
                    "[EventType] IN ('Run', 'Walk', 'Cycle')"));
                e.HasIndex(x => x.OrganiserId);
            });

            // Events -> Categories (1..M)
            builder.Entity<Category>(c =>
            {
                c.HasOne(x => x.Event)
                    .WithMany(x => x.Categories)
                    .HasForeignKey(x => x.EventId)
                    .OnDelete(DeleteBehavior.Restrict);
                c.Property(x => x.Price).HasDefaultValue(0m);
                c.HasIndex(x => x.EventId);
            });

            // Categories -> Routes (1..1): the unique index on CategoryId enforces it
            builder.Entity<RaceRoute>(r =>
            {
                r.HasOne(x => x.Category)
                    .WithOne(x => x.Route)
                    .HasForeignKey<RaceRoute>(x => x.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Users(Participant) <-> Categories many-to-many, resolved by Enrolments
            builder.Entity<Enrolment>(en =>
            {
                en.HasOne(x => x.Participant)
                    .WithMany()
                    .HasForeignKey(x => x.ParticipantId)
                    .OnDelete(DeleteBehavior.Restrict);
                en.HasOne(x => x.Category)
                    .WithMany(x => x.Enrolments)
                    .HasForeignKey(x => x.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
                en.Property(x => x.EnrolmentDate).HasDefaultValueSql("GETDATE()");
                en.Property(x => x.Status).HasDefaultValue("Confirmed");
                en.HasIndex(x => new { x.ParticipantId, x.CategoryId }).IsUnique();
                en.ToTable(t => t.HasCheckConstraint("CK_Enrolments_Status",
                    "[Status] IN ('Confirmed', 'Cancelled')"));
            });

            // Enrolments -> Results (1..1)
            builder.Entity<Result>(r =>
            {
                r.HasOne(x => x.Enrolment)
                    .WithOne(x => x.Result)
                    .HasForeignKey<Result>(x => x.EnrolmentId)
                    .OnDelete(DeleteBehavior.Restrict);
                r.Property(x => x.RecordedAt).HasDefaultValueSql("GETDATE()");
            });
        }
    }
}
