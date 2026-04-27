// <copyright file="TlumachExtension.cs" company="Allied Bits Ltd.">
//
// Copyright 2026 Allied Bits Ltd.
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;

namespace AlliedBits.Tlumach.Extension.VisualStudio;

/// <summary>
/// VisualStudio.Extensibility entry point for the Tlumach.NET Generator extension.
/// Commands decorated with <see cref="VisualStudioContributionAttribute"/> are
/// discovered and registered automatically by the build tooling — no explicit
/// registration is required here.
/// </summary>
[VisualStudioContribution]
public sealed class TlumachExtension : Microsoft.VisualStudio.Extensibility.Extension
{
    /// <inheritdoc />
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        // Run in-process so that VSSDK/DTE services (used by GeneratorRunner and
        // TranslationNavigator) remain available alongside the new-model commands.
        RequiresInProcessHosting = true,
    };

    /// <inheritdoc />
    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        base.InitializeServices(serviceCollection);
    }
}
