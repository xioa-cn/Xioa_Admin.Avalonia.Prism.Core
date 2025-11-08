using System.ComponentModel.DataAnnotations.Schema;
using Ava.Xioa.Entities.Models;

namespace Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;

[Table("Ava_NowUserInformation")]
public class NowUserInformation : SystemEntity
{
    public string Account { get; set; }
    
    public string Password { get; set; }
    
    public bool RememberPassword { get; set; }
    
    public bool AutoLogin { get; set; }
}