using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Authentication.Domain.Enums;

namespace Authentication.Domain.Entities;

    [Table("tbUser")]
    public class User : BaseEntity
    {
        [StringLength(30)]
        public string? Username {get;private set;}
        [StringLength(40)]
        public string Uuid { get; private set; }

        [Required, StringLength(50)]
        public string? FirstName { get; private set; }

        [Required, StringLength(50)]
        public string? LastName { get; private set; }

        [Required, StringLength(10)]
        public string NationalCode { get; private set; }

        [Required, EmailAddress(ErrorMessage = "Email should be a proper email address format"), StringLength(50)]
        public string? Email { get; private set; }

        [Required, Phone, StringLength(13)]
        public string? PhoneNumber { get; private set; }

        [StringLength(2000)]
        public string? Description { get; private set; }

        [StringLength(255)]
        public string? PrivateKey { get; private set; }

        [StringLength(50)]
        public string IpRange { get; private set; }

        public int LoginAttempt { get; private set; }

        [StringLength(2000)]
        public string? Picture { get; private set; }

        [StringLength(10)]
        public string? PictureType { get; private set; }

        [StringLength(400)]
        public string Scheduled { get; private set; }

        public bool TwoFactorEnabled { get; private set; } = false;

        public UserProperty UserProperty {get;private set;}
        public LoginPolicy? LoginPolicy {get;private set;}
        public UserBiometric? UserBiometric {get; private set;}

        public ICollection<UserRole> UserRoles {get;private set;} = new List<UserRole>();    
        public User() { }

        public User(
    UserProperty userProperty,
    LoginPolicy loginPolicy,
    UserBiometric userBiometric,
    ICollection<UserRole> userRoles, // ✅ Accept a collection
    string username,
    long id,
    string uuid,
    string firstName,
    string lastName,
    string nationalCode,
    string email,
    string phoneNumber,
    string? description = null,
    string? privateKey = null,
    string ipRange = "0.0.0.0",
    int loginAttempt = 0,
    string? picture = null,
    string? pictureType = null,
    string scheduled = "00:00-23:59",
    bool twoFactorEnabled = false,
    DateTime? createDate = null,
    DateTime? modifyDate = null,
    DateTime? deleteDate = null,
    string? deleteUser = null,
    string? modifyUser = null)
    : base(id, createDate ?? new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 
      modifyDate ?? new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 
      deleteDate, deleteUser, modifyUser)
{
    Username = username;
    Uuid = uuid;
    FirstName = firstName;
    LastName = lastName;
    NationalCode = nationalCode;
    Email = email;
    PhoneNumber = phoneNumber;
    Description = description;
    PrivateKey = privateKey;
    IpRange = ipRange;
    LoginAttempt = loginAttempt;
    Picture = picture;
    PictureType = pictureType;
    Scheduled = scheduled;
    TwoFactorEnabled = twoFactorEnabled;
    UserProperty = userProperty;
    LoginPolicy = loginPolicy;
    UserBiometric = userBiometric;
    UserRoles = userRoles ?? new List<UserRole>(); 
    
}
    public void IncrementLoginAttempt()
    {
        LoginAttempt += 1;
    }
    public void ResetLoginAttempt()
    {
        LoginAttempt = 0;
    }
}

