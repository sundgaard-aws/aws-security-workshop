using System.Threading.Tasks;
using OM.AWS.Demo.DTL;

namespace OM.AWS.Demo.SL
{
    public interface IDatabaseService
    {
        public Task SaveAsync<T>(T item);
    }
}