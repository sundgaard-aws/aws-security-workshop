
using System.IO;
using System.Threading.Tasks;

namespace OM.AWS.Demo.SL {
    public interface ICryptoService
    {
        Task<string> Encrypt(string data);
        Task<FileInfo> Encrypt(FileInfo dataFile);
    }
}