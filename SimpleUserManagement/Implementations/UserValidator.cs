using Microsoft.Extensions.Logging;
using SimpleUserManagement.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SimpleUserManagement.Implementations
{
    public class UserValidator
    {
        private readonly ILogger<UserValidator> logger;
        private readonly TableServiceClientWrapper tableServiceClientWrapper;

        public UserValidator(ILogger<UserValidator> logger, TableServiceClientWrapper tableServiceClientWrapper)
        {
            this.logger = logger;
            this.tableServiceClientWrapper = tableServiceClientWrapper;
        }

        public async Task<(bool success, string message)> ValidateLoginData(string mail, string password)
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
                if (!existingUserEntity.IsActivated)
                {
                    return (false, "Activate user first");
                }

                using var sha256Hash = SHA256.Create();
                var isPasswordValid = VerifyHash(sha256Hash, $"{password}{existingUserEntity.Salt}", existingUserEntity.Password);
                return (isPasswordValid, "Wrong password");
            }
        }

        public static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < data.Length; i++)
            {
                stringBuilder.Append(data[i].ToString("x2"));
            }

            return stringBuilder.ToString();
        }

        public static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
        {
            var hashOfInput = GetHash(hashAlgorithm, input);
            var comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(hashOfInput, hash) == 0;
        }
    }
}
