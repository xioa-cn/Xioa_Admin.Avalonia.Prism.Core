using System;
using System.Windows.Input;

namespace Ava.Xioa.Infrastructure.Services.Services.ThemesServices;

public interface IThemesServices
{
    /// <summary>
    /// 开启动画
    /// </summary>
    Action<bool?>? AnimationsEnabledChanged { get; set; }

    /// <summary>
    /// 修改背景
    /// </summary>
    Action<string?>? CustomBackgroundStyleChanged { get; set; }

    /// <summary>
    /// 明暗主题切换
    /// </summary>
    ICommand LightThemeChangedCommand { get; }

    /// <summary>
    /// 背景效果切换
    /// </summary>
    ICommand BackgroundEffectCommand { get; }

    /// <summary>
    /// 动画启动
    /// </summary>
    ICommand AnimationsEnabledCommand { get; }

    /// <summary>
    /// 颜色主题切换
    /// </summary>
    ICommand SwitchToColorThemeCommand { get; }
}