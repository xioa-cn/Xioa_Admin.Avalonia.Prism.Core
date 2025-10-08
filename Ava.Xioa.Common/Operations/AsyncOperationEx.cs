using System;
using System.Threading.Tasks;

namespace Ava.Xioa.Common.Operations;

public static class AsyncOperation
{
    public static AsyncOperation<T> FromAsync<T>(Func<Task<T>> asyncFunc)
    {
        return new AsyncOperation<T>(asyncFunc);
    }
}