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
    public DbSet<Championship> Championships { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .ToTable("Users");

        modelBuilder.Entity<Participant>()
            .ToTable("Participants")
            .HasBaseType<User>();

        modelBuilder.Entity<Administrator>()
            .ToTable("Administrators")
            .HasBaseType<User>();

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

        modelBuilder.Entity<Championship>()
            .HasOne(c => c.Administrator)
            .WithMany(a => a.Championships)
            .HasForeignKey(c => c.AdministratorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Event>()
            .HasOne(e => e.Championship)
            .WithMany(c => c.Events)
            .HasForeignKey(e => e.ChampionshipId)
            .OnDelete(DeleteBehavior.SetNull);

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

        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<Event>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Event>()
            .Property(e => e.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Event>()
            .Property(e => e.CarTypeRequirement)
            .HasConversion<string>();

        modelBuilder.Entity<Application>()
            .Property(a => a.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Application>()
            .Property(a => a.HelmetType)
            .HasConversion<string>();

        modelBuilder.Entity<Application>()
            .Property(a => a.TimerType)
            .HasConversion<string>();

        modelBuilder.Entity<Car>()
            .HasIndex(c => c.LicensePlate)
            .IsUnique();

        modelBuilder.Entity<LapTime>()
            .Property(lt => lt.Time)
            .HasConversion(
                v => v,
                v => v)
            .HasColumnType("time(6)");

        modelBuilder.Entity<FinalResult>()
            .Property(fr => fr.BestLapTime)
            .HasConversion(
                v => v,
                v => v)
            .HasColumnType("time(6)");

        modelBuilder.Entity<FinalResult>()
            .Property(fr => fr.AverageLapTime)
            .HasConversion(
                v => v,
                v => v)
            .HasColumnType("time(6)");

        modelBuilder.Entity<FinalResult>()
            .Property(fr => fr.TotalTime)
            .HasConversion(
                v => v,
                v => v)
            .HasColumnType("time(6)");

        modelBuilder.Entity<Participant>()
            .Property(p => p.BestLapTime)
            .HasConversion(
                v => v.HasValue ? v.Value : (TimeSpan?)null,
                v => v)
            .HasColumnType("time(6)");
    }
}

