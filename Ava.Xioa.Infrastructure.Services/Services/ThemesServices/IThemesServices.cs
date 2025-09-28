using System;
using System.Windows.Input;
using Ava.Xioa.Infrastructure.Models.Models.ThemesModels;
using Avalonia.Collections;
using SukiUI.Models;

namespace Ava.Xioa.Infrastructure.Services.Services.ThemesServices;

public interface IThemesServices
{
    /// <summary>
    /// 主题颜色
    /// </summary>
    IAvaloniaReadOnlyList<SukiColorTheme> AvailableColors { get; }

    /// <summary>
    /// 背景样式
    /// </summary>
    IAvaloniaReadOnlyList<SukiBackgroundStyleDesc> AvailableBackgroundStyles { get; }

    SukiBackgroundStyleDesc BackgroundStyle { get; set; }

    /// <summary>
    /// 背景动画
    /// </summary>
    bool BackgroundAnimations { get; set; }

    bool BackgroundTransitions { get; set; }

    /// <summary>
    /// 明亮主题
    /// </summary>
    bool IsLightTheme { get; set; }

    /// <summary>
    /// 开启动画
    /// </summary>
    Action<bool?>? AnimationsEnabledChanged { get; set; }

    /// <summary>
    /// 修改背景
    /// </summary>
    Action<string?>? CustomBackgroundStyleChanged { get; set; }

    Action<bool?>? BackgroundTransitionsChanged { get; set; }

    Action<SukiBackgroundStyleDesc?>? BackgroundStyleChanged { get; set; }

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