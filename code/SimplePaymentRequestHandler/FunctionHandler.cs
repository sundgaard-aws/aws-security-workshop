using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using OM.AWS.Demo.BL;
using OM.AWS.Demo.Config;
using OM.AWS.Demo.ParameterStore;
using OM.AWS.Demo.SecretsManager;
using OM.AWS.Demo.SL;
using PGPCrypto;

namespace OM.AWS.Demo.API {
    public class FunctionHandler {

        //public static IPaymentService PaymentService { get; }
        public static IObjectStoreService ObjectStoreService { get; }
        public static ISecretsService SecretsService { get; }
        public static ICryptoService CryptoService { get; }
        public static ISettingsService SettingsService { get; }

        static FunctionHandler() {
            var sc=new ServiceCollection();
            //sc.AddSingleton<IPaymentService, ProPayService>();
            //sc.AddSingleton<IObjectStoreService, AmazonS3Service>();
            //sc.AddSingleton<IDatabaseService, AmazonDynamoDBService>();
            //sc.AddSingleton<ISecretsService, AWSSecretsManagerService>();
            sc.AddSingleton<ISettingsService, AWSPublicParameterStoreService>();
            sc.AddSingleton<ISecretsService, AWSPublicSecretsManagerService>();
            sc.AddSingleton<ICryptoService, PGPCryptoService>();
            sc.AddSingleton<IAppContextService, AppConfig>();
            var serviceProvider=sc.BuildServiceProvider();
            //FunctionHandler.PaymentService=serviceProvider.GetService<IPaymentService>();
            //if(FunctionHandler.PaymentService==null) throw new Exception("Please make sure that IPaymentService is initialized!");
            FunctionHandler.ObjectStoreService=serviceProvider.GetService<IObjectStoreService>();
            if(FunctionHandler.ObjectStoreService==null) throw new Exception("Please make sure that IObjectStoreService is initialized!");
            FunctionHandler.SecretsService=serviceProvider.GetService<ISecretsService>();
            if(FunctionHandler.SecretsService==null) throw new Exception("Please make sure that ISecretsService is initialized!");
            FunctionHandler.CryptoService=serviceProvider.GetService<ICryptoService>();
            if(FunctionHandler.SettingsService==null) throw new Exception("Please make sure that ISettingsService is initialized!");
            FunctionHandler.SettingsService=serviceProvider.GetService<ISettingsService>();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
        public async Task<Object> Invoke(object eventData, ILambdaContext context) {
            var paymentDate=new DateTime(2023, 3, 1);
            var paymentsFileName="payments.json";
            Console.WriteLine("ProcessPaymentsAsync started...");
            var paymentInputBucketName=await FunctionHandler.SettingsService.GetSettingAsync("PaymentInputBucketName");
            var paymentsFile=await FunctionHandler.ObjectStoreService.GetObjectAsync(paymentInputBucketName, paymentsFileName);
            var paymentsData=File.ReadAllText(paymentsFile.FullName);
            var encryptedPaymentsData=await FunctionHandler.CryptoService.EncryptAsync(paymentsData);
            var paymentRequestBucketName=await FunctionHandler.SettingsService.GetSettingAsync("PaymentRequestBucketName");
            var tempFileName="/tmp/"+Guid.NewGuid()+".pgp";
            File.WriteAllText(tempFileName, encryptedPaymentsData);
            await FunctionHandler.ObjectStoreService.UploadObjectAsync(paymentRequestBucketName, new FileInfo(tempFileName));
            Console.WriteLine("ProcessPaymentsAsync ended.");
            return new { Status="Success", Code=200 };
        }   
    }
}