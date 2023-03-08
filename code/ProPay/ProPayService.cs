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

        public ProPayService(ICryptoService cryptoService) {
            this.cryptoService=cryptoService;
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

        public async Task<string> SendToPaymentProviderAsync(FileInfo paymentsFile)
        {
            Console.WriteLine("ProcessPaymentsAsync started...");
            var encryptedFile=await cryptoService.EncryptAsync(paymentsFile);
            var encryptedContent=File.ReadAllBytes(encryptedFile.FullName);
            var uri="http://";
            var httpContent = new ByteArrayContent(encryptedContent);
            var httpClient=new HttpClient();
            httpClient.Timeout=TimeSpan.FromSeconds(3);
            var resp=await httpClient.PostAsync(uri, httpContent);
            await resp.Content.ReadAsStringAsync();
            var paymentReceiptGUID=resp.Content.Headers.GetValues("paymentReceiptGUID").SingleOrDefault();
            if(String.IsNullOrEmpty(paymentReceiptGUID)) throw new Exception("The payment receipt GUID from ProPay API was null, failed!");
            Console.WriteLine("ProcessPaymentsAsync ended.");
            return paymentReceiptGUID;
        }
    }
}