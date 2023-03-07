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
    Console.WriteLine("Started Simple Console...");

    var serviceProvider=AppConfig.Wireup();

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

    cryptoService.LazyInit("demo@mydomain.com", "12345678ABCdef");
    await cryptoService.GenerateKeyPairAsync();

    var paymentRequest=new PaymentRequestDTO{ PaymentsFileGUID="61b889b2-10fc-4dcd-a280-8720cfa50c7f",PaymentDate=new DateTime(2023,3,1)};
    await paymentBO.ProcessPaymentsAsync(paymentRequest);
    //var secret=new SecretDTO();
    //secretsService.CreateSecret("secret1", secret).ConfigureAwait(false).GetAwaiter().GetResult();
    //var secret=secretsService.RestoreSecret<SecretDTO>("demo/secret2").ConfigureAwait(false).GetAwaiter().GetResult();
    //Console.WriteLine($"KeyType={secret.keyType}");
    //var encryptedFile=pgpCryptFacade.EncryptFile();
    //await objectStore.UploadFile("pgp-demo-output-archive", encryptedFile);
    //ftpService.UploadFile(encryptedFile, "/pgp-demo-output-archive/demo-user/content.aaaa");
    //crypt.GenerateKeyPair();
    //var decryptedContents=crypt.DecryptFile();
    //Console.WriteLine($"Decrypted contents are {decryptedContents}");
    Console.WriteLine("Ended Simple Console.");
    }
}