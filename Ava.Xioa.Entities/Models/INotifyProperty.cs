using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ava.Xioa.Entities.Models;

public interface INotifyProperty : INotifyPropertyChanged
{
    void RaisePropertyChanged([CallerMemberName] string? propertyName = null);
}