// <copyright file="Program.cs" company="Allied Bits Ltd.">
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

using Tlumach.WinForms;

namespace Tlumach.Sample.WinForms
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Every TranslationProvider that has no TranslationManager of its own uses this one.
            // It must be set before the first form is created, because the provider applies the translations at the end of InitializeComponent.
            TranslationProvider.DefaultTranslationManager = Strings.TranslationManager;

            using var mainForm = new MainForm();
            Application.Run(mainForm);
        }
    }
}
