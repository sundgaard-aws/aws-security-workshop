using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OM.AWS.Demo.DTL;
using OM.AWS.Demo.SL;

namespace OM.AWS.Demo.BL
{
    public class PaymentBO
    {
        private IPaymentService paymentService;
        private IObjectStoreService objectStoreService;
        private IDatabaseService databaseService;
        private ISettingsService settingsService;
        private IAppContextService appContextService;
        private ICryptoService cryptoService;
        private ISecretsService secretsService;

        public PaymentBO(IAppContextService appContextService, IObjectStoreService objectStoreService, ISettingsService settingsService, IDatabaseService databaseService, IPaymentService paymentService, ICryptoService cryptoService, ISecretsService secretsService) {
            this.paymentService=paymentService;
            this.objectStoreService=objectStoreService;
            this.databaseService=databaseService;
            this.settingsService=settingsService;
            this.appContextService=appContextService;
            this.cryptoService=cryptoService;
            this.secretsService=secretsService;
        }

        public async Task CreatePaymentRequestAsync(PaymentRequestDTO paymentRequest)
        {
            Console.WriteLine("Started CreatePaymentRequestAsync...");            
            if(String.IsNullOrEmpty(paymentRequest.PaymentsFileGUID)) throw new Exception("Please ensure that PaymentsFileGUID is valid!");
            if(paymentRequest.PaymentDate==null) throw new Exception("Please ensure that PaymentDate is valid!");
            Console.WriteLine("Step1...");
            var paymentInputBucketName=await settingsService.GetSettingAsync(this.appContextService.GetAppPrefix()+"PaymentInputBucketName");     
            Console.WriteLine("Step2...");
            paymentRequest.Status=PaymentRequestDTO.StatusEnum.CREATED;
            await databaseService.SaveAsync<PaymentRequestDTO>(paymentRequest);
            Console.WriteLine("Step3...");
            var paymentsFile=await objectStoreService.GetObjectAsync(paymentInputBucketName, Path.Join(paymentRequest.PaymentsFileGUID+".json"));
            var cryptoSecretName=await settingsService.GetSettingAsync(this.appContextService.GetAppPrefix()+"CryptoSecretName");     
            var cryptoSecret=await secretsService.RestoreSecret<CryptoSecretDTO>(cryptoSecretName);
            Console.WriteLine($"Found user name {cryptoSecret.UserName}");
            Console.WriteLine($"Found pass phrase {cryptoSecret.PassPhrase}");
            cryptoService.LazyInit(cryptoSecret.UserName, cryptoSecret.PassPhrase);
            await paymentService.SendToPaymentProviderAsync(paymentsFile);           
            Console.WriteLine("Step4...");
            paymentRequest.Status=PaymentRequestDTO.StatusEnum.SENT_TO_EXTERNAL_PP;
            await databaseService.SaveAsync<PaymentRequestDTO>(paymentRequest);
            //paymentRequest.Status=PaymentRequestDTO.StatusEnum.CONFIRMED;
            //await databaseService.SaveAsync<PaymentRequestDTO>(paymentRequest);
            Console.WriteLine($"Payment Date={paymentRequest.PaymentDate}");
            Console.WriteLine($"Payment ID={paymentRequest.PaymentsFileGUID}");
            Console.WriteLine("Ended CreatePaymentRequestAsync.");
        }

        public async Task ProcessPaymentsAsync(PaymentRequestDTO paymentRequest)
        {
            Console.WriteLine("Started ProcessPaymentsAsync...");
            if(String.IsNullOrEmpty(paymentRequest.PaymentsFileGUID)) throw new Exception("Please ensure that PaymentsFileGUID is valid!");
            if(paymentRequest.PaymentDate==null) throw new Exception("Please ensure that PaymentDate is valid!");
            
            Console.WriteLine($"Payment Date={paymentRequest.PaymentDate}");
            Console.WriteLine($"Payment ID={paymentRequest.PaymentsFileGUID}");
            Console.WriteLine("Ended ProcessPaymentsAsync.");
        }

        public async Task CreatePaymentsResponseAsync(PaymentRequestDTO paymentRequest)
        {
            Console.WriteLine("Started CreatePaymentsResponseAsync...");
            if(String.IsNullOrEmpty(paymentRequest.PaymentsFileGUID)) throw new Exception("Please ensure that PaymentsFileGUID is valid!");
            if(paymentRequest.PaymentDate==null) throw new Exception("Please ensure that PaymentDate is valid!");
            
            Console.WriteLine($"Payment Date={paymentRequest.PaymentDate}");
            Console.WriteLine($"Payment ID={paymentRequest.PaymentsFileGUID}");
            Console.WriteLine("Ended CreatePaymentsResponseAsync.");
        }            
    }
}