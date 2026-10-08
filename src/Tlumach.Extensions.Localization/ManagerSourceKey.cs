// <copyright file="ManagerSourceKey.cs" company="Allied Bits Ltd.">
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

using System.Runtime.CompilerServices;

using Tlumach.Base;

namespace Tlumach.Extensions.Localization;

/// <summary>
/// The key of a manager entry created from options: the values that <see cref="TranslationManagerResolver.CreateFromOptions"/> uses, and the text processing mode of the entry.
/// Options that are built anew for every call (by a custom <see cref="ITlumachSettingsProvider"/>) share one entry as long as they describe the same manager.
/// <para>The source is the <see cref="TranslationManager"/>, the <see cref="TranslationConfiguration"/>, or the assembly of the default file (<see cref="TranslationManagerResolver.GetDefaultFileAssembly"/>), compared by reference.</para>
/// <para>Used by <see cref="TlumachStringLocalizerFactory"/> and, compiled from this file as its own internal copy, by the HTML localizer factory of Tlumach.AspNetCore.Mvc.</para>
/// </summary>
internal readonly struct ManagerSourceKey : IEquatable<ManagerSourceKey>
{
    // The source of a default file when there is no assembly to load it from (no Assembly in the options and no entry assembly).
    private static readonly object NoAssembly = new();

    private readonly object _source;
    private readonly string? _defaultFile;
    private readonly string? _defaultFileLocale;
    private readonly TextFormat? _textProcessingMode;

    private ManagerSourceKey(object source, string? defaultFile, string? defaultFileLocale, TextFormat? textProcessingMode)
    {
        _source = source;
        _defaultFile = defaultFile;
        _defaultFileLocale = defaultFileLocale;
        _textProcessingMode = textProcessingMode;
    }

    public static bool operator ==(ManagerSourceKey left, ManagerSourceKey right) => left.Equals(right);

    public static bool operator !=(ManagerSourceKey left, ManagerSourceKey right) => !left.Equals(right);

    // The same precedence as CreateFromOptions: the manager, then the configuration, then the default file.
    internal static ManagerSourceKey From(TlumachLocalizationOptions options)
    {
        if (options.TranslationManager is not null)
            return new ManagerSourceKey(options.TranslationManager, defaultFile: null, defaultFileLocale: null, options.TextProcessingMode);

        if (options.Configuration is not null)
            return new ManagerSourceKey(options.Configuration, defaultFile: null, defaultFileLocale: null, options.TextProcessingMode);

        return new ManagerSourceKey((object?)TranslationManagerResolver.GetDefaultFileAssembly(options) ?? NoAssembly, options.DefaultFile, options.DefaultFileLocale, options.TextProcessingMode);
    }

    public bool Equals(ManagerSourceKey other)
        => ReferenceEquals(_source, other._source)
            && string.Equals(_defaultFile, other._defaultFile, StringComparison.Ordinal)
            && string.Equals(_defaultFileLocale, other._defaultFileLocale, StringComparison.Ordinal)
            && _textProcessingMode == other._textProcessingMode;

    public override bool Equals(object? obj) => obj is ManagerSourceKey other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(
            RuntimeHelpers.GetHashCode(_source),
            _defaultFile is null ? 0 : StringComparer.Ordinal.GetHashCode(_defaultFile),
            _defaultFileLocale is null ? 0 : StringComparer.Ordinal.GetHashCode(_defaultFileLocale),
            _textProcessingMode);
}
