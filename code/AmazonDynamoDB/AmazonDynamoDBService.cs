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

        public AmazonDynamoDBService(RegionEndpoint region) {
            var client = new AmazonDynamoDBClient(region);            
            dbContext = new DynamoDBContext(client);
        }

        public async Task SaveAsync<T>(T item)
        {            
            await dbContext.SaveAsync(item);
            
        }
    }
}