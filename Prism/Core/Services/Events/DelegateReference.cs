using System;
using System.Reflection;

namespace Prism.Events;

internal sealed class DelegateReference
{
    private readonly Delegate? _strongReference;
    private readonly WeakReference? _weakTargetReference;
    private readonly MethodInfo? _method;
    private readonly Type? _delegateType;

    public DelegateReference(Delegate target, bool keepReferenceAlive)
    {
        if (keepReferenceAlive || target.Target is null)
        {
            _strongReference = target;
            return;
        }

        _weakTargetReference = new WeakReference(target.Target);
        _method = target.Method;
        _delegateType = target.GetType();
    }

    public Delegate? Target
    {
        get
        {
            if (_strongReference is not null)
            {
                return _strongReference;
            }

            var target = _weakTargetReference?.Target;
            if (target is null || _method is null || _delegateType is null)
            {
                return null;
            }

            return Delegate.CreateDelegate(_delegateType, target, _method, throwOnBindFailure: false);
        }
    }
}