using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime.Endpoints;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using OM.AWS.Demo.SL;

namespace OM.AWS.Demo.ParameterStore
{
    public class AWSParameterStoreService : ISettingsService
    {
        private AmazonSimpleSystemsManagementClient client;

        public AWSParameterStoreService() {
            //var region=System.Environment.GetEnvironmentVariable("AWS_REGION");
            //if(String.IsNullOrEmpty(region)) throw new Exception("Please specify the AWS_REGION!");
            //if(String.IsNullOrEmpty(region)) region=RegionEndpoint.EUNorth1.SystemName;
            var region=RegionEndpoint.EUNorth1;
            Console.WriteLine($"AWSRegion={region.SystemName}");
            //this.client = new AmazonSimpleSystemsManagementClient(RegionEndpoint.GetBySystemName(region));
            var privateEndpointURL="https://ssm.eu-north-1.amazonaws.com";
            //privateEndpointURL="https://vpce-08aa97abcbb850f94-v2rom4xi.ssm.eu-north-1.vpce.amazonaws.com";
            var endpoint=new Endpoint(privateEndpointURL);
            var  ssmConfig = new AmazonSimpleSystemsManagementConfig{ ServiceURL = privateEndpointURL }; //{ ServiceURL = "https://vpce-098lnz0211f9f045g-madxscbm.secretsmanager.eu-west-1.vpce.amazonaws.com" };
            this.client=new AmazonSimpleSystemsManagementClient(ssmConfig);
            //this.client = new AmazonSimpleSystemsManagementClient(region);
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