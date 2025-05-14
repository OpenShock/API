using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// This is meant for use in migrations only.
/// </summary>
public sealed class MigrationOpenShockContext : OpenShockContext
{
    private readonly string? _connectionString = null;
    private readonly bool _debug;
    private readonly bool _migrationTool;
    private readonly ILoggerFactory? _loggerFactory = null;
    
    public MigrationOpenShockContext()
    {
        _migrationTool = true;
    }
    
    public MigrationOpenShockContext(string connectionString, bool debug, ILoggerFactory loggerFactory)
    {
        _connectionString = connectionString;
        _debug = debug;
        _loggerFactory = loggerFactory;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_migrationTool)
        {
            ConfigureOptionsBuilder(optionsBuilder, "Host=localhost;Database=openshock;Username=openshock;Password=openshock", true);
            return;
        }
        if(string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("Connection string is not set.");
        ConfigureOptionsBuilder(optionsBuilder, _connectionString, _debug);
        
        if (_loggerFactory != null)
            optionsBuilder.UseLoggerFactory(_loggerFactory);
    }
}

/// <summary>
/// Main OpenShock DB Context
/// </summary>
public class OpenShockContext : DbContext
{
    public OpenShockContext()
    {
    }

    public OpenShockContext(DbContextOptions<OpenShockContext> options)
        : base(options)
    {
    }
    
