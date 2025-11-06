using Ava.Xioa.Common.Attributes;

namespace Ava.Xioa.Common.Models;

/// <summary>
/// 导航栏信息模型类，继承自ReactiveObject，提供响应式属性支持
/// </summary>
public partial class NavigableBarInfoModel : ReactiveObject
{
    /// <summary>
    /// 导航栏名称属性
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 目标视图名称属性，用于导航到指定视图
    /// </summary>
    public string TargetView { get; set; }

    /// <summary>
    /// 区域名称属性，用于标识导航栏所属的区域
    /// </summary>
    public string RegionName { get; set; }

    /// <summary>
    /// 私有字段，表示导航栏项是否被选中
    /// 使用[ObservableBindProperty]特性可以自动生成对应的响应式属性绑定
    /// </summary>
    private bool _isCheck;

    public bool IsCheck
    {
        get => _isCheck;
        set => this.SetProperty(ref _isCheck, value);
    }
}