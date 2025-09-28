using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ava.Xioa.Entities.Models;

namespace Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;

[Table("Ava_SystemThemesInformation")]
public class SystemThemesInformation : SystemEntity
{
    [Column("BackgroundStyleKey")] public int BackgroundStyleKey { get; set; }

    [Column("IsLightTheme")] public bool IsLightTheme { get; set; }
    
    [Column("Animation")]
    public bool Animation { get; set; }

    [MaxLength(255)]
    [Required]
    [Column("ColorThemeDisplayName")]
    public string ColorThemeDisplayName { get; set; }
    
    [MaxLength(255)]
    [Column("BackgroundEffectKey")]
    public string? BackgroundEffectKey { get; set; }
}