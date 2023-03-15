using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OM.AWS.Demo.DTL;
using OM.AWS.Demo.SL;

namespace OM.AWS.Demo.ProPay
{
    public class ProPayService : IPaymentService
    {
        private ICryptoService cryptoService;
        private ISettingsService settingsService;
        private IAppContextService appContextService;
        private IObjectStoreService objectStoreService;
        private string TempPath { get { return Path.GetTempPath(); } }

        public ProPayService(ICryptoService cryptoService, ISettingsService settingsService, IAppContextService appContextService, IObjectStoreService objectStoreService) {
            this.cryptoService=cryptoService;
            this.settingsService=settingsService;
            this.appContextService=appContextService;
            this.objectStoreService=objectStoreService;
        }

        public async Task ProcessPaymentsAsync(List<PaymentDTO> payments)
        {
            Console.WriteLine("ProcessPaymentsAsync started...");
            var paymentsJSON=JsonSerializer.Serialize(payments);
            await cryptoService.EncryptAsync(paymentsJSON);
            Console.WriteLine("ProcessPaymentsAsync ended.");
        }

        public async Task ReceiveFromPaymentProviderAsync(string paymentReceiptGUID)
        {
            Console.WriteLine("ProcessPaymentsAsync started...");            
            var encryptedFile=await cryptoService.EncryptAsync(new FileInfo(""));
            var encryptedContent=File.ReadAllBytes(encryptedFile.FullName);
            var uri="http://";
            var httpContent = new ByteArrayContent(encryptedContent);
            var httpClient=new HttpClient();
            var resp=await httpClient.PostAsync(uri, httpContent);
            resp.Content.ReadAsByteArrayAsync();
            Console.WriteLine("ProcessPaymentsAsync ended.");
        }

        public async Task<string> SendToPaymentProviderAsync(string paymentsData)
        {
            Console.WriteLine("ProcessPaymentsAsync started...");
            var encryptedPaymentsData=await cryptoService.EncryptAsync(paymentsData);
            //var encryptedContent=File.ReadAllBytes(encryptedFile.FullName);
            var proPayRequestFunctionURL=await settingsService.GetSettingAsync(this.appContextService.GetAppPrefix()+"ProPayRequestFunctionURL");     
            var httpContent = new StringContent(encryptedPaymentsData);
            var httpClient=new HttpClient();
            httpClient.Timeout=TimeSpan.FromSeconds(3);
            var resp=await httpClient.PostAsync(proPayRequestFunctionURL, httpContent);
            var proPayResponseJSON=await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"proPayResponseJSON {proPayResponseJSON}");
            var proPayResponse=JsonSerializer.Deserialize<ProPayResponseDTO>(proPayResponseJSON);
            //var paymentReceiptGUID=resp.Content.Headers.GetValues("paymentReceiptGUID").SingleOrDefault();
            if(proPayResponse==null || String.IsNullOrEmpty(proPayResponse.TransactionID)) throw new Exception("The payment receipt GUID from ProPay API was null, failed!");
            var paymentRequestBucketName=await settingsService.GetSettingAsync(this.appContextService.GetAppPrefix()+"PaymentRequestBucketName");
            var tempFileName=Path.Join(TempPath, Guid.NewGuid()+".pgp");
            File.WriteAllText(tempFileName, encryptedPaymentsData);
            await objectStoreService.UploadObjectAsync(paymentRequestBucketName, new FileInfo(tempFileName));
            Console.WriteLine("ProcessPaymentsAsync ended.");
            return proPayResponse.TransactionID;
        }
    }
}