using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using OM.AWS.Demo.DTL;
using OM.AWS.Demo.SL;
using PgpCore;

namespace PGPCrypto
{
    public class PGPCryptoService : ICryptoService
    {
        private const string PGPPublicKeyName="pgp-public.key";
        private const string PGPPrivateKeyName="pgp-private.key";
        private string? userName;
        private string? passPhrase;
        private ISecretsService secretsService;
        private ISettingsService settingsService;
        private IAppContextService appContextService;

        private string TempPath { get { return Path.Join(Path.GetTempPath(),"pgp"); } }

        public PGPCryptoService(ISecretsService secretsService, ISettingsService settingsService, IAppContextService appContextService) {
            this.secretsService=secretsService;
            this.settingsService=settingsService;
            this.appContextService=appContextService;
        }

        public void LazyInit(string userName, string passPhrase) {
            if(String.IsNullOrEmpty(userName)) throw new Exception("Please set userName!");
            if(String.IsNullOrEmpty(passPhrase)) throw new Exception("Please set passPhrase!");
            this.userName=userName;
            this.passPhrase=passPhrase;
        }        

        public async Task<KeyPairDTO> GenerateKeyPairAsFilesAsync() {
            Directory.CreateDirectory(TempPath);
            PGP.Instance.GenerateKey(Path.Join(TempPath,PGPPublicKeyName), Path.Join(TempPath,PGPPrivateKeyName),this.userName,this.passPhrase);
            var kp=new KeyPairDTO {
                PGPPublicKeyName=PGPPublicKeyName,
                PGPPrivateKeyName=PGPPrivateKeyName
            };
            Console.WriteLine($"Key pair generated in folder={TempPath}");
            return kp;
        }

        public async Task<CryptoMaterialDTO> GenerateKeyPairAsync() {
            var pgpPublicKeyStream=new MemoryStream();
            var pgpPrivateKeyStream=new MemoryStream();
            PGP.Instance.GenerateKey(pgpPublicKeyStream,pgpPrivateKeyStream,this.userName,this.passPhrase);
            pgpPublicKeyStream.Seek(0,0);
            pgpPrivateKeyStream.Seek(0,0);
            var publicKeyMaterial=await new StreamReader(pgpPublicKeyStream).ReadToEndAsync();
            var privateKeyMaterial=await new StreamReader(pgpPrivateKeyStream).ReadToEndAsync();
            //Console.WriteLine($"publicKeyMaterial {publicKeyMaterial}");
            //Console.WriteLine($"privateKeyMaterial {privateKeyMaterial}");
            var keyMaterial=new CryptoMaterialDTO {
                PublicKey=publicKeyMaterial,
                PrivateKey=privateKeyMaterial
            };
            var cryptoMaterialSecretName=await settingsService.GetSettingAsync(this.appContextService.GetAppPrefix()+"CryptoMaterialSecretName");
            Console.WriteLine($"Updating key material in secret...");
            await secretsService.UpdateSecret<CryptoMaterialDTO>(cryptoMaterialSecretName, keyMaterial);
            Console.WriteLine($"Key pair generated.");
            return keyMaterial;
        }        

        private async Task<EncryptionKeys> GetEncryptionKeys() {
            var cryptoMaterialSecretName=await settingsService.GetSettingAsync(this.appContextService.GetAppPrefix()+"CryptoMaterialSecretName");     
            var cryptoMaterial=await secretsService.RestoreSecret<CryptoMaterialDTO>(cryptoMaterialSecretName);
            //var secret=new SecretDTO();
            //secretsService.CreateSecret("secret1", secret).ConfigureAwait(false).GetAwaiter().GetResult();
            //var secret = secretsService.RestoreSecret<SecretDTO>("demo/secret2").ConfigureAwait(false).GetAwaiter().GetResult();
            //Console.WriteLine($"KeyType={secret.keyType}");
            //var encKeys=new EncryptionKeys(new FileInfo(Path.Join(TempPath,PGPPublicKeyName)), new FileInfo(Path.Join(TempPath,PGPPrivateKeyName)), this.passPhrase);
            var encKeys=new EncryptionKeys(cryptoMaterial.PublicKey, cryptoMaterial.PrivateKey, this.passPhrase);
            return encKeys;
        }

        public async Task<FileInfo> EncryptAsync(FileInfo dataFile) {
            using (PGP pgp = new PGP(await GetEncryptionKeys()))
            {                
                Console.WriteLine(@$"Encrypting file via PGP {TempPath}...");
                var encryptedFilePath=Path.Join(TempPath,"content__encrypted.pgp");
                Directory.CreateDirectory(encryptedFilePath);
                var encryptedFile=new FileInfo(encryptedFilePath);
                
                pgp.EncryptFile(dataFile, encryptedFile);
                Console.WriteLine(@$"Done encrypting file. File is in {TempPath}.");
                return encryptedFile;
            }
        }

