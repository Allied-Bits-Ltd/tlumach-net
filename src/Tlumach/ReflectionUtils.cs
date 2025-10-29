// <copyright file="ReflectionUtils.cs" company="Allied Bits Ltd.">
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

namespace Tlumach
{
    public static class ReflectionUtils
    {
        public static bool TryGetPropertyValue(object obj, string propertyName, out object? value)
        {
            value = null;
            if (obj == null || string.IsNullOrWhiteSpace(propertyName))
                return false;

            // Get the type
            var type = obj.GetType();

            // Case-insensitive property lookup
            var prop = type.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop == null)
                return false;

            // Get the value
            value = prop.GetValue(obj);
            return true;
        }
    }

}