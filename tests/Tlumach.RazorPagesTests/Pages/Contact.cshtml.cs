// <copyright file="Contact.cshtml.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace Tlumach.RazorPagesTests.Pages;

[IgnoreAntiforgeryToken]
#pragma warning disable CA1515, MA0048 // Razor Pages discovers only public page models (CA1515), and the page model file is named after its page, as the convention requires (MA0048).
public sealed class ContactModel : PageModel
#pragma warning restore CA1515, MA0048
{
    private readonly IStringLocalizer<ContactModel> _localizer;

    public ContactModel(IStringLocalizer<ContactModel> localizer) => _localizer = localizer;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet() => ViewData["Title"] = _localizer["Pages.Contact.Title"].Value;

    public void OnPost() => ViewData["Title"] = _localizer["Pages.Contact.Title"].Value;

#pragma warning disable CA1034, CA1515 // The public nested input model is what the display-name keys of this test are about; model binding and reflection need it public.
    public sealed class InputModel
#pragma warning restore CA1034, CA1515
    {
        [Required]
        public string? Email { get; set; }

        public int Age { get; set; }
    }
}
