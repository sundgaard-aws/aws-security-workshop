using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using OM.AWS.Demo.SL;

namespace OM.AWS.Demo.ParameterStore
{
    public class AWSPublicParameterStoreService : ISettingsService
    {
        private AmazonSimpleSystemsManagementClient client;

        public AWSPublicParameterStoreService(RegionEndpoint region) {
            this.client = new AmazonSimpleSystemsManagementClient(region);
        }       

        public async Task<string> GetSettingAsync(string settingName)
        {
            Console.WriteLine($"Getting parameter {settingName}");
            var resp=await this.client.GetParameterAsync(new GetParameterRequest{Name=settingName});
            return resp.Parameter.Value;
        }

        public async Task PutSettingAsync<T>(string settingName, string value)
        {
            Console.WriteLine($"Putting parameter {settingName}");
            var resp=await this.client.PutParameterAsync(new PutParameterRequest{Name=settingName});
        }
    }
}