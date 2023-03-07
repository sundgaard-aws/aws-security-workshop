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

        public Task ProcessPaymentsAsync(List<PaymentDTO> payments)
        {
            Console.WriteLine("ProcessPaymentsAsync started...");
            var paymentsJSON=JsonSerializer.Serialize(payments);
            cryptoService.Encrypt(paymentsJSON);
            Console.WriteLine("ProcessPaymentsAsync ended.");
            return Task.CompletedTask;
        }

        public Task ProcessPaymentsAsync(FileInfo paymentsFile)
        {
            Console.WriteLine("ProcessPaymentsAsync started...");
            cryptoService.Encrypt(paymentsFile);
            Console.WriteLine("ProcessPaymentsAsync ended.");
            return Task.CompletedTask;
        }
    }
}