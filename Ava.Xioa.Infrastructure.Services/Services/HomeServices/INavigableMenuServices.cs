using Ava.Xioa.Common.Models;
using Avalonia.Collections;

namespace Ava.Xioa.Infrastructure.Services.Services.HomeServices;

public interface INavigableMenuServices
{
    IAvaloniaReadOnlyList<NavigableMenuItemModel> NavigableMenuItems { get; }
    
    object? SelectedView { get; set; }
}