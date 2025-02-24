using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Seed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateSequence(
                name: "ApplicationPackageSeq",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "ConfigurationLockSequence",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "ConfigurationSessionSequence",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "Seq_Actee",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "Seq_Aplication",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "Seq_ConfigurationPassword",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "Seq_LoginPolicy",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "Seq_Mask",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "Seq_Menu",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "Seq_OauthTokens",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "Seq_Permission",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "Seq_Role",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "Seq_Service",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "Seq_User",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "Seq_UserBiometric",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "seq_UserRole",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "Seq_VerificationCode",
                schema: "dbo");

            migrationBuilder.CreateSequence(
                name: "UserPropertySequence");

            migrationBuilder.CreateTable(
                name: "tbApplication",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.Seq_Aplication"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RedirectUrls = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClientScope = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ClientSecret = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AuthenticateGrantType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IpRange = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsAutoApprove = table.Column<bool>(type: "bit", nullable: false),
                    Scheduled = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    LockEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbApplication", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbBiometricType",
                columns: table => new
                {
                    Title = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbBiometricType", x => x.Title);
                });

            migrationBuilder.CreateTable(
                name: "tbOauthTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.Seq_OauthTokens"),
                    ClientId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TokenType = table.Column<short>(type: "smallint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbOauthTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbUser",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.Seq_User"),
                    Username = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Uuid = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NationalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PrivateKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IpRange = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LoginAttempt = table.Column<int>(type: "int", nullable: false),
                    Picture = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PictureType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Scheduled = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbVerificationCode",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.Seq_VerificationCode"),
                    ClientId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    VerifyCode = table.Column<int>(type: "int", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TicketExpireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbVerificationCode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbApplicationPackage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.ApplicationPackageSeq"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbApplicationPackage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbApplicationPackage_tbApplication_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "tbApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbConfigurationLock",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.ConfigurationLockSequence"),
                    CaptchaNeeded = table.Column<bool>(type: "bit", nullable: false),
                    FailedLoginAmountBeforeCaptcha = table.Column<short>(type: "smallint", nullable: false),
                    LockTimeInterval = table.Column<int>(type: "int", nullable: false),
                    LockType = table.Column<short>(type: "smallint", nullable: false),
                    ApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbConfigurationLock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbConfigurationLock_tbApplication_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "tbApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbConfigurationPassword",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.Seq_ConfigurationPassword"),
                    IsComplex = table.Column<bool>(type: "bit", nullable: false),
                    MustBeChangedInFirstLogin = table.Column<bool>(type: "bit", nullable: false),
                    MustContainChar = table.Column<bool>(type: "bit", nullable: false),
                    MustContainUpperCase = table.Column<bool>(type: "bit", nullable: false),
                    IsPolicyNeeded = table.Column<bool>(type: "bit", nullable: false),
                    MinPassLength = table.Column<short>(type: "smallint", nullable: false),
                    MaxPassLength = table.Column<short>(type: "smallint", nullable: false),
                    NumericPassNotEqual = table.Column<short>(type: "smallint", nullable: false),
                    WillPassExpire = table.Column<bool>(type: "bit", nullable: false),
                    ExpireDaysAmount = table.Column<short>(type: "smallint", nullable: false),
                    RedirectToCustomUrlAfterChangePass = table.Column<bool>(type: "bit", nullable: false),
                    UrlAfterChangePass = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbConfigurationPassword", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbConfigurationPassword_tbApplication_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "tbApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbConfigurationSession",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.ConfigurationSessionSequence"),
                    IsConcurrentActive = table.Column<bool>(type: "bit", nullable: false),
                    ConcurrencyCount = table.Column<int>(type: "int", nullable: false),
                    SessionTimeout = table.Column<int>(type: "int", nullable: false),
                    ApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbConfigurationSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbConfigurationSession_tbApplication_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "tbApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbRole",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.Seq_Role"),
                    Uuid = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Authority = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IpRange = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Scheduled = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbRole_tbApplication_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "tbApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbLoginPolicy",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.Seq_LoginPolicy"),
                    LockTypes = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    LockStartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockEndDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbLoginPolicy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbLoginPolicy_tbUser_UserId",
                        column: x => x.UserId,
                        principalTable: "tbUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbUserBiometric",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.Seq_UserBiometric"),
                    BiometricTitle = table.Column<string>(type: "nvarchar(25)", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbUserBiometric", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbUserBiometric_tbBiometricType_BiometricTitle",
                        column: x => x.BiometricTitle,
                        principalTable: "tbBiometricType",
                        principalColumn: "Title",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbUserBiometric_tbUser_UserId",
                        column: x => x.UserId,
                        principalTable: "tbUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbActee",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uuid = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActeeType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApplicationPackageId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbActee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbActee_tbApplicationPackage_ApplicationPackageId",
                        column: x => x.ApplicationPackageId,
                        principalTable: "tbApplicationPackage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbUserProperty",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR UserPropertySequence"),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ConfigurationPasswordId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbUserProperty", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_tbUserProperty_tbConfigurationPassword_ConfigurationPasswordId",
                        column: x => x.ConfigurationPasswordId,
                        principalTable: "tbConfigurationPassword",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbUserProperty_tbUser_UserId",
                        column: x => x.UserId,
                        principalTable: "tbUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbUserRole",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.seq_UserRole"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbUserRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbUserRole_tbRole_RoleId",
                        column: x => x.RoleId,
                        principalTable: "tbRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbUserRole_tbUser_UserId",
                        column: x => x.UserId,
                        principalTable: "tbUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbMenu",
                columns: table => new
                {
                    MenuKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<short>(type: "smallint", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActeeId = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.Seq_Menu")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbMenu", x => x.MenuKey);
                    table.ForeignKey(
                        name: "FK_tbMenu_tbActee_ActeeId",
                        column: x => x.ActeeId,
                        principalTable: "tbActee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbPermission",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.Seq_Permission"),
                    ActeeId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Granting = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbPermission_tbActee_ActeeId",
                        column: x => x.ActeeId,
                        principalTable: "tbActee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbPermission_tbRole_RoleId",
                        column: x => x.RoleId,
                        principalTable: "tbRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbService",
                columns: table => new
                {
                    ServiceKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ServiceType = table.Column<int>(type: "int", nullable: false),
                    Rest = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActeeId = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.Seq_Service")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbService", x => x.ServiceKey);
                    table.ForeignKey(
                        name: "FK_tbService_tbActee_ActeeId",
                        column: x => x.ActeeId,
                        principalTable: "tbActee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbMask",
                columns: table => new
                {
                    MaskId = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR dbo.Seq_Mask"),
                    PermissionId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbMask", x => x.MaskId);
                    table.ForeignKey(
                        name: "FK_tbMask_tbPermission_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "tbPermission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "tbApplication",
                columns: new[] { "Id", "AuthenticateGrantType", "ClientId", "ClientScope", "ClientSecret", "CreateDate", "DeleteDate", "DeleteUser", "Description", "IpRange", "IsAutoApprove", "LockEnabled", "ModifyDate", "ModifyUser", "RedirectUrls", "Scheduled", "Status", "Title" },
                values: new object[,]
                {
                    { 1L, "password", "sample-client-id", "read write", "secret", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "This is a sample application", "192.168.1.1/24", true, false, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "https://example.com/callback", "daily", (short)1, "Sample App" },
                    { 2L, "password", "client-id", "read write", "secret", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "This is a sample application", "192.168.1.1/24", true, false, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "https://example.com/callback", "daily", (short)2, "Sample App" }
                });

            migrationBuilder.InsertData(
                table: "tbUser",
                columns: new[] { "Id", "CreateDate", "DeleteDate", "DeleteUser", "Description", "Email", "FirstName", "IpRange", "LastName", "LoginAttempt", "ModifyDate", "ModifyUser", "NationalCode", "PhoneNumber", "Picture", "PictureType", "PrivateKey", "Scheduled", "TwoFactorEnabled", "Username", "Uuid" },
                values: new object[,]
                {
                    { 1L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Default admin user", "admin@example.com", "Admin", "0.0.0.0", "User", 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "1234567890", "+1234567890", null, null, null, "00:00-23:59", false, "admin", "43t8haoghaioergh" },
                    { 2L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Default admin user", "hamid.ba@gmail.com", "Hamid", "0.0.0.0", "Ba", 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "1234567890", "+989389074038", null, null, null, "00:00-23:59", true, "Hamid", "dfgjoi;sdjgsdopfi" }
                });

            migrationBuilder.InsertData(
                table: "tbConfigurationLock",
                columns: new[] { "Id", "ApplicationId", "CaptchaNeeded", "CreateDate", "DeleteDate", "DeleteUser", "FailedLoginAmountBeforeCaptcha", "LockTimeInterval", "LockType", "ModifyDate", "ModifyUser" },
                values: new object[,]
                {
                    { 1L, 1L, true, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, (short)3, 300, (short)5, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null },
                    { 2L, 2L, false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, (short)3, 100, (short)5, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null }
                });

            migrationBuilder.InsertData(
                table: "tbConfigurationPassword",
                columns: new[] { "Id", "ApplicationId", "CreateDate", "DeleteDate", "DeleteUser", "ExpireDaysAmount", "IsComplex", "IsPolicyNeeded", "MaxPassLength", "MinPassLength", "ModifyDate", "ModifyUser", "MustBeChangedInFirstLogin", "MustContainChar", "MustContainUpperCase", "NumericPassNotEqual", "RedirectToCustomUrlAfterChangePass", "TwoFactorEnabled", "UrlAfterChangePass", "WillPassExpire" },
                values: new object[,]
                {
                    { 1L, 1L, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, (short)90, true, true, (short)16, (short)8, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, true, true, (short)3, false, true, "", true },
                    { 2L, 2L, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, (short)90, true, true, (short)16, (short)8, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, true, true, (short)3, false, true, "", true }
                });

            migrationBuilder.InsertData(
                table: "tbLoginPolicy",
                columns: new[] { "Id", "CreateDate", "DeleteDate", "DeleteUser", "LockEndDateTime", "LockStartDateTime", "LockTypes", "ModifyDate", "ModifyUser", "UserId" },
                values: new object[,]
                {
                    { 1L, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, new DateTime(2024, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), 5, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 1L },
                    { 2L, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, new DateTime(2024, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), 5, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 2L }
                });

            migrationBuilder.InsertData(
                table: "tbUserProperty",
                columns: new[] { "UserId", "ConfigurationPasswordId", "Password" },
                values: new object[,]
                {
                    { 1L, 1L, "123123123" },
                    { 2L, 2L, "22334455" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbActee_ApplicationPackageId",
                table: "tbActee",
                column: "ApplicationPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_tbApplicationPackage_ApplicationId",
                table: "tbApplicationPackage",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbConfigurationLock_ApplicationId",
                table: "tbConfigurationLock",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbConfigurationPassword_ApplicationId",
                table: "tbConfigurationPassword",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbConfigurationSession_ApplicationId",
                table: "tbConfigurationSession",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbLoginPolicy_UserId",
                table: "tbLoginPolicy",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbMask_PermissionId",
                table: "tbMask",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbMenu_ActeeId",
                table: "tbMenu",
                column: "ActeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbPermission_ActeeId",
                table: "tbPermission",
                column: "ActeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbPermission_RoleId",
                table: "tbPermission",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_tbRole_ApplicationId",
                table: "tbRole",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbService_ActeeId",
                table: "tbService",
                column: "ActeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbUserBiometric_BiometricTitle",
                table: "tbUserBiometric",
                column: "BiometricTitle");

            migrationBuilder.CreateIndex(
                name: "IX_tbUserBiometric_UserId",
                table: "tbUserBiometric",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbUserProperty_ConfigurationPasswordId",
                table: "tbUserProperty",
                column: "ConfigurationPasswordId");

            migrationBuilder.CreateIndex(
                name: "IX_tbUserRole_RoleId",
                table: "tbUserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_tbUserRole_UserId",
                table: "tbUserRole",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbConfigurationLock");

            migrationBuilder.DropTable(
                name: "tbConfigurationSession");

            migrationBuilder.DropTable(
                name: "tbLoginPolicy");

            migrationBuilder.DropTable(
                name: "tbMask");

            migrationBuilder.DropTable(
                name: "tbMenu");

            migrationBuilder.DropTable(
                name: "tbOauthTokens");

            migrationBuilder.DropTable(
                name: "tbService");

            migrationBuilder.DropTable(
                name: "tbUserBiometric");

            migrationBuilder.DropTable(
                name: "tbUserProperty");

            migrationBuilder.DropTable(
                name: "tbUserRole");

            migrationBuilder.DropTable(
                name: "tbVerificationCode");

            migrationBuilder.DropTable(
                name: "tbPermission");

            migrationBuilder.DropTable(
                name: "tbBiometricType");

            migrationBuilder.DropTable(
                name: "tbConfigurationPassword");

            migrationBuilder.DropTable(
                name: "tbUser");

            migrationBuilder.DropTable(
                name: "tbActee");

            migrationBuilder.DropTable(
                name: "tbRole");

            migrationBuilder.DropTable(
                name: "tbApplicationPackage");

            migrationBuilder.DropTable(
                name: "tbApplication");

            migrationBuilder.DropSequence(
                name: "ApplicationPackageSeq",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "ConfigurationLockSequence",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "ConfigurationSessionSequence",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "Seq_Actee",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "Seq_Aplication",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "Seq_ConfigurationPassword",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "Seq_LoginPolicy",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "Seq_Mask",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "Seq_Menu",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "Seq_OauthTokens",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "Seq_Permission",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "Seq_Role",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "Seq_Service",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "Seq_User",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "Seq_UserBiometric",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "seq_UserRole",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "Seq_VerificationCode",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "UserPropertySequence");
        }
    }
}
