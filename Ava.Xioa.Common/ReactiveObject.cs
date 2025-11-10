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

    protected readonly ToastsService? ToastsService;

    public ReactiveObject(ToastsService? toastsService)
    {
        ToastsService = toastsService;
    }
}