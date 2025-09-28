using SukiUI.Enums;

namespace Ava.Xioa.Infrastructure.Models.Models.ThemesModels;

public class SukiBackgroundStyleDesc
{
    public SukiBackgroundStyle SukiBackgroundStyle { get; set; }
    public string Description { get; set; }

    public int Key { get; set; }

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
        new SukiBackgroundStyleDesc(SukiBackgroundStyle.Gradient, "渐变背景")
        {
            Key = 0
        },
        new SukiBackgroundStyleDesc(SukiBackgroundStyle.GradientSoft, "渐变背景(柔和)")
        {
            Key = 1
        },
        new SukiBackgroundStyleDesc(SukiBackgroundStyle.GradientDarker, "渐变背景(暗色)")
        {
            Key = 2
        },
        new SukiBackgroundStyleDesc(SukiBackgroundStyle.Flat, "纯色扁平背景")
        {
            Key = 3
        },
        new SukiBackgroundStyleDesc(SukiBackgroundStyle.Bubble, "气泡效果背景")
        {
            Key = 4
        }
    ];
}