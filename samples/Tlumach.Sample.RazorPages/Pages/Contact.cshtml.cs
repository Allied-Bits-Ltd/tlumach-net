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

namespace Tlumach.Sample.RazorPages.Pages;

#pragma warning disable CA1515, MA0048 // Razor Pages discovers only public page models; the page model file is named after the page (Contact.cshtml.cs) by convention.
public sealed class ContactModel : PageModel
#pragma warning restore CA1515, MA0048
{
    private readonly IStringLocalizer<ContactModel> _localizer;

    public ContactModel(IStringLocalizer<ContactModel> localizer) => _localizer = localizer;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? Message { get; private set; }

    public void OnGet()
    {
        // Nothing to prepare: the page only shows an empty form.
    }

    public void OnPost()
    {
        if (ModelState.IsValid)
            Message = _localizer["Pages.Contact.Sent", Input.Name ?? string.Empty].Value;
    }

#pragma warning disable CA1515, CA1034 // The model binder and the display name provider access the input model type; the display names of Tlumach are keyed by the nested type chain.
    public sealed class InputModel
#pragma warning restore CA1515, CA1034
    {
        [Required(ErrorMessage = "Validation.Required")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Validation.Required")]
        [EmailAddress(ErrorMessage = "Validation.Email")]
        public string? Email { get; set; }

        [Range(1, 150, ErrorMessage = "Validation.Range")]
        public int Age { get; set; }
    }
}
