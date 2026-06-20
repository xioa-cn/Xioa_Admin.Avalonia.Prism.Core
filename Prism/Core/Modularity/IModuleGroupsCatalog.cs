using System.Collections.Generic;

namespace Prism.Modularity;

public interface IModuleGroupsCatalog
{
    IList<IModuleCatalogItem> Items { get; }
}