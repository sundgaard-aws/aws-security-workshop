using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OM.AWS.Demo.DTL;

namespace OM.AWS.Demo.SL
{
    public interface IPaymentService
    {
        public Task ProcessPaymentsAsync(List<PaymentDTO> payments);
        public Task ProcessPaymentsAsync(FileInfo paymentsFile);
    }
}