    public static void ConfigureOptionsBuilder(DbContextOptionsBuilder optionsBuilder, string connectionString,
        bool debug)
    {
        optionsBuilder.UseNpgsql(connectionString, npgsqlBuilder =>
        {
            npgsqlBuilder.MapEnum<RoleType>();
            npgsqlBuilder.MapEnum<ControlType>();
            npgsqlBuilder.MapEnum<PermissionType>();
            npgsqlBuilder.MapEnum<ShockerModelType>();
            npgsqlBuilder.MapEnum<OtaUpdateStatus>();
        });

        if (debug)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }
    }

    public DbSet<ApiToken> ApiTokens { get; set; }

    public DbSet<Device> Devices { get; set; }

    public DbSet<DeviceOtaUpdate> DeviceOtaUpdates { get; set; }

    public DbSet<UserPasswordReset> UserPasswordResets { get; set; }

    public DbSet<UserShareInvite> UserShareInvites { get; set; }

    public DbSet<UserShareInviteShocker> UserShareInviteShockers { get; set; }

    public DbSet<Shocker> Shockers { get; set; }

    public DbSet<ShockerControlLog> ShockerControlLogs { get; set; }

    public DbSet<UserShare> UserShares { get; set; }

    public DbSet<ShockerShareCode> ShockerShareCodes { get; set; }

    public DbSet<PublicShare> PublicShares { get; set; }

    public DbSet<PublicShareShocker> PublicShareShockerMappings { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<UserActivation> UserActivations { get; set; }

    public DbSet<UserEmailChange> UserEmailChanges { get; set; }

    public DbSet<UserNameChange> UserNameChanges { get; set; }

    public DbSet<AdminUsersView> AdminUsersViews { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=openshock;Username=openshock;Password=openshock");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("control_type", ["sound", "vibrate", "shock", "stop"])
            .HasPostgresEnum("ota_update_status", ["started", "running", "finished", "error", "timeout"])
            .HasPostgresEnum("password_encryption_type", ["pbkdf2", "bcrypt_enhanced"])
            .HasPostgresEnum("permission_type",
                ["shockers.use", "shockers.edit", "shockers.pause", "devices.edit", "devices.auth"])
            .HasPostgresEnum("role_type", ["support", "staff", "admin", "system"])
            .HasPostgresEnum("shocker_model_type", ["caiXianlin", "petTrainer", "petrainer998DR"])
            .HasAnnotation("Npgsql:CollationDefinition:public.ndcoll", "und-u-ks-level2,und-u-ks-level2,icu,False");

        modelBuilder.Entity<ApiToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("api_tokens_pkey");

            entity.ToTable("api_tokens");

            // What does this do? please leave comment
            entity.HasIndex(e => e.UserId).HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");
            // What does this do? please leave comment
            entity.HasIndex(e => e.ValidUntil).HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");
            entity.HasIndex(e => e.TokenHash).IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedByIp)
                .HasColumnName("created_by_ip");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.LastUsed)
                .HasDefaultValueSql("'-infinity'::timestamp without time zone")
                .HasColumnName("last_used");
            entity.Property(e => e.Name)
                .HasMaxLength(HardLimits.ApiKeyNameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(HardLimits.Sha256HashHexLength)
                .HasColumnName("token_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ValidUntil).HasColumnName("valid_until");

            entity.Property(e => e.Permissions).HasColumnType("permission_type[]").HasColumnName("permissions");

            entity.HasOne(d => d.User).WithMany(p => p.ApiTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_api_tokens_user_id");
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("devices_pkey");

            entity.ToTable("devices");

            // What does this do? please leave comment
            entity.HasIndex(e => e.OwnerId).HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");
            entity.HasIndex(e => e.Token).IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .VarCharWithLength(HardLimits.HubNameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Token)
                .HasMaxLength(HardLimits.HubTokenMaxLength)
                .HasColumnName("token");

            entity.HasOne(d => d.Owner).WithMany(p => p.Devices)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("fk_devices_owner_id");
        });

        modelBuilder.Entity<DeviceOtaUpdate>(entity =>
        {
            entity.HasKey(e => new { e.DeviceId, e.UpdateId }).HasName("device_ota_updates_pkey");

            entity.ToTable("device_ota_updates");

            entity.HasIndex(e => e.CreatedAt, "device_ota_updates_created_at_idx")
                // What does this do? please leave comment
                .HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.UpdateId).HasColumnName("update_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Message)
                .VarCharWithLength(HardLimits.OtaUpdateMessageMaxLength)
                .HasColumnName("message");
            entity.Property(e => e.Version)
                .VarCharWithLength(HardLimits.SemVerMaxLength)
                .HasColumnName("version");

            entity.Property(e => e.Status).HasColumnType("ota_update_status").HasColumnName("status");


            entity.HasOne(d => d.Device).WithMany(p => p.OtaUpdates)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("fk_device_ota_updates_device_id");
        });

        modelBuilder.Entity<UserPasswordReset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_password_resets_pkey");

            entity.ToTable("user_password_resets");

            // What does this do? please leave comment
            entity.HasIndex(e => e.UserId).HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.SecretHash)
                .VarCharWithLength(HardLimits.PasswordResetSecretMaxLength)
                .HasColumnName("secret");
            entity.Property(e => e.UsedAt)
                .HasColumnName("used_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResets)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_password_resets_user_id");
        });

        modelBuilder.Entity<UserShareInvite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("share_requests_pkey");

            entity.ToTable("share_requests");

            // What does this do? please leave comment
            entity.HasIndex(e => e.OwnerId).HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.RecipientUserId).HasColumnName("user_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.OwnedShockerShareRequests)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("fk_share_requests_owner_id");

            entity.HasOne(d => d.RecipientUser).WithMany(p => p.UserShockerShareRequests)
                .HasForeignKey(d => d.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_share_requests_user_id");
        });

        modelBuilder.Entity<UserShareInviteShocker>(entity =>
        {
            entity.HasKey(e => new { ShareRequestId = e.UserShareInviteId, e.ShockerId }).HasName("share_request_shockers_pkey");

            entity.ToTable("share_request_shockers");

            entity.Property(e => e.UserShareInviteId).HasColumnName("share_request_id");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.AllowShock)
                .HasDefaultValue(true)
                .HasColumnName("allow_shock");
            entity.Property(e => e.AllowVibrate)
                .HasDefaultValue(true)
                .HasColumnName("allow_vibrate");
            entity.Property(e => e.AllowSound)
                .HasDefaultValue(true)
                .HasColumnName("allow_sound");
            entity.Property(e => e.AllowLiveControl)
                .HasDefaultValue(true)
                .HasColumnName("allow_livecontrol");
            entity.Property(e => e.MaxIntensity)
                .HasColumnName("max_intensity");
            entity.Property(e => e.MaxDuration)
                .HasColumnName("max_duration");
            entity.Property(e => e.IsPaused)
                .HasDefaultValue(false)
                .HasColumnName("is_paused");

            entity.HasOne(d => d.UserShareInvite).WithMany(p => p.ShockerMappings)
                .HasForeignKey(d => d.UserShareInviteId)
                .HasConstraintName("fk_share_request_shockers_share_request_id");

            entity.HasOne(d => d.Shocker).WithMany(p => p.UserShareInviteShockers)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("fk_share_request_shockers_shocker_id");
        });

        modelBuilder.Entity<Shocker>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shockers_pkey");

            entity.ToTable("shockers");

            // What does this do? please leave comment
            entity.HasIndex(e => e.DeviceId).HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.Name)
                .HasMaxLength(HardLimits.ShockerNameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.IsPaused)
                .HasDefaultValue(false)
                .HasColumnName("is_paused");
            entity.Property(e => e.Model).HasColumnType("shocker_model_type").HasColumnName("model");
            entity.Property(e => e.RfId).HasColumnName("rf_id");

            entity.HasOne(d => d.Device).WithMany(p => p.Shockers)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("fk_shockers_device_id");
        });

        modelBuilder.Entity<ShockerControlLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shocker_control_logs_pkey");

            entity.ToTable("shocker_control_logs");

            // What does this do? please leave comment
            entity.HasIndex(e => e.ShockerId).HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ControlledByUserId).HasColumnName("controlled_by_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomName)
                .VarCharWithLength(HardLimits.ShockerControlLogCustomNameMaxLength)
                .HasColumnName("custom_name");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.Intensity).HasColumnName("intensity");
            entity.Property(e => e.LiveControl)
                .HasDefaultValue(false)
                .HasColumnName("live_control");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.Type).HasColumnType("control_type").HasColumnName("type");

            entity.HasOne(d => d.ControlledByUser).WithMany(p => p.ShockerControlLogs)
                .HasForeignKey(d => d.ControlledByUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_shocker_control_logs_controlled_by_user_id");

            entity.HasOne(d => d.Shocker).WithMany(p => p.ShockerControlLogs)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("fk_shocker_control_logs_shocker_id");
        });

        modelBuilder.Entity<UserShare>(entity =>
        {
            entity.HasKey(e => new { e.OwnerId, e.SharedWithUserId }).HasName("user_shares_pkey");

            entity.ToTable("user_shares");

            // What does this do? please leave comment
            entity.HasIndex(e => e.SharedWithUserId).HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.SharedWithUserId).HasColumnName("shared_with_user_id");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.AllowShock)
                .HasDefaultValue(true)
                .HasColumnName("allow_shock");
            entity.Property(e => e.AllowVibrate)
                .HasDefaultValue(true)
                .HasColumnName("allow_vibrate");
            entity.Property(e => e.AllowSound)
                .HasDefaultValue(true)
                .HasColumnName("allow_sound");
            entity.Property(e => e.AllowLiveControl)
                .HasDefaultValue(true)
                .HasColumnName("allow_livecontrol");
            entity.Property(e => e.MaxIntensity)
                .HasColumnName("max_intensity");
            entity.Property(e => e.MaxDuration)
                .HasColumnName("max_duration");
            entity.Property(e => e.IsPaused)
                .HasDefaultValue(false)
                .HasColumnName("is_paused");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Owner).WithMany(p => p.OwnedUserShares)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("fk_user_shares_owner_id");

            entity.HasOne(d => d.SharedWithUser).WithMany(p => p.ReceivedUserShares)
                .HasForeignKey(d => d.SharedWithUserId)
                .HasConstraintName("fk_user_shares_shared_with_user_id");

            entity.HasOne(d => d.Shocker).WithMany(p => p.UserShares)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("fk_user_shares_shocker_id");
        });

        modelBuilder.Entity<ShockerShareCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shocker_share_codes_pkey");

            entity.ToTable("shocker_share_codes");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.AllowShock)
                .HasDefaultValue(true)
                .HasColumnName("allow_shock");
            entity.Property(e => e.AllowVibrate)
                .HasDefaultValue(true)
                .HasColumnName("allow_vibrate");
            entity.Property(e => e.AllowSound)
                .HasDefaultValue(true)
                .HasColumnName("allow_sound");
            entity.Property(e => e.AllowLiveControl)
                .HasDefaultValue(true)
                .HasColumnName("allow_livecontrol");
            entity.Property(e => e.MaxIntensity)
                .HasColumnName("max_intensity");
            entity.Property(e => e.MaxDuration)
                .HasColumnName("max_duration");
            entity.Property(e => e.IsPaused)
                .HasDefaultValue(false)
                .HasColumnName("is_paused");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");

            entity.HasOne(d => d.Shocker).WithMany(p => p.ShockerShareCodes)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("fk_shocker_share_codes_shocker_id");
        });

        modelBuilder.Entity<PublicShare>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("public_shares_pkey");

            entity.ToTable("public_shares");

            // What does this do? please leave comment
            entity.HasIndex(e => e.OwnerId).HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Name)
                .VarCharWithLength(HardLimits.PublicShareNameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.OwnedPublicShares)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("fk_public_shares_owner_id");
        });

        modelBuilder.Entity<PublicShareShocker>(entity =>
        {
            entity.HasKey(e => new { e.PublicShareId, e.ShockerId }).HasName("public_share_shockers_pkey");

            entity.ToTable("public_share_shockers");

            entity.Property(e => e.PublicShareId).HasColumnName("public_share_id");
            entity.Property(e => e.ShockerId).HasColumnName("shocker_id");
            entity.Property(e => e.Cooldown).HasColumnName("cooldown");
            entity.Property(e => e.AllowShock)
                .HasDefaultValue(true)
                .HasColumnName("allow_shock");
            entity.Property(e => e.AllowVibrate)
                .HasDefaultValue(true)
                .HasColumnName("allow_vibrate");
            entity.Property(e => e.AllowSound)
                .HasDefaultValue(true)
                .HasColumnName("allow_sound");
            entity.Property(e => e.AllowLiveControl)
                .HasDefaultValue(false)
                .HasColumnName("allow_livecontrol");
            entity.Property(e => e.MaxIntensity)
                .HasColumnName("max_intensity");
            entity.Property(e => e.MaxDuration)
                .HasColumnName("max_duration");
            entity.Property(e => e.IsPaused)
                .HasDefaultValue(false)
                .HasColumnName("is_paused");

            entity.HasOne(d => d.PublicShare).WithMany(p => p.ShockerMappings)
                .HasForeignKey(d => d.PublicShareId)
                .HasConstraintName("fk_public_share_shockers_public_share_id");

            entity.HasOne(d => d.Shocker).WithMany(p => p.PublicShareMappings)
                .HasForeignKey(d => d.ShockerId)
                .HasConstraintName("fk_public_share_shockers_shocker_id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Name).UseCollation("ndcoll").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .VarCharWithLength(HardLimits.EmailAddressMaxLength)
                .HasColumnName("email");
            entity.Property(e => e.EmailActivated)
                .HasDefaultValue(false)
                .HasColumnName("email_activated");
            entity.Property(e => e.Name)
                .UseCollation("ndcoll")
                .VarCharWithLength(HardLimits.UsernameMaxLength)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .VarCharWithLength(HardLimits.PasswordHashMaxLength)
                .HasColumnName("password_hash");
            entity.Property(e => e.Roles)
                .HasColumnType("role_type[]")
                .HasColumnName("roles");
        });

        modelBuilder.Entity<UserActivation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_activations_pkey");

            entity.ToTable("user_activations");

            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.SecretHash)
                .VarCharWithLength(HardLimits.UserActivationSecretMaxLength)
                .HasColumnName("secret");
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserActivations)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_activations_user_id");
        });

        modelBuilder.Entity<UserEmailChange>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_email_changes_pkey");

            entity.ToTable("user_email_changes");

            entity.HasIndex(e => e.UserId);

            // What does this do? please leave comment
            entity.HasIndex(e => e.CreatedAt).HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            // What does this do? please leave comment
            entity.HasIndex(e => e.UsedAt).HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .VarCharWithLength(HardLimits.EmailAddressMaxLength)
                .HasColumnName("email");
            entity.Property(e => e.SecretHash)
                .VarCharWithLength(HardLimits.UserEmailChangeSecretMaxLength)
                .HasColumnName("secret");
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.EmailChanges)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_email_changes_user_id");
        });

        modelBuilder.Entity<UserNameChange>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.UserId }).HasName("user_name_changes_pkey");

            entity.ToTable("user_name_changes");

            // What does this do? please leave comment
            entity.HasIndex(e => e.CreatedAt).HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            // What does this do? please leave comment
            entity.HasIndex(e => e.OldName).HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            // What does this do? please leave comment
            entity.HasIndex(e => e.UserId).HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.OldName)
                .VarCharWithLength(HardLimits.UsernameMaxLength)
                .HasColumnName("old_name");

            entity.HasOne(d => d.User).WithMany(p => p.NameChanges)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_name_changes_user_id");
        });

        modelBuilder.Entity<AdminUsersView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("admin_users_view");

            entity.Property(e => e.Id)
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.Email)
                .HasColumnType("character varying")
                .HasColumnName("email");
            entity.Property(e => e.PasswordHashType)
                .HasColumnType("character varying")
                .HasColumnName("password_hash_type");
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");
            entity.Property(e => e.EmailActivated)
                .HasColumnName("email_activated");
            entity.Property(e => e.Roles)
                .HasColumnType("role_type[]")
                .HasColumnName("roles")
                .HasConversion(x => x.ToArray(), x => x.ToList());
            entity.Property(e => e.ApiTokenCount)
                .HasColumnName("api_token_count");
            entity.Property(e => e.PasswordResetCount)
                .HasColumnName("password_reset_count");
            entity.Property(e => e.ShockerShareCount)
                .HasColumnName("shocker_share_count");
            entity.Property(e => e.ShockerPublicShareCount)
                .HasColumnName("shocker_public_share_count");
            entity.Property(e => e.EmailChangeRequestCount)
                .HasColumnName("email_change_request_count");
            entity.Property(e => e.NameChangeRequestCount)
                .HasColumnName("name_change_request_count");
            entity.Property(e => e.UserActivationCount)
                .HasColumnName("user_activation_count");
            entity.Property(e => e.DeviceCount)
                .HasColumnName("device_count");
            entity.Property(e => e.ShockerCount)
                .HasColumnName("shocker_count");
            entity.Property(e => e.ShockerControlLogCount)
                .HasColumnName("shocker_control_log_count");
        });
    }
}
