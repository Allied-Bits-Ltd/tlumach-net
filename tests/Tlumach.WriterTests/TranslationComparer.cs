// <copyright file="TranslationComparer.cs" company="Allied Bits Ltd.">
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

using System;
using System.Collections.Generic;
using System.Linq;

using Tlumach.Base;

namespace Tlumach.WriterTests
{
    internal static class TranslationComparer
    {
        /// <summary>
        /// Compares two collections of translation entries for equality, including all metadata fields.
        /// </summary>
        /// <param name="expected">Expected translation entries.</param>
        /// <param name="actual">Actual translation entries to compare.</param>
        /// <param name="message">Optional message to include in assertion failures.</param>
        public static void AssertTranslationsEqual(
            IEnumerable<TranslationEntry> expected,
            IEnumerable<TranslationEntry> actual,
            string message = "")
        {
            var expectedList = expected.ToList();
            var actualList = actual.ToList();

            Assert.Equal(expectedList.Count, actualList.Count);

            for (int i = 0; i < expectedList.Count; i++)
            {
                var exp = expectedList[i];
                var act = actualList[i];

                Assert.Equal(exp.Key, act.Key);
                Assert.Equal(exp.Text, act.Text);

                // For optional metadata, only compare if both are non-empty.
                // Writers may not preserve all metadata fields.
                if (!string.IsNullOrEmpty(exp.EscapedText) && !string.IsNullOrEmpty(act.EscapedText))
                {
                    Assert.Equal(exp.EscapedText, act.EscapedText);
                }

                if (!string.IsNullOrEmpty(exp.Reference) && !string.IsNullOrEmpty(act.Reference))
                {
                    Assert.Equal(exp.Reference, act.Reference);
                }

                if (!string.IsNullOrEmpty(exp.Context) && !string.IsNullOrEmpty(act.Context))
                {
                    Assert.Equal(exp.Context, act.Context);
                }

                if (!string.IsNullOrEmpty(exp.Description) && !string.IsNullOrEmpty(act.Description))
                {
                    Assert.Equal(exp.Description, act.Description);
                }

                if (!string.IsNullOrEmpty(exp.Comment) && !string.IsNullOrEmpty(act.Comment))
                {
                    Assert.Equal(exp.Comment, act.Comment);
                }

                if (!string.IsNullOrEmpty(exp.Target) && !string.IsNullOrEmpty(act.Target))
                {
                    Assert.Equal(exp.Target, act.Target);
                }

                if (!string.IsNullOrEmpty(exp.Type) && !string.IsNullOrEmpty(act.Type))
                {
                    Assert.Equal(exp.Type, act.Type);
                }

                if (!string.IsNullOrEmpty(exp.SourceText) && !string.IsNullOrEmpty(act.SourceText))
                {
                    Assert.Equal(exp.SourceText, act.SourceText);
                }

                if (!string.IsNullOrEmpty(exp.Screen) && !string.IsNullOrEmpty(act.Screen))
                {
                    Assert.Equal(exp.Screen, act.Screen);
                }

                if (!string.IsNullOrEmpty(exp.Video) && !string.IsNullOrEmpty(act.Video))
                {
                    Assert.Equal(exp.Video, act.Video);
                }

                Assert.Equal(exp.ContainsPlaceholders, act.ContainsPlaceholders);
            }
        }
    }
}
