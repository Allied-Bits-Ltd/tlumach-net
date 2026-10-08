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

namespace Tlumach.MvcTests.Models;

#pragma warning disable CA1515 // The model of a view and of an action is used by the MVC infrastructure, which needs public types.
public sealed class RegisterViewModel
#pragma warning restore CA1515
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    public int Age { get; set; }

    public string? Phone { get; set; }

    [Display(Name = "Mail")]
    public string? Backup { get; set; }
}
