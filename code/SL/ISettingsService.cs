using System.Threading.Tasks;

namespace OM.AWS.Demo.SL
{
    public interface ISettingsService {
        Task<string> GetSettingAsync(string settingName);
        Task PutSettingAsync<T>(string settingName, string value);
    }
}