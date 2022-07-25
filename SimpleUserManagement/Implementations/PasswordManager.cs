using Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SimpleUserManagement.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SimpleUserManagement.Implementations
{
    public class PasswordManager
    {
        private readonly ILogger<ActivationTokenManager> logger;
        private readonly TableServiceClientWrapper tableServiceClientWrapper;
        private readonly EmailSender emailSender;

        public PasswordManager(ILogger<ActivationTokenManager> logger,
            TableServiceClientWrapper tableServiceClientWrapper,
            EmailSender emailSender)
        {
            this.logger = logger;
            this.tableServiceClientWrapper = tableServiceClientWrapper;
            this.emailSender = emailSender;
        }

        public async Task<(bool success, string message)> RequestNewPasswordAsync(string mail, ExecutionContext context)
        {
            var tableClient = this.tableServiceClientWrapper.GetTableClient();
            var queryResult = tableClient.Query<UserEntity>(x => x.Email.Equals(mail));
            var existingUserEntity = queryResult.FirstOrDefault();
            if (existingUserEntity == null)
            {
                var message = "Email does not exist. Register user first";
                logger.LogInformation(message);
                return (false, message);
            }
            else
            {
                var passwordResetToken = Guid.NewGuid().ToString();
                existingUserEntity.PasswordResetToken = passwordResetToken;
                existingUserEntity.PasswordAesKey = AesKeyGeneration.Generate(16);
                existingUserEntity.PasswordIvKey = AesKeyGeneration.Generate(16);
                await tableClient.UpdateEntityAsync<UserEntity>(existingUserEntity, existingUserEntity.ETag);
                var link = $"http://localhost:7071/api/LoadResetPasswordViewFunction?token={passwordResetToken}";
                await emailSender.SendPasswordResetMailAsync(mail, link, context);
                return (true, string.Empty);
            }
        }

        public async Task<(bool success, string message)> IsPasswordTokenResetValid(string token)
        {
            var tableClient = this.tableServiceClientWrapper.GetTableClient();
            var queryResult = tableClient.Query<UserEntity>(x => x.PasswordResetToken.Equals(token));
            var existingUserEntity = queryResult.FirstOrDefault();
            if (existingUserEntity == null)
            {
                var message = "Password reset token is invalid";
                logger.LogInformation(message);
                return (false, message);
            }

            return (true, string.Empty);
        }

        public async Task<(bool success, string message)> ExecuteResetPasswordAsync(string token, string password)
        {
            var tableClient = this.tableServiceClientWrapper.GetTableClient();
            var queryResult = tableClient.Query<UserEntity>(x => x.PasswordResetToken.Equals(token));
            var existingUserEntity = queryResult.FirstOrDefault();
            if (existingUserEntity == null)
            {
                var message = "Something went wrong while resetting the password";
                logger.LogInformation(message);
                return (false, message);
            }
            else
            {
                using var sha256Hash = SHA256.Create();
                var hash = UserValidator.GetHash(sha256Hash, $"{password}{existingUserEntity.Salt}");
                existingUserEntity.PasswordResetToken = string.Empty;
                existingUserEntity.PasswordAesKey = string.Empty;
                existingUserEntity.PasswordIvKey = string.Empty;
                existingUserEntity.Password = hash;
                await tableClient.UpdateEntityAsync<UserEntity>(existingUserEntity, existingUserEntity.ETag);
                return (true, string.Empty);
            }
        }

        public async Task<(bool success, string message, string aesKey, string ivKey)> GetDecryptionKeys(string token)
        {
            var tableClient = this.tableServiceClientWrapper.GetTableClient();
            var queryResult = tableClient.Query<UserEntity>(x => x.PasswordResetToken.Equals(token));
            var existingUserEntity = queryResult.FirstOrDefault();
            if (existingUserEntity == null)
            {
                var message = "Token is invalid";
                logger.LogInformation(message);
                return (false, message, string.Empty, string.Empty);
            }
            else
            {
                return (true, string.Empty, existingUserEntity.PasswordAesKey, existingUserEntity.PasswordIvKey);
            }
        }
    }
}
