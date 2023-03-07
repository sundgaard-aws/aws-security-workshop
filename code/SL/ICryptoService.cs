
using System.IO;
using System.Threading.Tasks;
using OM.AWS.Demo.DTL;

namespace OM.AWS.Demo.SL {
    public interface ICryptoService
    {
        Task<string> EncryptAsync(string data);
        Task<FileInfo> EncryptAsync(FileInfo dataFile);
        Task<KeyPairDTO> GenerateKeyPairAsync();
        void LazyInit(string userName, string passPhrase);
    }
}