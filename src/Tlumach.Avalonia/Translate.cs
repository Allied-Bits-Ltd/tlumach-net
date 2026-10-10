// <copyright file="Translate.cs" company="Allied Bits Ltd.">
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

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Avalonia;
using Avalonia.Markup.Xaml;

namespace Tlumach.Avalonia
{
    public sealed class Translate : MarkupExtension
    {
        // AvaloniaObjectExtensions.ToBinding returns IBinding in Avalonia 11 and BindingBase in Avalonia 12. A call compiled against one version fails
        // with MissingMethodException on the other, so the method is found at run time, which lets one build of this assembly work with both versions.
        private static readonly MethodInfo _toBinding = FindToBinding();

        private readonly Tlumach.Avalonia.TranslationUnit _translationUnit;

        public Translate(Tlumach.Avalonia.TranslationUnit translationUnit)
        {
            _translationUnit = translationUnit ?? throw new ArgumentNullException(nameof(translationUnit));
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _toBinding.Invoke(null, [_translationUnit.Value])!;
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(AvaloniaObjectExtensions))]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The method is instantiated over string, a reference type, whose shared code is always available.")]
        private static MethodInfo FindToBinding()
        {
            MethodInfo? method = Array.Find(
                typeof(AvaloniaObjectExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static),
                m => string.Equals(m.Name, "ToBinding", StringComparison.Ordinal) && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);

            if (method is null)
                throw new MissingMethodException(typeof(AvaloniaObjectExtensions).FullName, "ToBinding");

            return method.MakeGenericMethod(typeof(string));
        }
    }
}
