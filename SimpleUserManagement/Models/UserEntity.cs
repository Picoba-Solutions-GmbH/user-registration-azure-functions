using Azure;
using Azure.Data.Tables;
using System;

namespace SimpleUserManagement.Models
{
    public class UserEntity : ITableEntity
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Company { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public bool IsActivated { get; set; }
        public string ActivationToken { get; set; }
        public DateTime TokenExpiration { get; set; }
        public string PasswordResetToken { get; set; }
        public string PasswordAesKey { get; set; }
        public string PasswordIvKey { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
