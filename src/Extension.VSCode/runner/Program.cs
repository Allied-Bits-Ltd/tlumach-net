// <copyright file="Program.cs" company="Allied Bits Ltd.">
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

using System.Text;

using AlliedBits.Tlumach.Extension.Runner;

// The report is UTF-8 whatever the console code page is: the extension decodes it as UTF-8
Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

using CancellationTokenSource cancellation = new();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

return RunnerCommand.Run(args, Console.Out, Console.Error, new ProjectEvaluator(), cancellation.Token);
