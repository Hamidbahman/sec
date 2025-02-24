
using Authentication.Domain.Entities;
using Authentication.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data;
public class AutheDbContext : DbContext
{
    public AutheDbContext(DbContextOptions options) : base(options)
    {
    }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<ConfigurationPassword> ConfigurationPasswords { get; set; }
        public DbSet<ConfigurationSession> ConfigurationSessions {get;set;}
        public DbSet<ConfigurationLock> ConfigurationLocks {get;set;}
        public DbSet<Actee> Actees {get;set;}
        public DbSet<Service> Services {get;set;}
        public DbSet<UserProperty> UserProperties {get;set;}
        public DbSet<UserBiometric> UserBiometrics {get;set;}
        public DbSet<BiometricType> BiometricTypes {get;set;}
        public DbSet<ApplicationPackage> ApplicationPackages {get;set;}
        public DbSet<Permission> Permissions {get;set;}
        public DbSet<LoginPolicy> LoginPolicies {get;set;}
        public DbSet<Mask> Masks {get;set;}
        public DbSet<Menu> Menus {get;set;}
        public DbSet<VerificationCode> VerificationCodes {get;set;}
        public DbSet<OauthToken> OauthTokens {get;set;}



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Aplication and Related Entities

            // Aplication sequence and configuration
            modelBuilder.HasSequence<long>("Seq_Aplication", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<Application>()
                .ToTable("tbApplication")
                .HasKey(a => a.Id);

            modelBuilder.Entity<Application>()
                .Property(a => a.Id)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.Seq_Aplication");

            // One-to-many: Aplication -> Roles
            modelBuilder.Entity<Application>()
                .HasMany(a => a.Roles)
                .WithOne(r => r.Application)
                .HasForeignKey(r => r.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Aplication -> ApplicationPackages
            modelBuilder.Entity<Application>()
                .HasMany(a => a.ApplicationPackages)
                .WithOne(p => p.Application)
                .HasForeignKey(p => p.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Application>()
                .HasMany(a => a.ConfigurationLocks) // <-- This must exist in `Application`
                .WithOne(cl => cl.Application)
                .HasForeignKey(cl => cl.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);


            #endregion

            #region ConfigurationPassword

            modelBuilder.HasSequence<long>("Seq_ConfigurationPassword", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<ConfigurationPassword>()
                .ToTable("tbConfigurationPassword")
                .HasKey(cp => cp.Id);

            modelBuilder.Entity<ConfigurationPassword>()
                .Property(cp => cp.Id)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.Seq_ConfigurationPassword");

            modelBuilder.Entity<ConfigurationPassword>()
                .HasOne(cp => cp.Application)
                .WithOne(a => a.ConfigurationPassword)
                .HasForeignKey<ConfigurationPassword>(cp => cp.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Changed cascade delete to Restrict for UserProperties to avoid multiple cascade paths.
            modelBuilder.Entity<ConfigurationPassword>()
                .HasMany(cp => cp.UserProperties)
                .WithOne()
                .OnDelete(DeleteBehavior.Restrict);

            #endregion

            #region  UserProperty
                    modelBuilder.HasSequence<long>("UserPropertySequence")
            .StartsAt(1)
            .IncrementsBy(1);

        // Apply sequence to UserProperty ID
        modelBuilder.Entity<UserProperty>()
            .Property(up => up.UserId)
            .HasDefaultValueSql("NEXT VALUE FOR UserPropertySequence");

        // Configure Foreign Key relationships explicitly
        modelBuilder.Entity<UserProperty>()
            .HasOne(up => up.User)
            .WithOne() // Assuming it's a 1-to-1 relationship
            .HasForeignKey<UserProperty>(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserProperty>()
            .HasOne(up => up.ConfigurationPassword)
            .WithMany(cp => cp.UserProperties)
            .HasForeignKey(up => up.ConfigurationPasswordId)
            .OnDelete(DeleteBehavior.Cascade);

            #endregion
            

            #region ConfigurationSession

            modelBuilder.HasSequence<long>("ConfigurationSessionSequence", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<ConfigurationSession>()
                .ToTable("tbConfigurationSession")
                .HasKey(cs => cs.Id);

            modelBuilder.Entity<ConfigurationSession>()
                .Property(cs => cs.Id)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.ConfigurationSessionSequence");

            modelBuilder.Entity<ConfigurationSession>()
                .HasOne(cs => cs.Application)
                .WithOne(a => a.ConfigurationSession)
                .HasForeignKey<ConfigurationSession>(cs => cs.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            #endregion

            #region ConfigurationLock

            modelBuilder.HasSequence<long>("ConfigurationLockSequence", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<ConfigurationLock>()
                .ToTable("tbConfigurationLock")
                .HasKey(cl => cl.Id);

            modelBuilder.Entity<ConfigurationLock>()
                .Property(cl => cl.Id)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.ConfigurationLockSequence");

            modelBuilder.Entity<ConfigurationLock>()
                .HasOne(cl => cl.Application)
                .WithMany(a => a.ConfigurationLocks)
                .HasForeignKey(cl => cl.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            #endregion

            #region ApplicationPackage

            modelBuilder.HasSequence<long>("ApplicationPackageSeq", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<ApplicationPackage>()
                .ToTable("tbApplicationPackage")
                .HasKey(p => p.Id);

            modelBuilder.Entity<ApplicationPackage>()
                .Property(p => p.Id)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.ApplicationPackageSeq");

            modelBuilder.Entity<ApplicationPackage>()
                .HasOne(p => p.Application)
                .WithMany(a => a.ApplicationPackages)
                .HasForeignKey(p => p.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            #endregion

            #region Role

            modelBuilder.HasSequence<long>("Seq_Role", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<Role>()
                .ToTable("tbRole")
                .HasKey(r => r.Id);

            modelBuilder.Entity<Role>()
                .Property(r => r.Id)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.Seq_Role");

            modelBuilder.Entity<Role>()
                .Property(r => r.Uuid)
                .HasMaxLength(40);

            modelBuilder.Entity<Role>()
                .Property(r => r.Title)
                .HasMaxLength(200);

            modelBuilder.Entity<Role>()
                .Property(r => r.Description)
                .HasMaxLength(2000);

            modelBuilder.Entity<Role>()
                .Property(r => r.IpRange)
                .HasMaxLength(30);

            modelBuilder.Entity<Role>()
                .Property(r => r.Scheduled)
                .HasMaxLength(400);

            modelBuilder.Entity<Role>()
                .HasOne(r => r.Application)
                .WithMany(a => a.Roles)
                .HasForeignKey(r => r.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Role>()
                .HasMany(r => r.UserRoles)
                .WithOne(ur => ur.Role)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Role>()
                .HasMany(r => r.Permissions)
                .WithOne(p => p.Role)
                .HasForeignKey(p => p.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            #endregion

            #region Permission

            modelBuilder.HasSequence<long>("Seq_Permission", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<Permission>()
                .ToTable("tbPermission")
                .HasKey(p => p.Id);

            modelBuilder.Entity<Permission>()
                .Property(p => p.Id)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.Seq_Permission");

            modelBuilder.Entity<Permission>()
                .HasOne(p => p.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(p => p.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Permission>()
                .HasOne(p => p.Actee)
                .WithMany()
                .HasForeignKey(p => p.ActeeId)
                .OnDelete(DeleteBehavior.Restrict);

            #endregion

            #region UserRole

            modelBuilder.HasSequence<long>("seq_UserRole", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<UserRole>()
                .ToTable("tbUserRole")
                .HasKey(ur => ur.Id);

            modelBuilder.Entity<UserRole>()
                .Property(ur => ur.Id)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.seq_UserRole");

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            #endregion

            #region LoginPolicy

            modelBuilder.HasSequence<long>("Seq_LoginPolicy", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<LoginPolicy>()
                .ToTable("tbLoginPolicy")
                .HasKey(lp => lp.Id);

            modelBuilder.Entity<LoginPolicy>()
                .Property(lp => lp.Id)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.Seq_LoginPolicy");

            modelBuilder.Entity<LoginPolicy>()
                .HasOne(lp => lp.User)
                .WithOne(u => u.LoginPolicy)
                .HasForeignKey<LoginPolicy>(lp => lp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LoginPolicy>()
                .Property(lp => lp.LockTypes)
                .HasConversion<int>();

            #endregion

            #region User

            modelBuilder.HasSequence<long>("Seq_User", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<User>()
                .ToTable("tbUser")
                .HasKey(u => u.Id);

            modelBuilder.Entity<User>()
                .Property(u => u.Id)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.Seq_User");

            modelBuilder.Entity<User>()
                .HasOne(u => u.UserProperty)
                .WithOne(up => up.User)
                .HasForeignKey<UserProperty>(up => up.UserId);

            modelBuilder.Entity<User>()
                .HasOne(u => u.LoginPolicy)
                .WithOne(lp => lp.User)
                .HasForeignKey<LoginPolicy>(lp => lp.UserId);

            modelBuilder.Entity<User>()
                .HasOne(u => u.UserBiometric)
                .WithOne(ub => ub.User)
                .HasForeignKey<UserBiometric>(ub => ub.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId);

            #endregion

            #region UserBiometric and BiometricType

            modelBuilder.HasSequence<long>("Seq_UserBiometric", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<UserBiometric>()
                .ToTable("tbUserBiometric")
                .HasKey(ub => ub.Id);

            modelBuilder.Entity<UserBiometric>()
                .Property(ub => ub.Id)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.Seq_UserBiometric");

            modelBuilder.Entity<UserBiometric>()
                .HasOne(ub => ub.User)
                .WithOne(u => u.UserBiometric)
                .HasForeignKey<UserBiometric>(ub => ub.UserId);

            modelBuilder.Entity<UserBiometric>()
                .HasOne(ub => ub.BiometricType)
                .WithMany(bt => bt.UserBiometrics)
                .HasForeignKey(ub => ub.BiometricTitle);

            modelBuilder.Entity<BiometricType>()
                .ToTable("tbBiometricType")
                .HasKey(bt => bt.Title);

            #endregion

            #region VerificationCode

            modelBuilder.HasSequence<long>("Seq_VerificationCode", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<VerificationCode>()
                .ToTable("tbVerificationCode")
                .HasKey(v => v.Id);

            modelBuilder.Entity<VerificationCode>()
                .Property(v => v.Id)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.Seq_VerificationCode");

            #endregion

            #region OauthTokens

            modelBuilder.HasSequence<long>("Seq_OauthTokens", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<OauthToken>()
                .ToTable("tbOauthTokens")
                .HasKey(o => o.Id);

            modelBuilder.Entity<OauthToken>()
                .Property(o => o.Id)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.Seq_OauthTokens");

            #endregion

            #region Menu

            modelBuilder.HasSequence<long>("Seq_Menu", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<Menu>()
                .ToTable("tbMenu")
                .HasKey(m => m.MenuKey);

            modelBuilder.Entity<Menu>()
                .Property(m => m.MenuKey)
                .HasMaxLength(50);

            modelBuilder.Entity<Menu>()
                .Property(m => m.Icon)
                .HasMaxLength(50);

            modelBuilder.Entity<Menu>()
                .Property(m => m.ActeeId)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.Seq_Menu");

            modelBuilder.Entity<Menu>()
                .HasOne(m => m.Actee)
                .WithMany()
                .HasForeignKey(m => m.ActeeId)
                .OnDelete(DeleteBehavior.Cascade);

            #endregion

            #region Service

            modelBuilder.HasSequence<long>("Seq_Service", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<Service>()
                .ToTable("tbService")
                .HasKey(s => s.ServiceKey);

            modelBuilder.Entity<Service>()
                .Property(s => s.ServiceKey)
                .HasMaxLength(200);

            modelBuilder.Entity<Service>()
                .Property(s => s.Rest)
                .HasMaxLength(200);

            modelBuilder.Entity<Service>()
                .Property(s => s.ActeeId)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.Seq_Service");

            modelBuilder.Entity<Service>()
                .HasOne(s => s.Actee)
                .WithMany()
                .HasForeignKey(s => s.ActeeId);

            #endregion

            #region Actee

            modelBuilder.HasSequence<long>("Seq_Actee", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<Actee>()
                .ToTable("tbActee")
                .HasKey(a => a.Id);

            modelBuilder.Entity<Actee>()
                .HasOne(a => a.ApplicationPackage)
                .WithMany()
                .HasForeignKey(a => a.ApplicationPackageId);

            #endregion

            #region Mask

            modelBuilder.HasSequence<long>("Seq_Mask", schema: "dbo")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<Mask>()
                .ToTable("tbMask")
                .HasKey(m => m.MaskId);

            modelBuilder.Entity<Mask>()
                .Property(m => m.MaskId)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.Seq_Mask");

            modelBuilder.Entity<Mask>()
                .HasOne(m => m.Permission)
                .WithMany()
                .HasForeignKey(m => m.PermissionId);

            #endregion


            #region SeedApplication
            modelBuilder.Entity<Application>().HasData(new Application(
                configurationPassword: null, // Adjust as needed
                configurationLocks: new List<ConfigurationLock>(),
                configurationSession: null, // Adjust as needed
                applicationPackages: new List<ApplicationPackage>(),
                roles: new List<Role>(),
                id: 1,
                title: "Sample App",
                clientId: "sample-client-id",
                redirectUrls: "https://example.com/callback",
                clientScope: "read write",
                clientSecret: "secret",
                authenticateGrantType: "password",
                ipRange: "192.168.1.1/24",
                isAutoApprove: true,
                scheduled: "daily",
                status: 1,
                lockEnabled: false,
                description: "This is a sample application"
            ));

            #endregion




            #region SeedUser
            modelBuilder.Entity<User>().HasData(new User(
                userProperty: null, // Adjust as needed
                loginPolicy: null, // Adjust as needed
                userBiometric: null, // Adjust as needed
                userRoles: new List<UserRole>(),
                username: "admin",
                id: 1,
                uuid: "43t8haoghaioergh",
                firstName: "Admin",
                lastName: "User",
                nationalCode: "1234567890",
                email: "admin@example.com",
                phoneNumber: "+1234567890",
                description: "Default admin user",
                privateKey: null,
                ipRange: "0.0.0.0",
                loginAttempt: 0,
                picture: null,
                pictureType: null,
                scheduled: "00:00-23:59",
                twoFactorEnabled: false
            ));
            #endregion


            #region SeedUserProps
            modelBuilder.Entity<UserProperty>().HasData(new UserProperty(
                userId: 1, // Ensure this matches an existing User ID
                password: "123123123", // Replace with a properly hashed password
                configurationPasswordId: 1 // Ensure this matches an existing ConfigurationPassword ID
            ));

            #endregion


            #region SeedConfigurationPassword

            modelBuilder.Entity<ConfigurationPassword>().HasData(new ConfigurationPassword(
                userProperties: new List<UserProperty>(), // Empty list as seeding relationships must be handled separately
                id: 1,
                isComplex: true,
                mustBeChangedInFirstLogin: true,
                mustContainChar: true,
                mustContainUpperCase: true,
                isPolicyNeeded: true,
                minPassLength: 8,
                maxPassLength: 16,
                numericPassNotEqual: 3,
                willPassExpire: true,
                expireDaysAmount: 90,
                redirectToCustomUrlAfterChangePass: false,
                urlAfterChangePass: "",
                applicationId: 1, // Ensure this matches an existing Application ID
                twoFactorEnabled: true
            ));

            #endregion




            #region SeedConfigurationLock

            modelBuilder.Entity<ConfigurationLock>().HasData(new ConfigurationLock(
                id: 1,
                captchaNeeded: true,
                failedLoginAmountBeforeCaptcha: 3,
                lockTimeInterval: 300, // Example: 5 minutes lock time
                lockType: Authentication.Domain.Enums.LockTypes.None, // Ensure this enum exists
                applicationId: 1 // Ensure this ApplicationId exists in the Application table
            ));

            #endregion



            #region SeedLockPolicy
            modelBuilder.Entity<LoginPolicy>().HasData(new LoginPolicy(
                id: 1,
                lockTypes: Authentication.Domain.Enums.LockTypes.None, // Ensure this enum exists and is handled correctly
                userId: 1, // Ensure a User with this ID exists
                lockStartDateTime: new DateTime(2024, 1, 1, 12, 0,0, DateTimeKind.Utc),
                lockEndDateTime: new DateTime(2024, 1, 1, 12, 0,0, DateTimeKind.Utc) // Example: 30-minute lock
            ));


            #endregion


            


            #region SeedApplication
            modelBuilder.Entity<Application>().HasData(new Application(
                configurationPassword: null, // Adjust as needed
                configurationLocks: new List<ConfigurationLock>(),
                configurationSession: null, // Adjust as needed
                applicationPackages: new List<ApplicationPackage>(),
                roles: new List<Role>(),
                id: 2,
                title: "Sample App",
                clientId: "client-id",
                redirectUrls: "https://example.com/callback",
                clientScope: "read write",
                clientSecret: "secret",
                authenticateGrantType: "password",
                ipRange: "192.168.1.1/24",
                isAutoApprove: true,
                scheduled: "daily",
                status: 2,
                lockEnabled: false,
                description: "This is a sample application"
            ));

            #endregion




            #region SeedUser
            modelBuilder.Entity<User>().HasData(new User(
                userProperty: null, // Adjust as needed
                loginPolicy: null, // Adjust as needed
                userBiometric: null, // Adjust as needed
                userRoles: new List<UserRole>(),
                username: "Hamid",
                id: 2,
                uuid: "dfgjoi;sdjgsdopfi",
                firstName: "Hamid",
                lastName: "Ba",
                nationalCode: "1234567890",
                email: "hamid.ba@gmail.com",
                phoneNumber: "+989389074038",
                description: "Default admin user",
                privateKey: null,
                ipRange: "0.0.0.0",
                loginAttempt: 0,
                picture: null,
                pictureType: null,
                scheduled: "00:00-23:59",
                twoFactorEnabled: true
            ));
            #endregion


            #region SeedUserProps
            modelBuilder.Entity<UserProperty>().HasData(new UserProperty(
                userId: 2, // Ensure this matches an existing User ID
                password: "22334455", // Replace with a properly hashed password
                configurationPasswordId: 2 // Ensure this matches an existing ConfigurationPassword ID
            ));

            #endregion


            #region SeedConfigurationPassword

            modelBuilder.Entity<ConfigurationPassword>().HasData(new ConfigurationPassword(
                userProperties: new List<UserProperty>(), // Empty list as seeding relationships must be handled separately
                id: 2,
                isComplex: true,
                mustBeChangedInFirstLogin: true,
                mustContainChar: true,
                mustContainUpperCase: true,
                isPolicyNeeded: true,
                minPassLength: 8,
                maxPassLength: 16,
                numericPassNotEqual: 3,
                willPassExpire: true,
                expireDaysAmount: 90,
                redirectToCustomUrlAfterChangePass: false,
                urlAfterChangePass: "",
                applicationId: 2, // Ensure this matches an existing Application ID
                twoFactorEnabled: true
            ));

            #endregion




            #region SeedConfigurationLock

            modelBuilder.Entity<ConfigurationLock>().HasData(new ConfigurationLock(
                id: 2,
                captchaNeeded: false,
                failedLoginAmountBeforeCaptcha: 3,
                lockTimeInterval: 100, // Example: 5 minutes lock time
                lockType: Authentication.Domain.Enums.LockTypes.None, // Ensure this enum exists
                applicationId: 2 // Ensure this ApplicationId exists in the Application table
            ));

            #endregion



            #region SeedLockPolicy
            modelBuilder.Entity<LoginPolicy>().HasData(new LoginPolicy(
                id: 2,
                lockTypes: LockTypes.None, // Ensure this enum exists and is handled correctly
                userId: 2, // Ensure a User with this ID exists
                lockStartDateTime: new DateTime(2024, 1, 1, 12, 0,0, DateTimeKind.Utc),
                lockEndDateTime: new DateTime(2024, 1, 1, 12, 0,0, DateTimeKind.Utc) // Example: 30-minute lock
            ));


            #endregion



            base.OnModelCreating(modelBuilder);


}
}
