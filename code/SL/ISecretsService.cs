using System.Threading.Tasks;

namespace OM.AWS.Demo.SL
{
    public interface ISecretsService {
        Task CreateSecret<T>(string secretName, T secret);
        Task UpdateSecret<T>(string secretName, T secret);
        Task<T> RestoreSecret<T>(string secretName);
    }
}