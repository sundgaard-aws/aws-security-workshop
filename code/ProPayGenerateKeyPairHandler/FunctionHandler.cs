using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using OM.AWS.Demo.BL;
using OM.AWS.Demo.Config;
using OM.AWS.Demo.SL;

namespace OM.AWS.Demo.API {
    public class FunctionHandler {
        private static ICryptoService cryptoService;

        static FunctionHandler() {
            var serviceProvider=AppConfig.Wireup();
            var secretsService=serviceProvider.GetService<ISecretsService>();
            if(secretsService==null) throw new Exception("Please make sure that ISecretsService is initialized!");
            var cryptoService=serviceProvider.GetService<ICryptoService>();
            if(cryptoService==null) throw new Exception("Please make sure that ICryptoService is initialized!");
            FunctionHandler.cryptoService=cryptoService;
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
        public async Task<Object> Invoke() {
            await cryptoService.GenerateKeyPairAsync();
            return new { Status="Success", Code=200 };
        }   
    }
}