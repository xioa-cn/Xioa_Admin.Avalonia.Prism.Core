using SukiUI.Enums;

namespace Ava.Xioa.Infrastructure.Models.Models.ThemesModels;

public class SukiBackgroundStyleDesc
{
    public SukiBackgroundStyle SukiBackgroundStyle { get; set; }
    public string Description { get; set; }

    public SukiBackgroundStyleDesc(SukiBackgroundStyle sukiBackgroundStyle, string description)
    {
        SukiBackgroundStyle = sukiBackgroundStyle;
        Description = description;
    }

    public override string ToString()
    {
        return Description;
    }

    public static SukiBackgroundStyleDesc[] SukiBackgroundStyleDescs =
    [
        new SukiBackgroundStyleDesc(SukiBackgroundStyle.Gradient, "渐变背景"),
        new SukiBackgroundStyleDesc(SukiBackgroundStyle.GradientSoft, "渐变背景(柔和)"),
        new SukiBackgroundStyleDesc(SukiBackgroundStyle.GradientDarker, "渐变背景(暗色)"),
        new SukiBackgroundStyleDesc(SukiBackgroundStyle.Flat, "纯色扁平背景"),
        new SukiBackgroundStyleDesc(SukiBackgroundStyle.Bubble, "气泡效果背景")
    ];
}