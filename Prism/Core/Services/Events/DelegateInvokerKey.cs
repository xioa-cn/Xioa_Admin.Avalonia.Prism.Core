using System.Reflection;

namespace Prism.Events;

internal readonly record struct DelegateInvokerKey(Type DelegateType, MethodInfo Method);
