// <copyright file="RegisterViewModel.cs" company="Allied Bits Ltd.">
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

using System.ComponentModel.DataAnnotations;

namespace Tlumach.Sample.Mvc.Models;

#pragma warning disable CA1515 // The Razor view and the model binder access the model type; keep it public like the controllers.
public class RegisterViewModel
#pragma warning restore CA1515
{
    [Required(ErrorMessage = "Validation.Required")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Validation.Required")]
    [EmailAddress]
    public string? Email { get; set; }

    [Range(1, 150)]
    public int Age { get; set; }
}
