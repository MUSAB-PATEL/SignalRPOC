using SignalR_POC.Utilities;
using Microsoft.AspNetCore.Authentication.Certificate;
using Serilog;
using System.Security.Cryptography.X509Certificates;

namespace SignalR_POC.Extensions
{
    public static class AuthenticationExtension
    {
        public static void ConfigureAuthetication(this IServiceCollection services)
        {
            services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
                .AddCertificate(options =>
                {
                    options.RevocationMode = X509RevocationMode.NoCheck;
                    options.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                    options.AllowedCertificateTypes = CertificateTypes.All;
                    options.Events = new CertificateAuthenticationEvents
                    {
                        OnCertificateValidated = context =>
                        {
                            Log.Information("ConfigureAuthetication:  {IssuerName}", context.ClientCertificate.IssuerName);
                            CertValidation? validationService = context.HttpContext.RequestServices.GetService<CertValidation>();
                            if (validationService != null && validationService.Validate(context.ClientCertificate))
                            {
                                Log.Information("Success");
                                context.Success();
                            }
                            else
                            {
                                Log.Information("invalid cert");
                                context.Fail("invalid cert");
                            }
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = c =>
                        {
                            Log.Information("OnAuthenticationFailed: Invalid certificate");
                            c.Fail("Invalid certificate");
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();
        }
    }
}
