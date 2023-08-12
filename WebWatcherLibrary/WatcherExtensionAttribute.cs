using System;

namespace DiegoG.WebWatcher;

/// <summary>
/// Decorates an assembly with metadata related to its WebWatcher host
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class WatcherExtensionAttribute : Attribute
{
    /// <summary>
    /// The minimum required version's major component of the WebWatcher host that is required for this assembly to run
    /// </summary>
    /// <remarks>
    /// If less than 0, it's ignored
    /// </remarks>
    public int MinimumMajorVersion { get; init; } = -1;

    /// <summary>
    /// The minimum required version's minor component of the WebWatcher host that is required for this assembly to run
    /// </summary>
    /// <remarks>
    /// If less than 0, it's ignored. If <see cref="MinimumMajorVersion"/> is ignored (i.e. less than 0) it is ignored.
    /// </remarks>
    public int MinimumMinorVersion { get; init; } = -1;

    /// <summary>
    /// The target version's major component of the WebWatcher host that this assembly was compiled for
    /// </summary>
    /// <remarks>
    /// If less than 0, it's ignored
    /// </remarks>
    public int TargetMajorVersion { get; init; } = -1;

    /// <summary>
    /// The target version's minor component of the WebWatcher host that this assembly was compiled for
    /// </summary>
    /// <remarks>
    /// If less than 0, it's ignored. If <see cref="TargetMajorVersion"/> is ignored (i.e. less than 0) it is ignored.
    /// </remarks>
    public int TargetMinorVersion { get; init; } = -1;
}
