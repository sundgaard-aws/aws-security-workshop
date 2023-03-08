using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations.APIGateway;
using Microsoft.Extensions.DependencyInjection;
using OM.AWS.Demo.BL;
using OM.AWS.Demo.Config;
using OM.AWS.Demo.SL;

namespace OM.AWS.Demo.API {
    public class FunctionHandler {
        private static PaymentBO paymentBO;

        static FunctionHandler() {
            var serviceProvider=AppConfig.Wireup();
            var paymentService=serviceProvider.GetService<IPaymentService>();
            if(paymentService==null) throw new Exception("Please make sure that IPaymentService is initialized!");
            var objectStore=serviceProvider.GetService<IObjectStoreService>();
            if(objectStore==null) throw new Exception("Please make sure that IObjectStoreService is initialized!");
            var secretsService=serviceProvider.GetService<ISecretsService>();
            if(secretsService==null) throw new Exception("Please make sure that ISecretsService is initialized!");
            var cryptoService=serviceProvider.GetService<ICryptoService>();
            if(cryptoService==null) throw new Exception("Please make sure that ICryptoService is initialized!");
            var paymentBO=serviceProvider.GetService<PaymentBO>();
            if(paymentBO==null) throw new Exception("Please make sure that PaymentBO is initialized!");
            FunctionHandler.paymentBO=paymentBO;
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
        public async Task<Object> Invoke(ILambdaContext lambdaContext) {
            var paymentDate=new DateTime(2023, 3, 1);
            await paymentBO.ProcessPaymentsAsync(new DTL.PaymentRequestDTO{PaymentDate=paymentDate,PaymentsFileGUID=Guid.NewGuid().ToString()});
            var body=new { Status="Success", TransactionID=Guid.NewGuid().ToString() };
            //var httpResponse=HttpResults.Ok(body); //.AddHeader("TransactionID", Guid.NewGuid().ToString());
            //httpResponse.AddHeader("TransactionID", Guid.NewGuid().ToString());
            //return httpResponse;
            return body;
        }   
    }
}