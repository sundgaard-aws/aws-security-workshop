using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using OM.AWS.Demo.DTL;
using OM.AWS.Demo.SL;



namespace OM.AWS.Demo.S3
{
    public class AmazonDynamoDBService : IDatabaseService
    {
        private DynamoDBContext dbContext;

        public AmazonDynamoDBService() {
            var regionName=System.Environment.GetEnvironmentVariable("AWS_REGION");
            if(String.IsNullOrEmpty(regionName)) regionName=RegionEndpoint.EUNorth1.SystemName;
            Console.WriteLine($"DynamoDB is using region {RegionEndpoint.EUNorth1.SystemName}");
            var client = new AmazonDynamoDBClient(RegionEndpoint.EUNorth1);            
            dbContext = new DynamoDBContext(client);
        }

        public async Task SaveAsync<T>(T item)
        {            
            await dbContext.SaveAsync(item);
            
        }
    }
}