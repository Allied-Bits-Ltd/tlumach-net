// <copyright file="MarkupPlaceholderValues.cs" company="Allied Bits Ltd.">
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

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Components;

namespace Tlumach.Blazor;

/// <summary>
/// Prepares placeholder values for a translation that is rendered as markup: the translation is trusted HTML, while the values are data.
/// <para>A <see cref="string"/> value is HTML-encoded, a <see cref="MarkupString"/> value is passed as its raw HTML, and any other value is left for the formatter.</para>
/// </summary>
internal static class MarkupPlaceholderValues
{
    /// <summary>
    /// Returns the named values with string values HTML-encoded. The returned dictionary looks keys up through <paramref name="args"/>, so its comparer is kept.
    /// </summary>
    /// <param name="args">The values of the named placeholders, or <see langword="null"/>.</param>
    /// <returns>The values to substitute, or <see langword="null"/> when <paramref name="args"/> is <see langword="null"/>.</returns>
    public static IDictionary<string, object?>? Encode(IDictionary<string, object?>? args)
    {
        if (args is null)
            return null;

        foreach (KeyValuePair<string, object?> pair in args)
        {
            if (NeedsConversion(pair.Value))
                return new EncodingDictionary(args);
        }

        return args;
    }

    /// <summary>
    /// Returns a copy of the indexed values with string values HTML-encoded.
    /// </summary>
    /// <param name="values">The values of the indexed placeholders, or <see langword="null"/>.</param>
    /// <returns>The values to substitute, or <see langword="null"/> when <paramref name="values"/> is <see langword="null"/>.</returns>
#pragma warning disable SA1011 // Closing square bracket must be followed by space - false positive for the nullable array type "object[]?"
    public static object[]? Encode(object[]? values)
#pragma warning restore SA1011
    {
        if (values is null || !Array.Exists(values, NeedsConversion))
            return values;

        object[] result = new object[values.Length];
        for (int i = 0; i < values.Length; i++)
            result[i] = EncodeValue(values[i]);

        return result;
    }

    private static bool NeedsConversion(object? value) => value is string or MarkupString;

    [return: NotNullIfNotNull(nameof(value))]
    private static object? EncodeValue(object? value) => value switch
    {
        string text => HtmlEncoder.Default.Encode(text),
        MarkupString markup => markup.Value,
        _ => value,
    };

    /// <summary>
    /// A read-only view of a dictionary that encodes values when they are read and leaves key lookup to the wrapped dictionary.
    /// </summary>
    private sealed class EncodingDictionary : IDictionary<string, object?>
    {
        private readonly IDictionary<string, object?> _inner;

        public EncodingDictionary(IDictionary<string, object?> inner)
        {
            _inner = inner;
        }

        public ICollection<string> Keys => _inner.Keys;

        public ICollection<object?> Values => [.. _inner.Values.Select(EncodeValue)];

        public int Count => _inner.Count;

        public bool IsReadOnly => true;

        public object? this[string key]
        {
            get => EncodeValue(_inner[key]);
            set => throw new NotSupportedException();
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value)
        {
            if (_inner.TryGetValue(key, out object? raw))
            {
                value = EncodeValue(raw);
                return true;
            }

            value = null;
            return false;
        }

        public bool ContainsKey(string key) => _inner.ContainsKey(key);

        public bool Contains(KeyValuePair<string, object?> item)
            => TryGetValue(item.Key, out object? value) && Equals(value, item.Value);

        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            foreach (KeyValuePair<string, object?> pair in this)
                array[arrayIndex++] = pair;
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            foreach (KeyValuePair<string, object?> pair in _inner)
                yield return new KeyValuePair<string, object?>(pair.Key, EncodeValue(pair.Value));
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(string key, object? value) => throw new NotSupportedException();

        public void Add(KeyValuePair<string, object?> item) => throw new NotSupportedException();

        public bool Remove(string key) => throw new NotSupportedException();

        public bool Remove(KeyValuePair<string, object?> item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();
    }
}
