namespace Prism.Navigation.Regions;

public interface IRegionBehavior
{
    IRegion Region { get; set; }

    void Attach();
}
