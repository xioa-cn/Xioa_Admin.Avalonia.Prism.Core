using System;

namespace Prism;

public interface IActiveAware
{
    bool IsActive { get; set; }

    event EventHandler? IsActiveChanged;
}