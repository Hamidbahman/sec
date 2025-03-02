using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Authentication.Domain.Entities;

    [Table("tbUserRole")]
    public class UserRole : BaseEntity
    {
        [ForeignKey("User")]
        public long UserId { get; private set; }
        public User? User { get; private set; }

        [ForeignKey("Role")]
        public long RoleId { get; private set; }
        public Role? Role { get; private set; }

        public bool IsDefault { get; private set; } = false;

        public UserRole() {}

        public UserRole(
            long id,
            long userId,
            long roleId,
            bool isDefault
        )
        {
            Id = id;
            UserId = userId;
            RoleId = roleId;
            IsDefault = isDefault;
        }

    public void SetDefaultStatus(bool isDefault)
    {
        IsDefault = isDefault;
    }
    }

