using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PulseHR.Api.Models;

namespace PulseHR.Api.Data;

public partial class PulseHRContext : DbContext
{
    public PulseHRContext(DbContextOptions<PulseHRContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeRole> EmployeeRoles { get; set; }

    public virtual DbSet<EmploymentHistory> EmploymentHistories { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<Holiday> Holidays { get; set; }

    public virtual DbSet<Leave> Leaves { get; set; }

    public virtual DbSet<LeaveBalance> LeaveBalances { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Policy> Policies { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectMember> ProjectMembers { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SalaryHistory> SalaryHistories { get; set; }

    public virtual DbSet<TimesheetEntry> TimesheetEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status).HasDefaultValue("active");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UserType).HasDefaultValue("Employee");
        });

        modelBuilder.Entity<EmployeeRole>(entity =>
        {
            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeRoles).HasConstraintName("FK_EmployeeRoles_Employee");

            entity.HasOne(d => d.Role).WithMany(p => p.EmployeeRoles).HasConstraintName("FK_EmployeeRoles_Role");
        });

        modelBuilder.Entity<EmploymentHistory>(entity =>
        {
            entity.HasOne(d => d.Employee).WithMany(p => p.EmploymentHistories).HasConstraintName("FK_EmploymentHistory_Employee");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.Property(e => e.Type).HasDefaultValue("Event");
        });

        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.Property(e => e.Type).HasDefaultValue("National");
        });

        modelBuilder.Entity<Leave>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status).HasDefaultValue("pending");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Employee).WithMany(p => p.Leaves).HasConstraintName("FK_Leaves_Employee");
        });

        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.Property(e => e.Balance).HasComputedColumnSql("([Total]-[Used])", true);
            entity.Property(e => e.Total).HasDefaultValue(12m);

            entity.HasOne(d => d.Employee).WithMany(p => p.LeaveBalances).HasConstraintName("FK_LeaveBalances_Employee");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Employee).WithMany(p => p.Notifications).HasConstraintName("FK_Notifications_Employee");
        });

        modelBuilder.Entity<Policy>(entity =>
        {
            entity.Property(e => e.UploadedOn).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.UploadedByEmployee).WithMany(p => p.Policies)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Policies_UploadedBy");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status).HasDefaultValue("active");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.ManagerEmployee).WithMany(p => p.Projects)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Projects_Manager");
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.Property(e => e.AssignedOn).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Employee).WithMany(p => p.ProjectMembers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectMembers_Employee");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectMembers).HasConstraintName("FK_ProjectMembers_Project");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Employee).WithMany(p => p.RefreshTokens).HasConstraintName("FK_RefreshTokens_Employee");
        });

        modelBuilder.Entity<SalaryHistory>(entity =>
        {
            entity.HasOne(d => d.Employee).WithMany(p => p.SalaryHistories).HasConstraintName("FK_SalaryHistory_Employee");
        });

        modelBuilder.Entity<TimesheetEntry>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Employee).WithMany(p => p.TimesheetEntries).HasConstraintName("FK_Timesheet_Employee");

            entity.HasOne(d => d.Project).WithMany(p => p.TimesheetEntries)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Timesheet_Project");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
