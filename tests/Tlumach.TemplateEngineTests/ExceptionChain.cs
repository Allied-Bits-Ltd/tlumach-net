// <copyright file="ExceptionChain.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.TemplateEngineTests;

/// <summary>
/// Finds an exception in the chain of inner exceptions, because template engines may wrap the exceptions thrown by functions, filters and helpers.
/// </summary>
internal static class ExceptionChain
{
    public static TException Find<TException>(Exception exception)
        where TException : Exception
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is TException found)
                return found;
        }

        throw new Xunit.Sdk.XunitException($"No {typeof(TException).Name} in the chain of {exception}");
    }
}
