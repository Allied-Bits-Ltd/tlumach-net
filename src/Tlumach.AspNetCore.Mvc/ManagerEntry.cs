// <copyright file="ManagerEntry.cs" company="Allied Bits Ltd.">
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

using Tlumach.Base;
using Tlumach.Extensions.Localization;
using Tlumach.Templating;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// A translation manager with the translator that renders its HTML and the string localizer that serves <c>GetString</c>. Immutable and shared by all localizers of the manager.
/// </summary>
internal sealed class ManagerEntry
{
    internal ManagerEntry(TranslationManager manager, TextFormat? textProcessingMode)
    {
        Manager = manager;
        Translator = new TemplateTranslator(manager);
        Strings = new TlumachStringLocalizer(manager, textProcessingMode);
    }

    internal TranslationManager Manager { get; }

    internal TemplateTranslator Translator { get; }

    internal TlumachStringLocalizer Strings { get; }
}
