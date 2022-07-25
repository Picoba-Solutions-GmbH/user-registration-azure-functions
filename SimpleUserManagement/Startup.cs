using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SimpleUserManagement.Implementations;

[assembly: FunctionsStartup(typeof(SimpleUserManagement.Startup))]
namespace SimpleUserManagement
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<ActivationTokenManager>();
            builder.Services.AddSingleton<PasswordManager>();
            builder.Services.AddSingleton<EmailSender>();
            builder.Services.AddSingleton<TableServiceClientWrapper>();
            builder.Services.AddSingleton<UserValidator>();
            builder.Services.AddSingleton<AesDecryption>();
        }
    }
}
