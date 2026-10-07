// <copyright file="TlumachDisplayNameKeys.cs" company="Allied Bits Ltd.">
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

using System.Text;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Forms the keys of display names. Keys use dots only: nested types are separated by dots, and the arity of generic types ("`1") and type arguments are dropped.
/// </summary>
public static class TlumachDisplayNameKeys
{
    /// <summary>
    /// Returns the key of a container type, e.g. "Pages.Movies.CreateModel.InputModel" for <c>MyApp.Pages.Movies.CreateModel+InputModel</c> with the root namespace "MyApp".
    /// </summary>
    /// <param name="type">The container type of a property.</param>
    /// <param name="style">How the key is formed.</param>
    /// <param name="rootNamespace">The root namespace removed by <see cref="TlumachDisplayNameKeyStyle.RelativeTypeName"/>; only removed when the namespace equals it or starts with it followed by a dot.</param>
    /// <returns>The key.</returns>
    public static string GetContainerKey(Type type, TlumachDisplayNameKeyStyle style, string? rootNamespace)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.IsGenericType && !type.IsGenericTypeDefinition)
            type = type.GetGenericTypeDefinition();

        string chain = TypeChain(type);
        string? ns = type.Namespace;
        switch (style)
        {
            case TlumachDisplayNameKeyStyle.TypeName:
                return chain;
            case TlumachDisplayNameKeyStyle.FullTypeName:
                return string.IsNullOrEmpty(ns) ? chain : ns + "." + chain;
            default:
                if (string.IsNullOrEmpty(ns))
                    return chain;

                if (!string.IsNullOrEmpty(rootNamespace))
                {
                    if (ns.Equals(rootNamespace, StringComparison.Ordinal))
                        return chain;

                    if (ns.Length > rootNamespace.Length && ns[rootNamespace.Length] == '.' && ns.StartsWith(rootNamespace, StringComparison.Ordinal))
                        return string.Concat(ns.AsSpan(rootNamespace.Length + 1), ".", chain);
                }

                return ns + "." + chain;
        }
    }

    private static string TypeChain(Type type)
    {
        StringBuilder builder = new(StripArity(type.Name));
        for (Type? declaring = type.DeclaringType; declaring is not null; declaring = declaring.DeclaringType)
            builder.Insert(0, '.').Insert(0, StripArity(declaring.Name));

        return builder.ToString();
    }

    private static string StripArity(string name)
    {
        int tick = name.IndexOf('`', StringComparison.Ordinal);
        return tick < 0 ? name : name[..tick];
    }
}
