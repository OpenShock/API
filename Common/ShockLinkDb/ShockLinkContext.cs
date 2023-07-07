using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ShockLink.Common.ShockLinkDb;

public partial class ShockLinkContext : DbContext
{
    public ShockLinkContext()
    {
    }

    public ShockLinkContext(DbContextOptions<ShockLinkContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ApiToken> ApiTokens { get; set; }

    public virtual DbSet<CfImage> CfImages { get; set; }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<PasswordReset> PasswordResets { get; set; }

    public virtual DbSet<Shocker> Shockers { get; set; }

    public virtual DbSet<ShockerControlLog> ShockerControlLogs { get; set; }

    public virtual DbSet<ShockerShare> ShockerShares { get; set; }

    public virtual DbSet<ShockerShareCode> ShockerShareCodes { get; set; }

    public virtual DbSet<ShockerSharesLink> ShockerSharesLinks { get; set; }

    public virtual DbSet<ShockerSharesLinksShocker> ShockerSharesLinksShockers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("ShockLink", "cf_images_type", new[] { "avatar" })
            .HasPostgresEnum("ShockLink", "control_type", new[] { "sound", "vibrate", "shock", "stop" })
            .HasPostgresEnum("ShockLink", "permission_type", new[] { "shockers.use" })
            .HasPostgresEnum("ShockLink", "shocker_model_type", new[] { "small", "petTrainer" });

        modelBuilder.Entity<ApiToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("api_tokens_pkey");

            entity.ToTable("api_tokens", "ShockLink");

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

        modelBuilder.Entity<CfImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cf_images_pkey");

            entity.ToTable("cf_images", "ShockLink");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");
            
            entity.Property(e => e.Type).HasColumnType("cf_images_type").HasColumnName("type");
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("devices_pkey");

            entity.ToTable("devices", "ShockLink");

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

        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("password_resets_pkey");

            entity.ToTable("password_resets", "ShockLink");

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

            entity.ToTable("shockers", "ShockLink");

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
            entity.Property(e => e.Paused).HasColumnName("paused");
            entity.Property(e => e.Model).HasColumnType("shocker_model_type").HasColumnName("model");
            entity.Property(e => e.RfId).HasColumnName("rf_id");

            entity.HasOne(d => d.DeviceNavigation).WithMany(p => p.Shockers)
                .HasForeignKey(d => d.Device)
                .HasConstraintName("device_id");
        });

        modelBuilder.Entity<ShockerControlLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shocker_control_logs_pkey");

            entity.ToTable("shocker_control_logs", "ShockLink");

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
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.Type).HasColumnType("control_type").HasColumnName("type");

            entity.HasOne(d => d.ControlledByNavigation).WithMany(p => p.ShockerControlLogs)
                .HasForeignKey(d => d.ControlledBy)
                .HasConstraintName("fk_controlled_by");

            entity.HasOne(d => d.Shocker).WithMany(p => p.ShockerControlLogs)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("fk_shocker_id");
        });

        modelBuilder.Entity<ShockerShare>(entity =>
        {
            entity.HasKey(e => new { e.ShockerId, e.SharedWith }).HasName("shocker_shares_pkey");

            entity.ToTable("shocker_shares", "ShockLink");

            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.SharedWith).HasColumnName("shared_with");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");
            entity.Property(e => e.LimitDuration).HasColumnName("limit_duration");
            entity.Property(e => e.LimitIntensity).HasColumnName("limit_intensity");
            entity.Property(e => e.PermShock)
                .IsRequired()
                .HasDefaultValueSql("true")
                .HasColumnName("perm_shock");
            entity.Property(e => e.PermSound)
                .IsRequired()
                .HasDefaultValueSql("true")
                .HasColumnName("perm_sound");
            entity.Property(e => e.PermVibrate)
                .IsRequired()
                .HasDefaultValueSql("true")
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

            entity.ToTable("shocker_share_codes", "ShockLink");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_on");
            entity.Property(e => e.LimitDuration).HasColumnName("limit_duration");
            entity.Property(e => e.LimitIntensity).HasColumnName("limit_intensity");
            entity.Property(e => e.PermShock)
                .IsRequired()
                .HasDefaultValueSql("true")
                .HasColumnName("perm_shock");
            entity.Property(e => e.PermSound)
                .IsRequired()
                .HasDefaultValueSql("true")
                .HasColumnName("perm_sound");
            entity.Property(e => e.PermVibrate)
                .IsRequired()
                .HasDefaultValueSql("true")
                .HasColumnName("perm_vibrate");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");

            entity.HasOne(d => d.Shocker).WithMany(p => p.ShockerShareCodes)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("fk_shocker_id");
        });

        modelBuilder.Entity<ShockerSharesLink>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shocker_shares_links_pkey");

            entity.ToTable("shocker_shares_links", "ShockLink");

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

            entity.ToTable("shocker_shares_links_shockers", "ShockLink");

            entity.Property(e => e.ShareLinkId).HasColumnName("share_link_id");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.Cooldown).HasColumnName("cooldown");
            entity.Property(e => e.LimitDuration).HasColumnName("limit_duration");
            entity.Property(e => e.LimitIntensity).HasColumnName("limit_intensity");
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

            entity.ToTable("users", "ShockLink");

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
            entity.Property(e => e.EmailActived).HasColumnName("email_actived");
            entity.Property(e => e.Image)
                .HasDefaultValueSql("'7d7302ba-be81-47bb-671d-33f9efd20900'::uuid")
                .HasColumnName("image");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasColumnType("character varying")
                .HasColumnName("password");

            entity.HasOne(d => d.ImageNavigation).WithMany(p => p.Users)
                .HasForeignKey(d => d.Image)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_pfp");
        });
    }
}
