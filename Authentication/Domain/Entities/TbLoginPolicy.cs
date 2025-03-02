using System;
using System.ComponentModel.DataAnnotations.Schema;
using Authentication.Domain.Enums;

namespace Authentication.Domain.Entities;

public class LoginPolicy : BaseEntity
{
    public LockTypes LockTypes { get; private set; }

    [ForeignKey("User")]
    public long UserId { get; private set; }
    public User User { get; private set; } // One-to-One Relationship (User must exist)

    public DateTime LockStartDateTime { get; private set; }
    public DateTime LockEndDateTime { get; private set; }

    public LoginPolicy() {}

    public LoginPolicy(
        long id,
        LockTypes lockTypes,
        long userId,
        DateTime lockStartDateTime,
        DateTime lockEndDateTime
    )
    {
        Id = id;
        LockTypes = lockTypes;
        UserId = userId;
        LockStartDateTime = lockStartDateTime;
        LockEndDateTime = lockEndDateTime;
    }

    public void SetLockType(LockTypes lockType)
    {
        LockTypes = lockType;
    }
    public void SetLockStartDateTime(DateTime lockStartDateTime)
    {
        LockStartDateTime = lockStartDateTime;
    }
        public void SetLockEndDateTime(DateTime lockEndDateTime)
    {
        LockEndDateTime = lockEndDateTime;
    }

}
