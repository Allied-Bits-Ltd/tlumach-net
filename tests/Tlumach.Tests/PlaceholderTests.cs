// <copyright file="PlaceholderParserTests.cs" company="Allied Bits Ltd.">
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
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tlumach.Base;

namespace Tlumach.Tests
{
    public class PlaceholderTests
    {
        //private const string TestFilesPath = "..\\..\\..\\TestData\\Placeholders";

        static PlaceholderTests()
        {
            ArbParser.Use();
            JsonParser.Use();
        }

        public PlaceholderTests()
        {
            ArbParser.TextProcessingMode = TextFormat.Arb;
            BaseParser.RecognizeFileRefs = true;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleSimplePlaceholderArb(int mode)
        {
            var parser = new ArbParser();

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Hello\" : \"Hello {name}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Hello"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { name = "world", }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { "world" }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?>(StringComparer.Ordinal) { { "name", "world" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "name", "world" } }),
                _ => string.Empty
            };

            Assert.Equal("Hello world", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldIgnoreUnknownPlaceholderArb(int mode)
        {
            var parser = new ArbParser();

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\r\n    \"Hello\": \"Hello {name}\",\r\n    \"@Hello\" : {\r\n        \"placeholders\": {\r\n            \"userName\": {\r\n              \"type\": \"String\",\r\n              \"example\": \"Bob\"\r\n            }\r\n        }\r\n    }\r\n", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Hello"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { name = "world", }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { "world" }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "name", "world" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "name", "world" } }),
                _ => string.Empty
            };

            Assert.Equal("Hello {name}", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldIgnoreSimplePlaceholderEscaped(int mode)
        {
            var parser = new ArbParser();

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Hello\" : \"Hello {name}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Hello"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.BackslashEscaping, new { name = "world", }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.BackslashEscaping, new object[] { "world" }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.BackslashEscaping, new Dictionary<string, object?> { { "name", "world" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.BackslashEscaping, new OrderedDictionary { { "name", "world" } }),
                _ => string.Empty
            };

            Assert.Equal("Hello {name}", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleSimplePlaceholderNet(int mode)
        {
            var parser = new ArbParser();

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Hello\" : \"Hello {name}\"}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Hello"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.DotNet, new { name = "world", }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.DotNet, new Dictionary<string, object?> { { "name", "world" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.DotNet, new OrderedDictionary { { "name", "world" } }),
                _ => string.Empty
            };

            Assert.Equal("Hello world", final);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void ShouldHandleSimplePlaceholderNetOrdinal(int mode)
        {
            var parser = new ArbParser();

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Hello\" : \"Hello {0}\"}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Hello"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.DotNet, new object[] { "world" }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.DotNet, new OrderedDictionary { { "name", "world" } }),
                _ => string.Empty
            };

            Assert.Equal("Hello world", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleNumericPlaceholderArbWithRounding(int mode)
        {
            var parser = new ArbParser();

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"{count, number, integer} files processed.\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { count = 1234.56, }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { 1234.56 }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "count", 1234.56 } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "count", 1234.56 } }),
                _ => string.Empty
            };

            Assert.Equal("1,235 files processed.", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleNumericPlaceholderArbAsPercent(int mode)
        {
            var parser = new ArbParser();

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"Progress: {progress, number, percent}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { progress = 0.375, }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { 0.375 }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "progress", 0.375 } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "progress", 0.375 } }),
                _ => string.Empty
            };

            Assert.Equal("Progress: 37.50 %", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleNumericPlaceholderArbAsCurrency(int mode)
        {
            var parser = new ArbParser();

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"Total: {total, number, currency}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { total = 1234.5, }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { 1234.5 }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "total", 1234.5 } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "total", 1234.5 } }),
                _ => string.Empty
            };

            Assert.Equal($"Total: {CultureInfo.InvariantCulture.NumberFormat.CurrencySymbol}1,234.50", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandlePluralPlaceholderArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"{total}: {count, plural, =0{no items} =1{# item} other{# items}}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { count = 2, total = "Total", }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { "Total", 2 }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "count", 2 }, { "total", "Total" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "total", "Total" }, { "count", 2 } }),
                _ => string.Empty
            };

            Assert.Equal("Total: 2 items", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandlePluralWithEmbeddedPlaceholderArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"{count, plural, one{{name} has 1 message} other{{name} has # messages}}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { count = 5, name = "Alex", }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { 5, "Alex" }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "count", 5 }, { "name", "Alex" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "count", 5 }, { "name", "Alex" } }),
                _ => string.Empty
            };

            Assert.Equal("Alex has 5 messages", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleSelectGenderNumberPlaceholdersArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"{gender, select, male{He} female{She} other{They}} uploaded {count, number, integer} file(s).\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { gender = "female", count = 1 }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { "female", 1 }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "count", 1 }, { "gender", "female" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "gender", "female" }, { "count", 1 } }),
                _ => string.Empty
            };

            Assert.Equal("She uploaded 1 file(s).", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleSelectNestedPluralPlaceholdersArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"{gender, select, male{He} female{She} other{They}} {count, plural, one{uploaded 1 file} other{uploaded # files}}.\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { gender = "other", count = 3 }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { "other", 3 }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "count", 3 }, { "gender", "other" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "gender", "other" }, { "count", 3 } }),
                _ => string.Empty
            };

            Assert.Equal("They uploaded 3 files.", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandlePluralWithOffsetEmbeddedPlaceholderArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"{count, plural, offset:1 =0{Nobody tagged you} =1{{first} tagged you} one{{first} and one other tagged you} other{{first} and # others tagged you}}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { count = 4, first = "Mia", }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { 4, "Mia" }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "count", 4 }, { "first", "Mia" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "count", 4 }, { "first", "Mia" } }),
                _ => string.Empty
            };

            Assert.Equal("Mia and 3 others tagged you", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleSelectOrdinalPlaceholderArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"{place, selectordinal, one{#st} two{#nd} few{#rd} other{#th}} place\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { place = 23, }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { 23, "Mia" }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "place", 23 }, { "first", "Mia" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "place", 23 }, { "first", "Mia" } }),
                _ => string.Empty
            };

            Assert.Equal("23rd place", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleSelectWithEmbeddedNumberFormattingPlaceholderArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"{tier, select, free{Free plan} pro{Pro plan — {storage, number, integer} GB} other{Custom}}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { tier = "pro", storage = 512.9 }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { "pro", 512.9 }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "storage", 512.9 }, { "tier", "pro" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "tier", "pro" }, { "storage", 512.9 } }),
                _ => string.Empty
            };

            Assert.Equal("Pro plan — 513 GB", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandlePluralWithEmbeddedCurrencyPlaceholderArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"{count, plural, one{Subscription costs {price, number, currency} per user} other{Subscriptions cost {price, number, currency} per {count} users}}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { count = 5, price = 12, }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { 5, 12, 5 }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "count", 5 }, { "price", 12 } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "count", 5 }, { "price", 12 } }),
                _ => string.Empty
            };

            Assert.Equal($"Subscriptions cost {CultureInfo.InvariantCulture.NumberFormat.CurrencySymbol}12.00 per 5 users", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleTwoPluralsPlaceholderArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"{apples, plural, one{# apple} other{# apples}} and {oranges, plural, one{# orange} other{# oranges}} in the basket.\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { apples = 1, oranges = 2, }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { 1, 2 }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "apples", 1 }, { "oranges", 2 } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "apples", 1 }, { "oranges", 2 } }),
                _ => string.Empty
            };

            Assert.Equal($"1 apple and 2 oranges in the basket.", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandlePluralAndPercentPlaceholderArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"{count, plural, one{Completion: {pct, number, percent} on 1 task} other{Completion: {pct, number, percent} on # tasks}}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { count = 8, pct = 0.8125, }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { 8, 0.8125 }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "count", 8 }, { "pct", 0.8125 } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "count", 8 }, { "pct", 0.8125 } }),
                _ => string.Empty
            };

            Assert.Equal($"Completion: {0.8125.ToString("P", CultureInfo.InvariantCulture)} on 8 tasks", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleAgreementViaPluralAndEmbeddedPlaceholderArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"{count, plural, one{{who} is online} other{{who} are online}}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { count = 3, who = "Alice, Bob, and Kim", }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { 3, "Alice, Bob, and Kim" }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "count", 3 }, { "who", "Alice, Bob, and Kim" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "count", 3 }, { "who", "Alice, Bob, and Kim" } }),
                _ => string.Empty
            };

            Assert.Equal("Alice, Bob, and Kim are online", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleNestedSelectInsidePluralPlaceholderArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"{count, plural, one{{gender, select, male{He has} female{She has} other{They have}} 1 message} other{{gender, select, male{He has} female{She has} other{They have}} # messages}}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { count = 2, gender = "male", }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { 2, "male", 2, "male" }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "count", 2 }, { "gender", "male" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "count", 2 }, { "gender", "male" } }),
                _ => string.Empty
            };

            Assert.Equal("He has 2 messages", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleMultilinePlusSelectDotNet(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.DotNet;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"Report:\\n- Name: {name}\\n- Files: {count, number}\\n- Done: {done, select, true{yes} other{no}}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { name = "Build #17", count = 1200, done = "true", }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { "Build #17", 1200, "true" }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "name", "Build #17" }, { "count", 1200 }, { "done", "true" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "name", "Build #17" }, { "count", 1200 }, { "done", "true" } }),
                _ => string.Empty
            };

            Assert.Equal("Report:\n- Name: Build #17\n- Files: 1,200\n- Done: yes", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleEscapesApostrophesPlaceholderArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"Don''t remove {count, plural, one{this item} other{these items}}.\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { count = 4 }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { 4 }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "count", 4 } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "count", 4 } }),
                _ => string.Empty
            };

            Assert.Equal("Don't remove these items.", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleLiteralBracesViaQuotingPlaceholderArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"Braces: '{'example'}' → {value}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { value = 42 }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { 42 }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "value", 42 } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "value", 42 } }),
                _ => string.Empty
            };

            Assert.Equal("Braces: {example} → 42", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleDeepEmbeddingArb(int mode)
        {
            var parser = new ArbParser();
            ArbParser.TextProcessingMode = TextFormat.Arb;

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"{mode, select, summary{{items, plural, =0{No results} one{# result (took {ms, number, integer} ms)} other{# results (took {ms, number, integer} ms)}}} detailed{{items, plural, one{Found 1 result: {first}} other{Found # results. First: {first}}}} other{Unknown mode}}\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { mode = "summary", items = 3, ms = 278.4, first = "N/A", }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { "summary", 3, 278.4, "N/A" }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "mode", "summary" }, {"items", 3 }, {"ms", 278.4 }, { "first" , "N/A" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "mode", "summary" }, { "items", 3 }, { "ms", 278.4 }, { "first", "N/A" } }),
                _ => string.Empty
            };

            Assert.Equal("3 results (took 278 ms)", final);
        }
/*
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandleDateArb(int mode)
        {
            var parser = new ArbParser();

            Translation? translation = parser.LoadTranslation(string.Concat("{", "\"Result\" : \"Today is {today, date}.\"", "}"), CultureInfo.InvariantCulture);
            Assert.NotNull(translation);
            TranslationEntry? entry = translation["Result"];
            Assert.NotNull(entry);
            Assert.True(entry.IsTemplated);
            string final;
            CultureInfo culture = new CultureInfo("en-US");
            final = mode switch
            {
                0 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new { today = "2025-11-28T00:00:00Z", }),
                1 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new object[] { "2025-11-28T00:00:00Z" }),
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "today", "2025-11-28T00:00:00Z" } }),
                3 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new OrderedDictionary { { "today", "2025-11-28T00:00:00Z" } }),
                _ => string.Empty
            };

            Assert.Equal("Today is Nov 28, 2025.", final);
        }*/
    }
}
