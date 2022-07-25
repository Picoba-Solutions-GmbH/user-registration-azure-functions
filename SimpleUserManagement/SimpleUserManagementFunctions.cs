using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SimpleUserManagement.Implementations;
using SimpleUserManagement.Models;

namespace SimpleUserManagement
{
    public class SimpleUserManagementFunctions
    {
        private readonly ActivationTokenManager activationTokenManager;
        private readonly PasswordManager passwordManager;
        private readonly AesDecryption aesDecryption;
        private readonly UserValidator userValidator;

        public SimpleUserManagementFunctions(
            ActivationTokenManager activationTokenManager,
            PasswordManager passwordManager,
            AesDecryption aesDecryption,
            UserValidator userValidator)
        {
            this.activationTokenManager = activationTokenManager;
            this.passwordManager = passwordManager;
            this.aesDecryption = aesDecryption;
            this.userValidator = userValidator;
        }

        [FunctionName("RegisterUserFunction")]
        public async Task<IActionResult> RunRegisterUserFunction(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ExecutionContext context)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // HINT: Enable this if you want to use encrypted data transfert to your azure function.
            //var jsonData = aesDecryption.DecryptCipherText(requestBody);

            var data = JsonConvert.DeserializeObject<UserRegistrationDto>(requestBody);
            var result = await this.activationTokenManager.RegisterUserAsync(data, context);
            if (result.success)
            {
                return new OkObjectResult("An email with an activation link will be sent to you shortly.");
            }

            return new BadRequestObjectResult(result.message);
        }

        [FunctionName("ActivateTokenFunction")]
        public async Task<IActionResult> RunActivateTokenFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ExecutionContext context)
        {
            var token = req.Query["token"];
            var result = await this.activationTokenManager.ActivateUserAsync(token);
            if (result.success)
            {
                var htmlContent = File.ReadAllText(Path.Combine(context.FunctionAppDirectory, "EmailTemplate", "registrationCompleted.html"));
                return new ContentResult { Content = htmlContent, ContentType = "text/html" };
            }

            return new BadRequestObjectResult(result.message);
        }

        [FunctionName("RequestResetPasswordTokenFunction")]
        public async Task<IActionResult> RequestResetPasswordTokenFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ExecutionContext context)
        {
            var email = req.Query["email"];
            var result = await this.passwordManager.RequestNewPasswordAsync(email, context);
            if (result.success)
            {
                return new OkObjectResult("Password reset successfully requested.");
            }

            return new BadRequestObjectResult(result.message);
        }

        [FunctionName("LoadResetPasswordViewFunction")]
        public async Task<IActionResult> LoadResetPasswordViewFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ExecutionContext context)
        {
            var token = req.Query["token"];
            var result = await this.passwordManager.GetDecryptionKeys(token);
            if (!result.success)
            {
                return new BadRequestObjectResult(result.message);
            }

            var htmlContent = System.IO.File.ReadAllText(Path.Combine(context.FunctionAppDirectory, "EmailTemplate", "forgotPassword.html"));
            htmlContent = htmlContent.Replace("TOKEN_PLACEHOLDER", token, StringComparison.OrdinalIgnoreCase);
            htmlContent = htmlContent.Replace("AES_KEY_PLACEHOLDER", result.aesKey, StringComparison.OrdinalIgnoreCase);
            htmlContent = htmlContent.Replace("IV_KEY_PLACEHOLDER", result.ivKey, StringComparison.OrdinalIgnoreCase);
            return new ContentResult { Content = htmlContent, ContentType = "text/html" };
        }

        [FunctionName("ExecuteResetPasswordFunction")]
        public async Task<IActionResult> ExecuteResetPasswordFunction(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            var token = req.Query["token"];
            var decryptionKeysResult = await this.passwordManager.GetDecryptionKeys(token);
            if (!decryptionKeysResult.success)
            {
                return new BadRequestObjectResult(decryptionKeysResult.message);
            }

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var password = aesDecryption.DecryptCipherText(requestBody, decryptionKeysResult.aesKey, decryptionKeysResult.ivKey);
            var result = await this.passwordManager.ExecuteResetPasswordAsync(token, password);
            if (result.success)
            {
                return new OkObjectResult("Password resetted successfully.");
            }

            return new BadRequestObjectResult(result.message);
        }

        [FunctionName("ValidateLoginDataFunction")]
        public async Task<IActionResult> ValidateLoginDataFunction(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // HINT: Enable this if you want to use encrypted data transfer to your azure function.
            //var jsonData = aesDecryption.DecryptCipherText(requestBody);

            var data = JsonConvert.DeserializeObject<LoginDto>(requestBody);
            var result = await this.userValidator.ValidateLoginData(data.Email, data.Password);
            if (result.success)
            {
                return new OkObjectResult(string.Empty);
            }

            return new BadRequestObjectResult(result.message);
        }
    }
}
