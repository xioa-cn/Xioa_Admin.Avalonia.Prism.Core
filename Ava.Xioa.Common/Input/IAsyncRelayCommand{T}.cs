using System.Threading.Tasks;

namespace Ava.Xioa.Common.Input;

public interface IAsyncRelayCommand<in T> : IAsyncRelayCommand, IRelayCommand<T>
{
    Task ExecuteAsync(T? parameter);
}