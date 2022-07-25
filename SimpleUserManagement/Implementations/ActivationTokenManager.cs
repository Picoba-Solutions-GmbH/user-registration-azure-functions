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
    public class ActivationTokenManager
    {
        private readonly ILogger<ActivationTokenManager> logger;
        private readonly TableServiceClientWrapper tableServiceClientWrapper;
        private readonly EmailSender emailSender;

        public ActivationTokenManager(ILogger<ActivationTokenManager> logger,
            TableServiceClientWrapper tableServiceClientWrapper,
            EmailSender emailSender)
        {
            this.logger = logger;
            this.tableServiceClientWrapper = tableServiceClientWrapper;
            this.emailSender = emailSender;
        }

        public async Task<(bool success, string message)> ActivateUserAsync(string token)
        {
            var tableClient = this.tableServiceClientWrapper.GetTableClient();
            var queryResult = tableClient.Query<UserEntity>(x => x.ActivationToken.Equals(token));
            var existingUserEntity = queryResult.FirstOrDefault();
            if (existingUserEntity == null)
            {
                var message = "Token is invalid";
                logger.LogInformation(message);
                return (false, message);
            }
            else
            {
                if (existingUserEntity.TokenExpiration < DateTime.UtcNow)
                {
                    var message = "Token expired";
                    logger.LogInformation(message);
                    return (false, message);
                }

                existingUserEntity.IsActivated = true;
                await tableClient.UpdateEntityAsync<UserEntity>(existingUserEntity, existingUserEntity.ETag);
                return (true, string.Empty);
            }
        }

        public async Task<(bool success, string message)> RegisterUserAsync(UserRegistrationDto userRegistrationDto, ExecutionContext context)
        {
            var activationToken = Guid.NewGuid().ToString();
            var salt = Guid.NewGuid().ToString();
            using var sha256Hash = SHA256.Create();
            var hash = UserValidator.GetHash(sha256Hash, $"{userRegistrationDto.Password}{salt}");

            var tableClient = this.tableServiceClientWrapper.GetTableClient();
            var user = new UserEntity
            {
                Name = userRegistrationDto.Name,
                Surname = userRegistrationDto.Surname,
                Company = userRegistrationDto.Company,
                Email = userRegistrationDto.Email,
                Password = hash,
                Salt = salt,
                ActivationToken = activationToken,
                TokenExpiration = DateTime.UtcNow.AddDays(1),
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = Guid.NewGuid().ToString(),
                ETag = new ETag(Guid.NewGuid().ToString())
            };

            var queryResult = tableClient.Query<UserEntity>(x => x.Email.Equals(userRegistrationDto.Email));
            var existingUserEntity = queryResult.FirstOrDefault();
            if (existingUserEntity == null)
            {
                try
                {
                    await emailSender.SendActivationTokenMailAsync(userRegistrationDto.Email, $"http://localhost:7071/api/ActivateTokenFunction?token={activationToken}", context);
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex.Message);
                    return (false, ex.Message);
                }

                await tableClient.AddEntityAsync<UserEntity>(user);
                return (true, string.Empty); ;
            }
            else
            {
                var message = "User already exists";
                logger.LogInformation(message);
                return (false, message);
            }
        }
    }
}
