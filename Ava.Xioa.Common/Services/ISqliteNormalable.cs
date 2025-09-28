using System.Threading.Tasks;

namespace Ava.Xioa.Common.Services;

public interface ISqliteNormalable
{
    public Task DbFileExistOrCreateAsync();
}