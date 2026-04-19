// <copyright file="HierarchicalKeyComparer.cs" company="Allied Bits Ltd.">
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

#if GENERATOR
namespace Tlumach.Generator;
#else
namespace Tlumach.Base;
#endif

public class HierarchicalKeyComparer : IComparer<string>
{
    public static int CompareKeys(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        if (x is null)
            return -1;

        if (y is null)
            return 1;

        var xSegments = x.Split('.');
        var ySegments = y.Split('.');

        int minSegments = Math.Min(xSegments.Length, ySegments.Length);
        for (int i = 0; i < minSegments; i++)
        {
            int segmentComparison = string.CompareOrdinal(xSegments[i], ySegments[i]);
            if (segmentComparison != 0)
                return segmentComparison;
        }

        return xSegments.Length.CompareTo(ySegments.Length);
    }

    public int Compare(string? x, string? y)
    {
        return CompareKeys(x, y);
    }
}
