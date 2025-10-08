using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ava.Xioa.Common.Operations;

public class AsyncOperation<T>
{
    // 原始异步方法
    private readonly Func<Task<T>> _asyncFunc;
    // 存储异常处理链（支持多个Catch）
    private readonly List<Func<Exception, Task<(bool handled, T result)>>> _errorHandlers = new();

    // 构造函数：仅初始化一次原始异步方法
    public AsyncOperation(Func<Task<T>> asyncFunc)
    {
        _asyncFunc = asyncFunc;
    }

    // Catch不再创建新实例，而是将处理器加入当前实例的列表
    public AsyncOperation<T> Catch(Func<Exception, Task<T>> errorHandler)
    {
        _errorHandlers.Add(async ex => 
        {
            var result = await errorHandler(ex);
            return (true, result); // 标记为已处理
        });
        return this; // 返回当前实例，支持链式调用
    }

    // 简化版Catch：仅处理异常，返回默认值
    public AsyncOperation<T> Catch(Func<Exception, Task> errorHandler, T defaultValue)
    {
        _errorHandlers.Add(async ex => 
        {
            await errorHandler(ex);
            return (true, defaultValue);
        });
        return this;
    }
    
    public async Task<T> Execute()
    {
        try
        {
            return await _asyncFunc();
        }
        catch (Exception ex)
        {
            // 按注册顺序尝试每个异常处理器
            foreach (var handler in _errorHandlers)
            {
                var (handled, result) = await handler(ex);
                if (handled)
                {
                    return result; // 找到处理者，返回结果
                }
            }
            // 没有处理器处理，继续抛出
            throw;
        }
    }
}