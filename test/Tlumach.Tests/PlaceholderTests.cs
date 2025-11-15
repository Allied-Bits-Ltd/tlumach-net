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
        const string TestFilesPath = "..\\..\\..\\TestData\\Placeholders";

        static PlaceholderTests()
        {
            ArbParser.Use();
            JsonParser.Use();
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
                2 => entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Arb, new Dictionary<string, object?> { { "name", "world" } }),
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

            Assert.Equal("Total: ¤1,234.50", final);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldHandlePluralPlaceholderArb(int mode)
        {
            var parser = new ArbParser();

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
    }
}
