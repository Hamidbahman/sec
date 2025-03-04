using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Authentication.Domain.Entities;

[Table("tbConfigurationSession")]
public class ConfigurationSession : BaseEntity
{
    public bool IsConcurrentActive { get; private set; }
    public int ConcurrencyCount { get; private set; }
    public int SessionTimeout { get; private set; }
    
    [ForeignKey("Application")]
    public long ApplicationId { get; private set; }
    
    public Application Application { get; private set; }

    // Constructor
    public ConfigurationSession(long applicationId, bool isConcurrentActive, int concurrencyCount, int sessionTimeout, long id)
    {
        Id = id;
        ApplicationId = applicationId;
        IsConcurrentActive = isConcurrentActive;
        ConcurrencyCount = concurrencyCount;
        SessionTimeout = sessionTimeout;
    }

    public void UpdateConcurrencyCount(int newConcurrencyCount)
    {
        if (newConcurrencyCount < 0)
        {
            throw new ArgumentException("Concurrency count cannot be negative.", nameof(newConcurrencyCount));
        }

        ConcurrencyCount = newConcurrencyCount;
    }
}