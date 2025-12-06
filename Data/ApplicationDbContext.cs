using Microsoft.EntityFrameworkCore;
using RaceEvents.Models;

namespace RaceEvents.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Participant> Participants { get; set; }
    public DbSet<Administrator> Administrators { get; set; }
    public DbSet<Car> Cars { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<LapTime> LapTimes { get; set; }
    public DbSet<FinalResult> FinalResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasDiscriminator(u => u.Role)
            .HasValue<Participant>(Models.Enums.Role.PARTICIPANT)
            .HasValue<Administrator>(Models.Enums.Role.ADMINISTRATOR);

        modelBuilder.Entity<Car>()
            .HasOne(c => c.Participant)
            .WithMany(p => p.Cars)
            .HasForeignKey(c => c.ParticipantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Event>()
            .HasOne(e => e.Administrator)
            .WithMany(a => a.Events)
            .HasForeignKey(e => e.AdministratorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.Participant)
            .WithMany(p => p.Applications)
            .HasForeignKey(a => a.ParticipantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.Event)
            .WithMany(e => e.Applications)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.Car)
            .WithMany(c => c.Applications)
            .HasForeignKey(a => a.CarId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LapTime>()
            .HasOne(lt => lt.Application)
            .WithMany(a => a.LapTimes)
            .HasForeignKey(lt => lt.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FinalResult>()
            .HasOne(fr => fr.Application)
            .WithOne(a => a.FinalResult)
            .HasForeignKey<FinalResult>(fr => fr.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Car>()
            .HasIndex(c => c.LicensePlate)
            .IsUnique();
    }
}

