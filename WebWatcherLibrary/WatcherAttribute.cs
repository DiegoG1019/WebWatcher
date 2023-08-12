using System;

namespace DiegoG.WebWatcher;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class WatcherAttribute : Attribute
{
    public WatcherAttribute() { }
}
