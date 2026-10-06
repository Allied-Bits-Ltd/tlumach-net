// <copyright file="Customer.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Sample.Emails;

/// <summary>
/// A customer who gets an email in their language.
/// </summary>
internal sealed class Customer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Customer"/> class.
    /// </summary>
    /// <param name="name">The name of the customer.</param>
    /// <param name="culture">The name of the culture of the customer.</param>
    public Customer(string name, string culture)
    {
        Name = name;
        Culture = culture;
    }

    /// <summary>
    /// Gets the name of the customer.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the name of the culture of the customer.
    /// </summary>
    public string Culture { get; }
}
