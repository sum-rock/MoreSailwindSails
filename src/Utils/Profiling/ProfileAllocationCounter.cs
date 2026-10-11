using System;
using System.Reflection;

namespace MoreSailwindSails.Utils.Profiling
{
    // Binds the optional runtime counter once; heap size is not an allocation counter.
    internal static class ProfileAllocationCounter
    {
        internal static Func<long> TryCreate(Type runtime)
        {
            try
            {
                var method = runtime.GetMethod(
                    name: "GetAllocatedBytesForCurrentThread",
                    bindingAttr: BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null
                );
                if (method == null || method.ReturnType != typeof(long))
                    return null;
                var counter =
                    (Func<long>)Delegate.CreateDelegate(type: typeof(Func<long>), method: method);
                if (counter() < 0)
                    return null;
                return counter;
            }
            catch (NotSupportedException)
            {
                return null;
            }
            catch (NotImplementedException)
            {
                return null;
            }
            catch (MemberAccessException)
            {
                return null;
            }
        }
    }
}
