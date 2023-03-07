using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using OM.AWS.Demo.DTL;
using OM.AWS.Demo.SL;



namespace OM.AWS.Demo.S3
{
    public class AmazonDynamoDBService : IDatabaseService
    {        
        public async Task SaveAsync<T>(T item)
        {
            var client = new AmazonDynamoDBClient();
            var context = new DynamoDBContext(client);
            await context.SaveAsync(item);
        }
    }
}