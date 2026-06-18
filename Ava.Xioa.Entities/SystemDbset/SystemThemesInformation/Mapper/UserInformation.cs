using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Entities.Models;

namespace Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;

[Table("Ava_UserInformation")]
public class UserInformation : SystemEntity, INotifyProperty
{
    [Column("UserName")]
    [Required]
    [MaxLength(255)]
    public string UserName
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged();
        }
    }

    [Column("Account")]
    [Required]
    [MaxLength(255)]
    public string Account
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged();
        }
    }

    [Column("Password")]
    [Required]
    [MaxLength(255)]
    public string Password
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged();
        }
    }


    [Column("UserAuth")]
    [Required]
    public UserAuthEnum UserAuth
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}