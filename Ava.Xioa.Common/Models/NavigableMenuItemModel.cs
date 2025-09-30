using Material.Icons;

namespace Ava.Xioa.Common.Models;

public class ResourcesRouters
{
    public NavigableMenuItemModel[] Routers { get; set; }
}

public class NavigableMenuItemModel
{
    public NavigableMenuItemModel(string key)
    {
        Key = key;
    }

    public NavigableMenuItemModel()
    {
        
    }

    public string Header { get; set; }

    public string Key { get; set; }

    public MaterialIconKind IconKind { get; set; }

    public NavigableMenuItemModel[]? Children { get; set; }

    public bool HasChildren => Children is not null && Children.Length > 0;

    public string NavigationName { get; set; }

    public string Region { get; set; }
    
    
}