        public async Task<string> EncryptAsync(string data) {
            using (PGP pgp = new PGP(await GetEncryptionKeys()))
            {                
                Console.WriteLine(@$"Encrypting data via PGP...");
                var dataMemStream=new MemoryStream();
                MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(data)); 
                var encryptedMemStream=new MemoryStream();
                pgp.EncryptStream(dataMemStream, encryptedMemStream);
                Console.WriteLine(@$"Done encrypting data.");
                encryptedMemStream.Seek(0,0);
                var buffer=encryptedMemStream.GetBuffer();
                return Convert.ToBase64String(buffer);
            }
        }        

        public async Task<FileInfo> DecryptAsync(FileInfo encryptedFile) {
            using (PGP pgp = new PGP(await GetEncryptionKeys()))
            {                
                var decryptedFile=new FileInfo(Path.Join(TempPath,"content__decrypted.txt"));
                pgp.DecryptFile(new FileInfo(Path.Join(TempPath,"content__encrypted.pgp")), decryptedFile);
                return new FileInfo(Path.Join(TempPath,"content__decrypted.txt"));
            }
        }

        public async Task<string> Decrypt(string encryptedData) {
            using (PGP pgp = new PGP(await GetEncryptionKeys()))
            {                
                var decryptedFile=new FileInfo(Path.Join(TempPath,"content__decrypted.txt"));
                pgp.DecryptFile(new FileInfo(Path.Join(TempPath,"content__encrypted.pgp")), decryptedFile);
                return File.ReadAllText(Path.Join(TempPath,"content__decrypted.txt"));
            }
        }        

        public void Simple() {
            Directory.CreateDirectory(TempPath);
            PGP.Instance.GenerateKey(Path.Join(TempPath,PGPPublicKeyName), Path.Join(TempPath,PGPPrivateKeyName), this.userName, this.passPhrase);
            var encKeys=new EncryptionKeys(new FileInfo(Path.Join(TempPath,PGPPublicKeyName)), new FileInfo(Path.Join(TempPath,PGPPrivateKeyName)), this.passPhrase);
            using (PGP pgp = new PGP(encKeys))
            {                
                Console.WriteLine(@$"New PGP key created in folder {TempPath}.");
                pgp.EncryptFile(new FileInfo(Path.Join(TempPath,"content.txt")), new FileInfo(Path.Join(TempPath,"content__encrypted.pgp")));
                pgp.DecryptFile(new FileInfo(Path.Join(TempPath,"content__encrypted.pgp")), new FileInfo(Path.Join(TempPath,"content__decrypted.txt")));
            }
        }

        public void All()
        {
            var tempPath=Path.Join(Path.GetTempPath(),"pgp");
            using (PGP pgp = new PGP())
            {
                pgp.GenerateKey(Path.Join(tempPath,PGPPublicKeyName), Path.Join(tempPath,PGPPrivateKeyName), "email@email.com", "password");
                pgp.EncryptFile(@"C:\TEMP\keys\content.txt", @"C:\TEMP\keys\content__encrypted.pgp", Path.Join(tempPath,PGPPublicKeyName), true, true);
                pgp.EncryptFileAndSign(@"C:\TEMP\keys\content.txt", @"C:\TEMP\keys\content__encrypted_signed.pgp", Path.Join(tempPath,PGPPublicKeyName), Path.Join(tempPath,PGPPrivateKeyName), "password", true, true);
                pgp.DecryptFile(@"C:\TEMP\keys\content__encrypted.pgp", @"C:\TEMP\keys\content__decrypted.txt", Path.Join(tempPath,PGPPrivateKeyName), "password");
                pgp.DecryptFile(@"C:\TEMP\keys\content__encrypted_signed.pgp", @"C:\TEMP\keys\content__decrypted_signed.txt", Path.Join(tempPath,PGPPrivateKeyName), "password");

                // Encrypt stream
                using (FileStream inputFileStream = new FileStream(@"C:\TEMP\keys\content.txt", FileMode.Open))
                using (Stream outputFileStream = File.Create(@"C:\TEMP\keys\content__encrypted2.pgp"))
                using (Stream publicKeyStream = new FileStream(Path.Join(tempPath,PGPPublicKeyName), FileMode.Open))
                    pgp.EncryptStream(inputFileStream, outputFileStream, publicKeyStream, true, true);

                // Decrypt stream
                using (FileStream inputFileStream = new FileStream(@"C:\TEMP\keys\content__encrypted2.pgp", FileMode.Open))
                using (Stream outputFileStream = File.Create(@"C:\TEMP\keys\content__decrypted2.txt"))
                using (Stream privateKeyStream = new FileStream(Path.Join(tempPath,PGPPrivateKeyName), FileMode.Open))
                    pgp.DecryptStream(inputFileStream, outputFileStream, privateKeyStream, "password");
            }
        }

    }
}