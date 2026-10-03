// <copyright file="ManagerSubscriptions.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.WinFormsTests
{
    /// <summary>
    /// Counts the handlers subscribed to <see cref="TranslationManager.OnCultureChanged"/>, which lets the tests detect leaked subscriptions
    /// (the disposed guards in the components hide a leak from tests that only check the visible behavior).
    /// </summary>
    internal static class ManagerSubscriptions
    {
        public static int CultureChangedHandlerCount(TranslationManager manager)
        {
            // A field-like event has a backing field with the name of the event.
            FieldInfo? field = typeof(TranslationManager).GetField("OnCultureChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);

            Delegate? handlers = field.GetValue(manager) as Delegate;
            return handlers?.GetInvocationList().Length ?? 0;
        }
    }
}
