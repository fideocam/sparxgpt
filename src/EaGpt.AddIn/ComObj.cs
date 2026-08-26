using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace EaGpt.AddIn
{
    /// <summary>
    /// Late-bound wrapper so the add-in compiles without Interop.EA.dll.
    /// EA passes live COM objects at runtime.
    /// </summary>
    internal sealed class ComObj
    {
        private const BindingFlags Invoke =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.InvokeMethod |
            BindingFlags.GetProperty | BindingFlags.SetProperty;

        public object Target { get; }

        public ComObj(object target)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public object? Get(string name)
        {
            return Target.GetType().InvokeMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.GetProperty, null, Target, null);
        }

        public void Set(string name, object? value)
        {
            Target.GetType().InvokeMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.SetProperty, null, Target, new[] { value });
        }

        public object? Call(string name, params object?[] args)
        {
            return Target.GetType().InvokeMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.InvokeMethod, null, Target, args);
        }

        public string Str(string name)
        {
            object? v = Get(name);
            return v == null ? "" : Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";
        }

        public int Int(string name)
        {
            object? v = Get(name);
            if (v == null)
            {
                return 0;
            }

            return Convert.ToInt32(v, CultureInfo.InvariantCulture);
        }

        public bool Bool(string name)
        {
            object? v = Get(name);
            return v != null && Convert.ToBoolean(v, CultureInfo.InvariantCulture);
        }

        public ComObj? Child(string name)
        {
            object? v = Get(name);
            return v == null ? null : new ComObj(v);
        }

        public ComObj? CallObj(string name, params object?[] args)
        {
            object? v = Call(name, args);
            return v == null ? null : new ComObj(v);
        }

        public IEnumerable<ComObj> Enumerate(string collectionProperty)
        {
            ComObj? coll = Child(collectionProperty);
            if (coll == null)
            {
                yield break;
            }

            int count = coll.Int("Count");
            for (int i = 0; i < count; i++)
            {
                object? item = null;
                try
                {
                    item = coll.Call("GetAt", (short)i);
                }
                catch
                {
                    try
                    {
                        item = coll.Call("GetAt", i);
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (item != null)
                {
                    yield return new ComObj(item);
                }
            }
        }

        public bool TryDeleteAt(string collectionProperty, int index)
        {
            ComObj? coll = Child(collectionProperty);
            if (coll == null)
            {
                return false;
            }

            try
            {
                coll.Call("Delete", (short)index);
                coll.Call("Refresh");
                return true;
            }
            catch
            {
                try
                {
                    coll.Call("DeleteAt", (short)index, true);
                    coll.Call("Refresh");
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
