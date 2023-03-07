using System;
using System.Collections.Generic;
using System.IO;
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

        public async Task ProcessPaymentsAsync(FileInfo paymentsFile)
        {
            Console.WriteLine("ProcessPaymentsAsync started...");
            await cryptoService.EncryptAsync(paymentsFile);
            Console.WriteLine("ProcessPaymentsAsync ended.");
        }
    }
}