using System.IO;
using System.Threading.Tasks;

namespace OM.AWS.Demo.SL
{
    public interface IObjectStoreService
    {
        public Task UploadObjectAsync(string path, FileInfo file);
        public Task<FileInfo> GetObjectAsync(string objectStoreName, string objectName);
    }
}