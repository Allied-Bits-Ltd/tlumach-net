// <copyright file="Person.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Sample.Syncfusion;

/// <summary>
/// A row of the sample data.
/// </summary>
public sealed record Person(string Name, string Country, DateTime Born)
{
    private static readonly string[] Names = ["Anna", "Bohdan", "Clara", "Dmytro", "Emil", "Freya", "Gustav", "Halyna", "Ida", "Jonas"];

    private static readonly string[] Countries = ["Austria", "Germany", "Ukraine", "Switzerland", "Poland"];

    /// <summary>
    /// Gets 32 people, enough for several pages.
    /// </summary>
    public static IReadOnlyList<Person> All { get; } = Enumerable.Range(0, 32)
        .Select(i => new Person($"{Names[i % Names.Length]} {i + 1}", Countries[i % Countries.Length], new DateTime(1960 + i, (i % 12) + 1, (i % 27) + 1, 0, 0, 0, DateTimeKind.Unspecified)))
        .ToArray();
}
