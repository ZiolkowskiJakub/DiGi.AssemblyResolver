using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace DiGi.AssemblyResolver.Classes
{
    /// <summary>
    /// Provides functionality to resolve managed and native assembly dependencies at runtime 
    /// by hooking into the .NET AssemblyLoadContext resolving events.
    /// </summary>
    public sealed class AssemblyResolver
    {
        private readonly object gate = new();

        private readonly List<string> managedDirectories = [];
        private readonly List<string> nativeDirectories = [];

        private readonly Dictionary<string, AssemblyName> redirects = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> resolving = new(StringComparer.OrdinalIgnoreCase);

        private bool enabled;

        /// <summary>
        /// Adds a directory to the list of paths searched for managed assemblies.
        /// </summary>
        /// <param name="directory">The path to the directory containing managed assemblies.</param>
        public void AddManagedDirectory(string? directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            lock (gate) AddManagedDirectory_NoLock(directory);
        }

        /// <summary>
        /// Adds a directory to the list of paths searched for native assemblies.
        /// </summary>
        /// <param name="directory">The path to the directory containing native assemblies.</param>
        public void AddNativeDirectory(string? directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            lock (gate) AddNativeDirectory_NoLock(directory);
        }

        /// <summary>
        /// Adds a redirect that maps a requested assembly name to a specific full assembly name.
        /// </summary>
        /// <param name="name">The simple name of the assembly to be redirected.</param>
        /// <param name="fullAssemblyName">The full assembly name string used for the redirection target.</param>
        public void AddRedirect(string? name, string? fullAssemblyName)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(fullAssemblyName))
            {
                return;
            }

            lock (gate) redirects[name] = new AssemblyName(fullAssemblyName);
        }

        /// <summary>
        /// Disables the assembly resolver and unhooks from the runtime resolving events.
        /// </summary>
        public void Disable()
        {
            lock (gate)
            {
                if (!enabled)
                {
                    return;
                }

                AssemblyLoadContext.Default.Resolving -= OnResolvingManaged;
                AssemblyLoadContext.Default.ResolvingUnmanagedDll -= OnResolvingNative;

                enabled = false;
            }
        }

        /// <summary>
        /// Enables the assembly resolver and hooks into the runtime resolving events.
        /// </summary>
        /// <param name="managedDirectories">An optional collection of directories to search for managed assemblies.</param>
        /// <param name="nativeDirectories">An optional collection of directories to search for native assemblies.</param>
        public void Enable(IEnumerable<string>? managedDirectories = null, IEnumerable<string>? nativeDirectories = null)
        {
            lock (gate)
            {
                if (enabled)
                {
                    return;
                }

                string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

                AddManagedDirectory_NoLock(directory);

                if (managedDirectories != null)
                {
                    foreach (string managedDirectory in managedDirectories)
                    {
                        AddManagedDirectory_NoLock(managedDirectory);
                    }
                }

                if (nativeDirectories != null)
                {
                    foreach (string nativeDirectory in nativeDirectories)
                    {
                        AddNativeDirectory_NoLock(nativeDirectory);
                    }
                }

                AssemblyLoadContext.Default.Resolving += OnResolvingManaged;
                AssemblyLoadContext.Default.ResolvingUnmanagedDll += OnResolvingNative;

                enabled = true;
            }
        }

        private void AddManagedDirectory_NoLock(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            string directory_FullPath = Path.GetFullPath(directory);

            if (!Directory.Exists(directory_FullPath))
            {
                return;
            }

            if (!managedDirectories.Contains(directory_FullPath, StringComparer.OrdinalIgnoreCase))
            {
                managedDirectories.Add(directory_FullPath);
            }

            // Common NuGet-style native layout next to managed bits:
            string directory_Native = Path.Combine(directory_FullPath, "runtimes", "win-x64", "native");
            if (Directory.Exists(directory_Native) && !nativeDirectories.Contains(directory_Native, StringComparer.OrdinalIgnoreCase))
            {
                nativeDirectories.Add(directory_Native);
            }
        }

        private void AddNativeDirectory_NoLock(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            string directory_FullPath = Path.GetFullPath(directory);

            if (!Directory.Exists(directory_FullPath))
            {
                return;
            }

            if (!nativeDirectories.Contains(directory_FullPath, StringComparer.OrdinalIgnoreCase))
            {
                nativeDirectories.Add(directory_FullPath);
            }
        }

        private bool BeginResolve(string name)
        {
            lock (gate)
            {
                if (resolving.Contains(name))
                {
                    return false; // recursion guard
                }

                resolving.Add(name);

                return true;
            }
        }

        private void EndResolve(string name)
        {
            lock (gate) resolving.Remove(name);
        }

        private Assembly? OnResolvingManaged(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName)
        {
            string name = assemblyName.Name ?? string.Empty;

            if (!BeginResolve(name))
            {
                return null;
            }

            try
            {
                // Honor redirect if specified
                if (redirects.TryGetValue(name, out var pinned))
                {
                    assemblyName = pinned;
                }

                // If already loaded (unification), return it
                Assembly? result = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
                if (result != null)
                {
                    return result;
                }

                // Satellite (resources) assembly?
                if (name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                {
                    string? satellite = TryFindSatellite(assemblyName);
                    if (satellite != null)
                    {
                        return assemblyLoadContext.LoadFromAssemblyPath(satellite);
                    }
                }

                // Managed probe
                string? path = TryFindManaged(assemblyName);

                return path != null ? assemblyLoadContext.LoadFromAssemblyPath(path) : null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[AssemblyResolver] Failed to resolve '{assemblyName}': {ex}");
                return null;
            }
            finally
            {
                EndResolve(name);
            }
        }

        private IntPtr OnResolvingNative(Assembly requestingAssembly, string unmanagedDllName)
        {
            try
            {
                // Only Windows for Revit: ensure .dll suffix
                string fileName = Path.HasExtension(unmanagedDllName) ? unmanagedDllName : unmanagedDllName + ".dll";
                foreach (string nativeDirectory in nativeDirectories)
                {
                    string path = Path.Combine(nativeDirectory, fileName);
                    if (File.Exists(path) && NativeLibrary.TryLoad(path, out nint handle))
                    {
                        return handle;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[AssemblyResolver] Failed to resolve native '{unmanagedDllName}': {ex}");
            }

            return IntPtr.Zero;
        }

        private string? TryFindManaged(AssemblyName assemblyName)
        {
            string name = assemblyName.Name ?? string.Empty;

            // Exact file name first
            foreach (string managedDirectory in managedDirectories)
            {
                string path = Path.Combine(managedDirectory, name + ".dll");
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    AssemblyName assemblyName_Temp = AssemblyName.GetAssemblyName(path);
                    if (!string.Equals(assemblyName_Temp.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Prefer exact version match if requested; otherwise accept
                    if (assemblyName.Version == null || assemblyName_Temp.Version == assemblyName.Version)
                    {
                        return path;
                    }
                }
                catch
                {
                    /* skip unreadable */
                }
            }

            // No exact version — accept first simple-name match we can read
            foreach (string managedDirectory in managedDirectories)
            {
                string path = Path.Combine(managedDirectory, name + ".dll");
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    AssemblyName assemblyName_Temp = AssemblyName.GetAssemblyName(path);
                    if (string.Equals(assemblyName_Temp.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return path;
                    }
                }
                catch
                {
                }
            }
            return null;
        }

        private string? TryFindSatellite(AssemblyName assemblyName)
        {
            string name = assemblyName.Name ?? string.Empty;

            if (!name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string? cultureInfoName = assemblyName.CultureInfo?.Name;
            if (string.IsNullOrEmpty(cultureInfoName))
            {
                return null;
            }

            string baseName = name[..^".resources".Length];

            foreach (string managedDirectory in managedDirectories)
            {
                string result = Path.Combine(managedDirectory, cultureInfoName, baseName + ".resources.dll");
                if (File.Exists(result))
                {
                    return result;
                }
            }

            return null;
        }
    }
}