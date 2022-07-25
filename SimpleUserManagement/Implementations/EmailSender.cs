using Azure.Identity;
using Microsoft.Azure.WebJobs;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SimpleUserManagement.Implementations
{
    public class EmailSender
    {
        public async Task SendActivationTokenMailAsync(string email, string activationLink, ExecutionContext context)
        {
            var htmlContent = System.IO.File.ReadAllText(Path.Combine(context.FunctionAppDirectory, "EmailTemplate", "template.html"));
            htmlContent = htmlContent.Replace("ACTIVATIONLINK_PLACEHOLDER", activationLink, StringComparison.OrdinalIgnoreCase);
            await SendMailInternalAsync("myApp: Please confirm email address", email, context, htmlContent);
        }

        public async Task SendPasswordResetMailAsync(string email, string passwordResetToken, ExecutionContext context)
        {
            var htmlContent = System.IO.File.ReadAllText(Path.Combine(context.FunctionAppDirectory, "EmailTemplate", "passwordMail.html"));
            htmlContent = htmlContent.Replace("PASSWORD_LINK_PLACEHOLDER", passwordResetToken, StringComparison.OrdinalIgnoreCase);
            await SendMailInternalAsync("myApp: Reset password", email, context, htmlContent);
        }

        private static async Task SendMailInternalAsync(string title, string email, ExecutionContext context, string htmlContent)
        {
            var scopes = new[] { "https://graph.microsoft.com/.default" };
            var tenantId = Environment.GetEnvironmentVariable("GraphTenantId");
            var clientId = Environment.GetEnvironmentVariable("GraphClientId");
            var clientSecret = Environment.GetEnvironmentVariable("GraphClientSecret");

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            var clientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, options);

            var graphClient = new GraphServiceClient(clientSecretCredential, scopes);
            var message = new Message
            {
                Subject = title,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = htmlContent
                },
                HasAttachments = true,
                Attachments = new MessageAttachmentsCollectionPage()
                {
                    new FileAttachment
                    {
                        Name = "myLogo.png",
                        ContentType = "image/png",
                        ContentBytes = System.IO.File.ReadAllBytes(Path.Combine(context.FunctionAppDirectory, "EmailTemplate", "myLogo.png"))
                    }
                },
                ToRecipients = new List<Recipient>()
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = email
                        }
                    }
                },
                From = new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = "myApp@myCompany.com"
                    }
                }
            };

            await graphClient.Users["myApp@myCompany.com"]
                .SendMail(message, null)
                .Request()
                .PostAsync();
        }
    }
}
