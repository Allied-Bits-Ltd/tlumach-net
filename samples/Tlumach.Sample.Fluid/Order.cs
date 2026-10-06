// <copyright file="Order.cs" company="Allied Bits Ltd.">
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
/// The order that has shipped.
/// </summary>
internal sealed class Order
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Order"/> class.
    /// </summary>
    /// <param name="id">The number of the order.</param>
    /// <param name="count">The number of items.</param>
    /// <param name="trackingUrl">The URL of the tracking page.</param>
    /// <param name="carrier">The name of the carrier.</param>
    public Order(string id, int count, Uri trackingUrl, string carrier)
    {
        Id = id;
        Count = count;
        TrackingUrl = trackingUrl;
        Carrier = carrier;
    }

    /// <summary>
    /// Gets the number of the order.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the number of items.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Gets the URL of the tracking page.
    /// </summary>
    public Uri TrackingUrl { get; }

    /// <summary>
    /// Gets the name of the carrier.
    /// </summary>
    public string Carrier { get; }
}
