using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace OpenShock.Common.OpenShockDb;

public partial class OpenShockContext : DbContext
{
    public OpenShockContext()
    {
    }

    public OpenShockContext(DbContextOptions<OpenShockContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ApiToken> ApiTokens { get; set; }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<DeviceOtaUpdate> DeviceOtaUpdates { get; set; }

    public virtual DbSet<PasswordReset> PasswordResets { get; set; }

    public virtual DbSet<Shocker> Shockers { get; set; }

    public virtual DbSet<ShockerControlLog> ShockerControlLogs { get; set; }

    public virtual DbSet<ShockerShare> ShockerShares { get; set; }

    public virtual DbSet<ShockerShareCode> ShockerShareCodes { get; set; }

    public virtual DbSet<ShockerSharesLink> ShockerSharesLinks { get; set; }

    public virtual DbSet<ShockerSharesLinksShocker> ShockerSharesLinksShockers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //optionsBuilder.UseNpgsql("Host=docker-node;Port=1337;Database=root;Username=root;Password=root;Search Path=openshock-new");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("control_type", new[] { "sound", "vibrate", "shock", "stop" })
            .HasPostgresEnum("ota_update_status", new[] { "requested", "started", "running", "finished", "error", "timeout" })
            .HasPostgresEnum("permission_type", new[] { "shockers.use" })
            .HasPostgresEnum("rank_type", new[] { "user", "support", "staff", "admin", "system" })
            .HasPostgresEnum("shocker_model_type", new[] { "caiXianlin", "petTrainer" });

        modelBuilder.Entity<ApiToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("api_tokens_pkey");

            entity.ToTable("api_tokens");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedByIp)
                .HasColumnType("character varying")
                .HasColumnName("created_by_ip");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");
            entity.Property(e => e.Name)
                .HasMaxLength(64)
                .HasColumnName("name");
            entity.Property(e => e.Token)
                .HasMaxLength(256)
                .HasColumnName("token");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ValidUntil).HasColumnName("valid_until");

            entity.Property(e => e.Permissions).HasColumnType("permission_type[]").HasColumnName("permissions");

            entity.HasOne(d => d.User).WithMany(p => p.ApiTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_id");
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("devices_pkey");

            entity.ToTable("devices");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.Owner).HasColumnName("owner");
            entity.Property(e => e.Token)
                .HasMaxLength(256)
                .HasColumnName("token");

            entity.HasOne(d => d.OwnerNavigation).WithMany(p => p.Devices)
                .HasForeignKey(d => d.Owner)
                .HasConstraintName("owner_user_id");
        });

        modelBuilder.Entity<DeviceOtaUpdate>(entity =>
        {
            entity.HasKey(e => new { e.Device, e.UpdateId }).HasName("device_ota_updates_pkey");

            entity.ToTable("device_ota_updates");

            entity.HasIndex(e => e.CreatedOn, "device_ota_updates_created_on_idx").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.Device).HasColumnName("device");
            entity.Property(e => e.UpdateId).HasColumnName("update_id");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");
            entity.Property(e => e.Version)
                .HasColumnType("character varying")
                .HasColumnName("version");
            
            entity.Property(e => e.Status).HasColumnType("ota_update_status").HasColumnName("status");
            

            entity.HasOne(d => d.DeviceNavigation).WithMany(p => p.DeviceOtaUpdates)
                .HasForeignKey(d => d.Device)
                .HasConstraintName("device_ota_updates_device");
        });

        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("password_resets_pkey");

            entity.ToTable("password_resets");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");
            entity.Property(e => e.Secret)
                .HasColumnType("character varying")
                .HasColumnName("secret");
            entity.Property(e => e.UsedOn)
                .HasColumnType("time with time zone")
                .HasColumnName("used_on");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResets)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_id");
        });

        modelBuilder.Entity<Shocker>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shockers_pkey");

            entity.ToTable("shockers");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");
            entity.Property(e => e.Device).HasColumnName("device");
            entity.Property(e => e.Name)
                .HasMaxLength(64)
                .HasColumnName("name");
            entity.Property(e => e.Paused)
                .HasDefaultValue(false)
                .HasColumnName("paused");
            entity.Property(e => e.Model).HasColumnType("shocker_model_type").HasColumnName("model");
            entity.Property(e => e.RfId).HasColumnName("rf_id");

            entity.HasOne(d => d.DeviceNavigation).WithMany(p => p.Shockers)
                .HasForeignKey(d => d.Device)
                .HasConstraintName("device_id");
        });

        modelBuilder.Entity<ShockerControlLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shocker_control_logs_pkey");

            entity.ToTable("shocker_control_logs");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ControlledBy).HasColumnName("controlled_by");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");
            entity.Property(e => e.CustomName)
                .HasColumnType("character varying")
                .HasColumnName("custom_name");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.Intensity).HasColumnName("intensity");
            entity.Property(e => e.LiveControl)
                .HasDefaultValue(false)
                .HasColumnName("live_control");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.Type).HasColumnType("control_type").HasColumnName("type");

            entity.HasOne(d => d.ControlledByNavigation).WithMany(p => p.ShockerControlLogs)
                .HasForeignKey(d => d.ControlledBy)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_controlled_by");

            entity.HasOne(d => d.Shocker).WithMany(p => p.ShockerControlLogs)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("fk_shocker_id");
        });

        modelBuilder.Entity<ShockerShare>(entity =>
        {
            entity.HasKey(e => new { e.ShockerId, e.SharedWith }).HasName("shocker_shares_pkey");

            entity.ToTable("shocker_shares");

            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.SharedWith).HasColumnName("shared_with");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");
            entity.Property(e => e.LimitDuration).HasColumnName("limit_duration");
            entity.Property(e => e.LimitIntensity).HasColumnName("limit_intensity");
            entity.Property(e => e.Paused)
                .HasDefaultValue(false)
                .HasColumnName("paused");
            entity.Property(e => e.PermLive)
                .HasDefaultValue(true)
                .HasColumnName("perm_live");
            entity.Property(e => e.PermShock)
                .HasDefaultValue(true)
                .HasColumnName("perm_shock");
            entity.Property(e => e.PermSound)
                .HasDefaultValue(true)
                .HasColumnName("perm_sound");
            entity.Property(e => e.PermVibrate)
                .HasDefaultValue(true)
                .HasColumnName("perm_vibrate");

            entity.HasOne(d => d.SharedWithNavigation).WithMany(p => p.ShockerShares)
                .HasForeignKey(d => d.SharedWith)
                .HasConstraintName("shared_with_user_id");

            entity.HasOne(d => d.Shocker).WithMany(p => p.ShockerShares)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("ref_shocker_id");
        });

        modelBuilder.Entity<ShockerShareCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shocker_share_codes_pkey");

            entity.ToTable("shocker_share_codes");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");
            entity.Property(e => e.LimitDuration).HasColumnName("limit_duration");
            entity.Property(e => e.LimitIntensity).HasColumnName("limit_intensity");
            entity.Property(e => e.PermShock)
                .HasDefaultValue(true)
                .HasColumnName("perm_shock");
            entity.Property(e => e.PermSound)
                .HasDefaultValue(true)
                .HasColumnName("perm_sound");
            entity.Property(e => e.PermVibrate)
                .HasDefaultValue(true)
                .HasColumnName("perm_vibrate");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");

            entity.HasOne(d => d.Shocker).WithMany(p => p.ShockerShareCodes)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("fk_shocker_id");
        });

        modelBuilder.Entity<ShockerSharesLink>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shocker_shares_links_pkey");

            entity.ToTable("shocker_shares_links");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");
            entity.Property(e => e.ExpiresOn).HasColumnName("expires_on");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.ShockerSharesLinks)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("owner_id");
        });

        modelBuilder.Entity<ShockerSharesLinksShocker>(entity =>
        {
            entity.HasKey(e => new { e.ShareLinkId, e.ShockerId }).HasName("shocker_shares_links_shockers_pkey");

            entity.ToTable("shocker_shares_links_shockers");

            entity.Property(e => e.ShareLinkId).HasColumnName("share_link_id");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.Cooldown).HasColumnName("cooldown");
            entity.Property(e => e.LimitDuration).HasColumnName("limit_duration");
            entity.Property(e => e.LimitIntensity).HasColumnName("limit_intensity");
            entity.Property(e => e.Paused)
                .HasDefaultValue(false)
                .HasColumnName("paused");
            entity.Property(e => e.PermLive)
                .HasDefaultValue(true)
                .HasColumnName("perm_live");
            entity.Property(e => e.PermShock).HasColumnName("perm_shock");
            entity.Property(e => e.PermSound).HasColumnName("perm_sound");
            entity.Property(e => e.PermVibrate).HasColumnName("perm_vibrate");

            entity.HasOne(d => d.ShareLink).WithMany(p => p.ShockerSharesLinksShockers)
                .HasForeignKey(d => d.ShareLinkId)
                .HasConstraintName("share_link_id");

            entity.HasOne(d => d.Shocker).WithMany(p => p.ShockerSharesLinksShockers)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("shocker_id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.HasIndex(e => e.Email, "idx_email");

            entity.HasIndex(e => e.Name, "idx_name");

            entity.HasIndex(e => e.Name, "username").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasColumnType("character varying")
                .HasColumnName("email");
            entity.Property(e => e.EmailActived)
                .HasDefaultValue(false)
                .HasColumnName("email_actived");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasColumnType("character varying")
                .HasColumnName("password");
            entity.Property(e => e.Rank)
                .HasColumnType("rank_type")
                .HasColumnName("rank");
        });
    }
}
