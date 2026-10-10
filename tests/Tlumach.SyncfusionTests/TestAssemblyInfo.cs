// <copyright file="TestAssemblyInfo.cs" company="Allied Bits Ltd.">
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

// Several tests change process-wide state (CultureInfo.CurrentUICulture of the test thread, the parsers registered with Tlumach),
// so the test classes must not run in parallel. Tests that check isolation start their concurrent work inside one test.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
