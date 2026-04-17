using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Grasshopper.Kernel;

namespace GrasshopperComponents;

/// <summary>
/// Resolves sibling assemblies when the plugin is loaded by Rhino or Rhino.Inside hosts.
/// </summary>
public sealed class GhCrowdFlowLoader : GH_AssemblyPriority
{
    public override GH_LoadingInstruction PriorityLoad()
    {
        string? pluginDir = Path.GetDirectoryName(typeof(GhCrowdFlowLoader).Assembly.Location);
        if (string.IsNullOrWhiteSpace(pluginDir))
        {
            return GH_LoadingInstruction.Proceed;
        }

        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            string? name = new AssemblyName(args.Name).Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            string candidatePath = Path.Combine(pluginDir, $"{name}.dll");
            return File.Exists(candidatePath) ? Assembly.LoadFrom(candidatePath) : null;
        };

#if NET5_0_OR_GREATER
        try
        {
            RegisterAlcResolver(pluginDir);
        }
        catch
        {
            // AppDomain fallback above is sufficient if ALC registration is unavailable.
        }
#endif

        return GH_LoadingInstruction.Proceed;
    }

#if NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RegisterAlcResolver(string pluginDir)
    {
        var alc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(GhCrowdFlowLoader).Assembly);
        if (alc == null)
        {
            return;
        }

        alc.Resolving += (context, name) =>
        {
            if (string.IsNullOrWhiteSpace(name.Name))
            {
                return null;
            }

            string candidatePath = Path.Combine(pluginDir, $"{name.Name}.dll");
            return File.Exists(candidatePath) ? context.LoadFromAssemblyPath(candidatePath) : null;
        };
    }
#endif
}
