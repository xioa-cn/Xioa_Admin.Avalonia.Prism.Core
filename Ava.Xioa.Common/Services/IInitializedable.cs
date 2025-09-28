using System.Threading.Tasks;

namespace Ava.Xioa.Common.Services;

public interface IInitializedable
{
    void Initialized();
}

public interface IInitializedAsyncable
{
    Task InitializedAsync();
}