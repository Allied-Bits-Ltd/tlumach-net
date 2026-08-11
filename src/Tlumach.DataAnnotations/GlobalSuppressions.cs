// <copyright file="GlobalSuppressions.cs" company="Allied Bits Ltd.">
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

// CA1019 asks that a property matching a positional argument of an attribute be read-only. The localized validation attributes of Tlumach deliberately offer both forms of the annotation: the positional
// constructor as a shorthand for the common case, and settable named properties, which are the only way to set CultureSource, the only way to combine a translation with the constructor of
// TlumachRangeAttribute that takes an operand type, and the form that matches how ErrorMessageResourceType and ErrorMessageResourceName are written. Making the properties read-only would remove the
// named form; removing the constructors would make every annotation more verbose. ValidationAttribute itself pairs a constructor argument with the settable ErrorMessage property for the same reason.
[assembly: SuppressMessage(
    "Design",
    "CA1019:Define accessors for attribute arguments",
    Scope = "namespaceanddescendants",
    Target = "~N:Tlumach.DataAnnotations",
    Justification = "The attributes intentionally support both the positional constructor and the settable named properties. See the comment above this suppression.")]
