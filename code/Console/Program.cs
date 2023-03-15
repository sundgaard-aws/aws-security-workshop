using Microsoft.Extensions.DependencyInjection;
using OM.AWS.Demo.BL;
using OM.AWS.Demo.Config;
using OM.AWS.Demo.DTL;
using OM.AWS.Demo.SL;

public class Program {
    public static void Main() {
        new Program().RunAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task RunAsync() {
        Console.WriteLine("Started Program...");
        var serviceProvider=AppConfig.Wireup();

        #region Check service initialization
        var paymentService=serviceProvider.GetService<IPaymentService>();
        if(paymentService==null) throw new Exception("Please make sure that IPaymentService is initialized!");
        var appContextService=serviceProvider.GetService<IAppContextService>();
        if(appContextService==null) throw new Exception("Please make sure that IAppContextService is initialized!");
        var settingService=serviceProvider.GetService<ISettingsService>();
        if(settingService==null) throw new Exception("Please make sure that ISettingsService is initialized!");
        var objectStore=serviceProvider.GetService<IObjectStoreService>();
        if(objectStore==null) throw new Exception("Please make sure that IObjectStoreService is initialized!");
        var databaseService=serviceProvider.GetService<IDatabaseService>();
        if(databaseService==null) throw new Exception("Please make sure that IDatabaseService is initialized!");
        var cryptoService=serviceProvider.GetService<ICryptoService>();
        if(cryptoService==null) throw new Exception("Please make sure that ICryptoService is initialized!");
        var paymentBO=serviceProvider.GetService<PaymentBO>();
        if(paymentBO==null) throw new Exception("Please make sure that PaymentBO is initialized!");
        #endregion

        cryptoService.LazyInit("demo@mydomain.com", "12345678ABCdef");
        await cryptoService.GenerateKeyPairAsync();

        var paymentRequest=new PaymentRequestDTO{ PaymentsFileGUID="61b889b2-10fc-4dcd-a280-8720cfa50c7f",PaymentDate=new DateTime(2023,3,1)};
        await paymentBO.CreatePaymentRequestAsync(paymentRequest);
        Console.WriteLine("Ended Program.");
    }
}