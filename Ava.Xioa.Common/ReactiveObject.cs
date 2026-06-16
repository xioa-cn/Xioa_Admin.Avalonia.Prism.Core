using Ava.Xioa.Common.Services;

namespace Ava.Xioa.Common;

/// <summary>
/// 核心 双向数据绑定功能
/// </summary>
public partial class ReactiveObject : ObservableBindBase
{
    public ReactiveObject()
    {
    }

    protected readonly IToastsService? ToastsService;

    public ReactiveObject(IToastsService? toastsService)
    {
        ToastsService = toastsService;
    }
}