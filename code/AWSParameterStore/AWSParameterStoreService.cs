using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using OM.AWS.Demo.SL;

namespace OM.AWS.Demo.ParameterStore
{
    public class AWSParameterStoreService : ISettingsService
    {
        private AmazonSimpleSystemsManagementClient client;

        public AWSParameterStoreService() {
            var region=System.Environment.GetEnvironmentVariable("AWS_REGION");
            //if(String.IsNullOrEmpty(region)) throw new Exception("Please specify the AWS_REGION!");
            if(String.IsNullOrEmpty(region)) region=RegionEndpoint.EUNorth1.SystemName;
            Console.WriteLine($"AWSRegion={region}");
            this.client = new AmazonSimpleSystemsManagementClient(RegionEndpoint.GetBySystemName(region));
        }       

        public async Task<string> GetSettingAsync(string settingName)
        {
            var resp=await this.client.GetParameterAsync(new GetParameterRequest{Name=settingName});
            return resp.Parameter.Value;
        }

        public async Task PutSettingAsync<T>(string settingName, string value)
        {
            var resp=await this.client.PutParameterAsync(new PutParameterRequest{Name=settingName});
        }
    }
}