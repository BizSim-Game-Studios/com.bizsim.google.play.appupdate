using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]
[assembly: Preserve]

// Tests/Runtime/ + Tests/Editor/ need internal access (PackageVersion is internal static class).
[assembly: InternalsVisibleTo("BizSim.Google.Play.AppUpdate.Tests")]
[assembly: InternalsVisibleTo("BizSim.Google.Play.AppUpdate.EditorTests")]

// Editor assembly needs internal access too — the AppUpdateConfiguration window reads
// PackageVersion.Current / .PlayCoreVersion / .ReleaseDate for the header display.
[assembly: InternalsVisibleTo("BizSim.Google.Play.AppUpdate.Editor")]
