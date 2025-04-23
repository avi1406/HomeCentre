using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;

namespace HomeCentre.PIM.BAL.Implementations.B2C.Graph
{
    public class B2CGraphLibrary
    {
        private readonly IConfidentialClientApplication _confidentialClientApplication;

        public B2CGraphLibrary(IConfiguration configuration)
        {
            // Initialize the client credential auth provider
            _confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(configuration["B2C-ClientId"])
                .WithTenantId(configuration["B2C-Tenant-Id"])
                .WithClientSecret(configuration["B2C-Secret"])
                .Build();
            var authProvider = new ClientCredentialProvider(_confidentialClientApplication);

            // Set up the Microsoft Graph service client with client credentials
            GraphClient = new GraphServiceClient(authProvider);
        }

        public GraphServiceClient GraphClient { get; }
    }
}
