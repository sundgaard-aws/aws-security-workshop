using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using OM.AWS.Demo.SL;

namespace OM.AWS.Demo.SecretsManager
{
    public class AWSPrivateSecretsManagerService : ISecretsService
    {
        private AmazonSecretsManagerClient client;

        public AWSPrivateSecretsManagerService() {
            var secretsManagerConfig = new AmazonSecretsManagerConfig { ServiceURL = "https://secretsmanager.eu-north-1.amazonaws.com" };
            //AmazonSecretsManagerClient client = new AmazonSecretsManagerClient(accessid, secretkey, config);
            //this.client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
            this.client = new AmazonSecretsManagerClient(secretsManagerConfig);
        }

        public async Task<T> RestoreSecret<T>(string secretName)
        {
            var request = new GetSecretValueRequest
            {
                SecretId = secretName,
                VersionStage = "AWSCURRENT", // VersionStage defaults to AWSCURRENT if unspecified.
            };
            var response = await client.GetSecretValueAsync(request);
            var secretStringJSON=response.SecretString;
            Console.WriteLine($"secretString={secretStringJSON}");
            var secret=JsonSerializer.Deserialize<T>(secretStringJSON);
            return secret;
        }

        public async Task UpdateSecret<T>(string secretName, T secret)
        {
            var memStream=new MemoryStream();
            var request = new PutSecretValueRequest
            {
                SecretId=secretName,
                SecretString=JsonSerializer.Serialize<T>(secret)
            };
            var response = await client.PutSecretValueAsync(request);
        }

        public async Task CreateSecret<T>(string secretName, T secret)
        {
            var request = new CreateSecretRequest
            {
                Name=secretName,
                SecretString=JsonSerializer.Serialize<T>(secret)
            };
            var response = await client.CreateSecretAsync(request);
        }        
    }
}