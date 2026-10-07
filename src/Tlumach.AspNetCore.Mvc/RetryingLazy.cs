// <copyright file="RetryingLazy.cs" company="Allied Bits Ltd.">
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

using System.Threading;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Creates a value on first use and keeps it, like <see cref="Lazy{T}"/>, but a failed creation is not remembered: the exception reaches the caller and the next call tries again.
/// The creation runs under a lock, so at most one successful creation happens (a translation manager created twice would be registered twice).
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
internal sealed class RetryingLazy<T>
    where T : class
{
    private readonly Lock _lock = new();
    private readonly Func<T> _factory;
    private volatile T? _value;

    public RetryingLazy(Func<T> factory)
    {
        _factory = factory;
    }

    public T Value
    {
        get
        {
            T? value = _value;
            if (value is not null)
                return value;

            lock (_lock)
            {
                value = _value;
                if (value is null)
                {
                    value = _factory();
                    _value = value;
                }

                return value;
            }
        }
    }
}
