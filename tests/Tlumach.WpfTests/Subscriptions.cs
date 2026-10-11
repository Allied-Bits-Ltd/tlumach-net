// <copyright file="Subscriptions.cs" company="Allied Bits Ltd.">
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

using System;
using System.Reflection;

namespace Tlumach.WpfTests
{
    /// <summary>
    /// Counts the handlers subscribed to the events of a translation manager and of a translation unit, which lets the tests detect leaked subscriptions.
    /// </summary>
    internal static class Subscriptions
    {
        public static int CultureChanged(TranslationManager manager) => Count(typeof(TranslationManager), manager, nameof(TranslationManager.OnCultureChanged));

        public static int UnitChanged(TranslationUnit unit) => Count(typeof(TranslationUnit), unit, nameof(TranslationUnit.OnChange));

        private static int Count(Type type, object instance, string eventName)
        {
            // A field-like event has a backing field with the name of the event.
            FieldInfo? field = type.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);

            Delegate? handlers = field.GetValue(instance) as Delegate;
            return handlers?.GetInvocationList().Length ?? 0;
        }
    }
}
