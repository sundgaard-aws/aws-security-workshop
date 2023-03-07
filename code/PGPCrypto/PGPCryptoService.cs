using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using OM.AWS.Demo.SL;
using PgpCore;

namespace PGPCrypto
{
    public class PGPCryptoService : ICryptoService
    {
        private const string PGPPublicKeyName="pgp-public.key";
        private const string PGPPrivateKeyName="pgp-private.key";
        private const string passphrase="password";

        private string TempPath { get { return Path.Join(Path.GetTempPath(),"pgp"); } }
        public void GenerateKeyPair() {
            Directory.CreateDirectory(TempPath);
            PGP.Instance.GenerateKey(Path.Join(TempPath,PGPPublicKeyName), Path.Join(TempPath,PGPPrivateKeyName), "email@email.com", passphrase);            
        }

        private EncryptionKeys GetEncryptionKeys() {
            //var secret=new SecretDTO();
            //secretsService.CreateSecret("secret1", secret).ConfigureAwait(false).GetAwaiter().GetResult();
            //var secret = secretsService.RestoreSecret<SecretDTO>("demo/secret2").ConfigureAwait(false).GetAwaiter().GetResult();
            //Console.WriteLine($"KeyType={secret.keyType}");
            var encKeys=new EncryptionKeys(new FileInfo(Path.Join(TempPath,PGPPublicKeyName)), new FileInfo(Path.Join(TempPath,PGPPrivateKeyName)), passphrase);
            return encKeys;
        }

        public async Task<FileInfo> Encrypt(FileInfo dataFile) {
            using (PGP pgp = new PGP(GetEncryptionKeys()))
            {                
                Console.WriteLine(@$"Encrypting file via PGP {TempPath}...");
                var encryptedFile=new FileInfo(Path.Join(TempPath,"content__encrypted.pgp"));
                pgp.EncryptFile(dataFile, encryptedFile);
                Console.WriteLine(@$"Done encrypting file. File is in {TempPath}.");
                return encryptedFile;
            }
        }

        public async Task<string> Encrypt(string data) {
            using (PGP pgp = new PGP(GetEncryptionKeys()))
            {                
                Console.WriteLine(@$"Encrypting data via PGP...");
                var dataMemStream=new MemoryStream();
                MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(data)); 
                var encryptedMemStream=new MemoryStream();
                pgp.EncryptStream(dataMemStream, encryptedMemStream);
                Console.WriteLine(@$"Done encrypting data.");
                return await encryptedMemStream.GetStringAsync();
            }
        }        

        public async Task<FileInfo> Decrypt(FileInfo encryptedFile) {
            using (PGP pgp = new PGP(GetEncryptionKeys()))
            {                
                var decryptedFile=new FileInfo(Path.Join(TempPath,"content__decrypted.txt"));
                pgp.DecryptFile(new FileInfo(Path.Join(TempPath,"content__encrypted.pgp")), decryptedFile);
                return new FileInfo(Path.Join(TempPath,"content__decrypted.txt"));
            }
        }

        public async Task<string> Decrypt(string encryptedData) {
            using (PGP pgp = new PGP(GetEncryptionKeys()))
            {                
                var decryptedFile=new FileInfo(Path.Join(TempPath,"content__decrypted.txt"));
                pgp.DecryptFile(new FileInfo(Path.Join(TempPath,"content__encrypted.pgp")), decryptedFile);
                return File.ReadAllText(Path.Join(TempPath,"content__decrypted.txt"));
            }
        }        

        public void Simple() {
            Directory.CreateDirectory(TempPath);
            PGP.Instance.GenerateKey(Path.Join(TempPath,PGPPublicKeyName), Path.Join(TempPath,PGPPrivateKeyName), "email@email.com", passphrase);
            var encKeys=new EncryptionKeys(new FileInfo(Path.Join(TempPath,PGPPublicKeyName)), new FileInfo(Path.Join(TempPath,PGPPrivateKeyName)), passphrase);
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