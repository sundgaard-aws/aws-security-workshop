using Amazon;
using Microsoft.Extensions.DependencyInjection;
using OM.AWS.Demo.BL;
using OM.AWS.Demo.ParameterStore;
using OM.AWS.Demo.ProPay;
using OM.AWS.Demo.S3;
using OM.AWS.Demo.SecretsManager;
using OM.AWS.Demo.SL;
using PGPCrypto;

namespace OM.AWS.Demo.Config
{
    public class AppConfig : IAppContextService
    {
        public static readonly string AppName="aws-sec-";
        public static ServiceProvider Wireup() {
            var sc=new ServiceCollection();
            var awsRegion=RegionEndpoint.EUNorth1;            
            sc.AddSingleton<IObjectStoreService>(new AmazonS3Service(awsRegion));
            sc.AddSingleton<IDatabaseService>(new AmazonDynamoDBService(awsRegion));
            sc.AddSingleton<ISecretsService>(new AWSPublicSecretsManagerService(awsRegion));
            sc.AddSingleton<ISettingsService>(new AWSPublicParameterStoreService(awsRegion));
            sc.AddSingleton<IPaymentService, ProPayService>();
            sc.AddSingleton<ICryptoService, PGPCryptoService>();
            sc.AddSingleton<IAppContextService, AppConfig>();
            sc.AddSingleton<PaymentBO, PaymentBO>();
            return sc.BuildServiceProvider();
        }

        public string GetAppName()
        {
            return "sec-workshop-app";
        }

        public string GetAppPrefix()
        {
            return "sec-ws-";
        }
    }
}