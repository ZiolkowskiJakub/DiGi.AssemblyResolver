## DiGi\.AssemblyResolver\.Classes Namespace
### Classes

<a name='DiGi.AssemblyResolver.Classes.AssemblyResolver'></a>

## AssemblyResolver Class

Provides functionality to resolve managed and native assembly dependencies at runtime 
by hooking into the \.NET AssemblyLoadContext resolving events\.

```csharp
public sealed class AssemblyResolver
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → AssemblyResolver
### Methods

<a name='DiGi.AssemblyResolver.Classes.AssemblyResolver.AddManagedDirectory(string)'></a>

## AssemblyResolver\.AddManagedDirectory\(string\) Method

Adds a directory to the list of paths searched for managed assemblies\.

```csharp
public void AddManagedDirectory(string? directory);
```
#### Parameters

<a name='DiGi.AssemblyResolver.Classes.AssemblyResolver.AddManagedDirectory(string).directory'></a>

`directory` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The path to the directory containing managed assemblies\.

<a name='DiGi.AssemblyResolver.Classes.AssemblyResolver.AddNativeDirectory(string)'></a>

## AssemblyResolver\.AddNativeDirectory\(string\) Method

Adds a directory to the list of paths searched for native assemblies\.

```csharp
public void AddNativeDirectory(string? directory);
```
#### Parameters

<a name='DiGi.AssemblyResolver.Classes.AssemblyResolver.AddNativeDirectory(string).directory'></a>

`directory` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The path to the directory containing native assemblies\.

<a name='DiGi.AssemblyResolver.Classes.AssemblyResolver.AddRedirect(string,string)'></a>

## AssemblyResolver\.AddRedirect\(string, string\) Method

Adds a redirect that maps a requested assembly name to a specific full assembly name\.

```csharp
public void AddRedirect(string? name, string? fullAssemblyName);
```
#### Parameters

<a name='DiGi.AssemblyResolver.Classes.AssemblyResolver.AddRedirect(string,string).name'></a>

`name` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The simple name of the assembly to be redirected\.

<a name='DiGi.AssemblyResolver.Classes.AssemblyResolver.AddRedirect(string,string).fullAssemblyName'></a>

`fullAssemblyName` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The full assembly name string used for the redirection target\.

<a name='DiGi.AssemblyResolver.Classes.AssemblyResolver.Disable()'></a>

## AssemblyResolver\.Disable\(\) Method

Disables the assembly resolver and unhooks from the runtime resolving events\.

```csharp
public void Disable();
```

<a name='DiGi.AssemblyResolver.Classes.AssemblyResolver.Enable(System.Collections.Generic.IEnumerable_string_,System.Collections.Generic.IEnumerable_string_)'></a>

## AssemblyResolver\.Enable\(IEnumerable\<string\>, IEnumerable\<string\>\) Method

Enables the assembly resolver and hooks into the runtime resolving events\.

```csharp
public void Enable(System.Collections.Generic.IEnumerable<string>? managedDirectories=null, System.Collections.Generic.IEnumerable<string>? nativeDirectories=null);
```
#### Parameters

<a name='DiGi.AssemblyResolver.Classes.AssemblyResolver.Enable(System.Collections.Generic.IEnumerable_string_,System.Collections.Generic.IEnumerable_string_).managedDirectories'></a>

`managedDirectories` [System\.Collections\.Generic\.IEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')

An optional collection of directories to search for managed assemblies\.

<a name='DiGi.AssemblyResolver.Classes.AssemblyResolver.Enable(System.Collections.Generic.IEnumerable_string_,System.Collections.Generic.IEnumerable_string_).nativeDirectories'></a>

`nativeDirectories` [System\.Collections\.Generic\.IEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')

An optional collection of directories to search for native assemblies\.