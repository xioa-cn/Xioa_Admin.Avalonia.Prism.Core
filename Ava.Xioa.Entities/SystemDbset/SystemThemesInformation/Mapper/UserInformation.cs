using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Entities.Models;

namespace Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;

[Table("Ava_UserInformation")]
public class UserInformation : SystemEntity
{
    [Column("UserName")]
    [Required]
    [MaxLength(255)]
    public string UserName { get; set; }

    [Column("Account")]
    [Required]
    [MaxLength(255)]
    public string Account { get; set; }

    [Column("Password")]
    [Required]
    [MaxLength(255)]
    public string Password { get; set; }

    [Column("UserAuth")] [Required] public UserAuthEnum UserAuth { get; set; }
}