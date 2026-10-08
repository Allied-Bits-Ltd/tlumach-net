// <copyright file="LocationAssemblies.cs" company="Allied Bits Ltd.">
//
// Copyright 2025 Allied Bits Ltd.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>

using System.Collections.Concurrent;
using System.Reflection;

namespace Tlumach.Extensions.Localization;

/// <summary>
/// Finds the assembly that the <c>location</c> argument of <c>IStringLocalizerFactory.Create(baseName, location)</c> and <c>IHtmlLocalizerFactory.Create(baseName, location)</c> names.
/// As for <c>ResourceManagerStringLocalizerFactory</c> of Microsoft, the location is the name (simple or full) of the assembly that holds the resources; MVC's <c>ViewLocalizer</c>, for example, passes the name of the application.
/// <para>A location that is empty or is not the name of a loadable assembly yields <see langword="null"/>, and the factory then uses the assembly that called it. Released versions did not use the location
/// to find the assembly, only as a part of the options context, so an application may pass a location that is not an assembly name; such a location keeps working as before.</para>
/// <para>The result is cached by the location, also when it is <see langword="null"/>, so a location that is not an assembly is not probed again on every call.</para>
/// <para>Used by <see cref="TlumachStringLocalizerFactory"/> and, compiled from this file as its own internal copy, by the HTML localizer factory of Tlumach.AspNetCore.Mvc.</para>
/// </summary>
internal sealed class LocationAssemblies
{
    private readonly ConcurrentDictionary<string, Assembly?> _cache = new(StringComparer.Ordinal);

    internal Assembly? Find(string? location)
    {
        if (string.IsNullOrEmpty(location))
            return null;

        return _cache.GetOrAdd(location, static name => TryLoad(name));
    }

    private static Assembly? TryLoad(string location)
    {
        try
        {
            return Assembly.Load(new AssemblyName(location));
        }
        catch (Exception ex) when (ex is IOException or BadImageFormatException or ArgumentException)
        {
            // FileNotFoundException and FileLoadException (also thrown by AssemblyName for an invalid name) derive from IOException.
            return null;
        }
    }
}
