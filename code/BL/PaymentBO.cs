using System;
using System.Collections.Generic;
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

        public PaymentBO(IAppContextService appContextService, IObjectStoreService objectStoreService, ISettingsService settingsService, IDatabaseService databaseService, IPaymentService paymentService) {
            this.paymentService=paymentService;
            this.objectStoreService=objectStoreService;
            this.databaseService=databaseService;
            this.settingsService=settingsService;
            this.appContextService=appContextService;
        }

        public async Task ProcessPaymentsAsync(PaymentRequestDTO paymentRequest)
        {
            Console.WriteLine("Started ProcessPaymentsAsync...");
            //var secret=new SecretDTO();
            //secretsService.CreateSecret("secret1", secret).ConfigureAwait(false).GetAwaiter().GetResult();
            //var secret = secretsService.RestoreSecret<SecretDTO>("demo/secret2").ConfigureAwait(false).GetAwaiter().GetResult();
            //Console.WriteLine($"KeyType={secret.keyType}");
            if(String.IsNullOrEmpty(paymentRequest.PaymentsFileGUID)) throw new Exception("Please ensure that PaymentsFileGUID is valid!");
            if(paymentRequest.PaymentDate==null) throw new Exception("Please ensure that PaymentDate is valid!");
            
            var paymentInputBucketName=await settingsService.GetSettingAsync(this.appContextService.GetAppPrefix()+"PaymentInputBucketName");     
            paymentRequest.Status=PaymentRequestDTO.StatusEnum.CREATED;       
            await databaseService.SaveAsync<PaymentRequestDTO>(paymentRequest);
            var paymentsFile=await objectStoreService.GetObjectAsync(paymentInputBucketName, paymentRequest.PaymentsFileGUID);
            await paymentService.ProcessPaymentsAsync(paymentsFile);           
            paymentRequest.Status=PaymentRequestDTO.StatusEnum.SENT_TO_EXTERNAL_PP; 
            await databaseService.SaveAsync<PaymentRequestDTO>(paymentRequest);
            //paymentRequest.Status=PaymentRequestDTO.StatusEnum.CONFIRMED;
            //await databaseService.SaveAsync<PaymentRequestDTO>(paymentRequest);
            Console.WriteLine($"Payment Date={paymentRequest.PaymentDate}");
            Console.WriteLine($"Payment ID={paymentRequest.PaymentsFileGUID}");
            Console.WriteLine("Ended ProcessPaymentsAsync.");
        }
    }
}