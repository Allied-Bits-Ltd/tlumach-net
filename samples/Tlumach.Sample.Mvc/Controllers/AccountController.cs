// <copyright file="AccountController.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Mvc;

using Tlumach.Sample.Mvc.Models;

namespace Tlumach.Sample.Mvc.Controllers;

#pragma warning disable CA1515 // MVC discovers only public controllers.
public class AccountController : Controller
#pragma warning restore CA1515
{
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // The sample does not persist anything: a valid form just returns to an empty one.
        return RedirectToAction(nameof(Register));
    }
}
