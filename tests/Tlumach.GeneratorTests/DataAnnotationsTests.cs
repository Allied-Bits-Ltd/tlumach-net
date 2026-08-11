// <copyright file="DataAnnotationsTests.cs" company="Allied Bits Ltd.">
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

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

using Tlumach.DataAnnotations;

namespace Tlumach.Tests
{
    /// <summary>
    /// Tests of the localized validation attributes of Tlumach.
    /// <para>The attributes are exercised against real classes created by Tlumach Generator: every test generates the classes, compiles them together with a model that carries the annotations, and then
    /// validates that model. Compiling the annotations proves that the intended syntax is the one that a consumer can actually write.</para>
    /// </summary>
    [Trait("Category", "DataAnnotations")]
    [Collection("Generator")]
    public class DataAnnotationsTests : IDisposable
    {
        private const string TestFilesPath = "..\\..\\..\\TestData\\Generator";

        /// <summary>
        /// The models with the annotations under test, compiled together with the generated classes.
        /// </summary>
        private const string ConsumerSource = @"
using System.ComponentModel.DataAnnotations;

using Test.Translations;
using Tlumach.DataAnnotations;

namespace Test.Consumer
{
    public class RegistrationModel
    {
        [TlumachRequired(typeof(Strings), Strings.emailRequiredKey)]
        [TlumachEmailAddress(typeof(Strings), Strings.emailInvalidKey)]
        [Display(ResourceType = typeof(Strings.Texts), Name = Strings.emailLabelKey)]
        public string? Email { get; set; }

        [TlumachStringLength(100, typeof(Strings), Strings.passwordLengthKey, MinimumLength = 6)]
        public string? Password { get; set; }

        [TlumachCompare(nameof(Password), typeof(Strings), Strings.passwordMismatchKey)]
        public string? ConfirmPassword { get; set; }

        [TlumachRange(18, 120, typeof(Strings), Strings.ageRangeKey)]
        public int Age { get; set; }

        [TlumachRegularExpression(""^[0-9]+$"", typeof(Strings), Strings.digitsOnlyKey)]
        public string? Digits { get; set; }

        [TlumachPhone(typeof(Strings), Strings.phoneInvalidKey)]
        public string? Phone { get; set; }

        [TlumachRequired(typeof(Strings.account), Strings.account.emailRequiredKey)]
        public string? GroupEmail { get; set; }

        [TlumachRequired(typeof(OtherStrings), OtherStrings.emailRequiredKey)]
        public string? OtherEmail { get; set; }

        [TlumachRequired(typeof(KeyOnlyStrings), KeyOnlyStrings.emailRequiredKey)]
        public string? KeyOnlyEmail { get; set; }

        [TlumachRequired(typeof(KeyOnlyStrings.account), ""account.emailRequired"")]
        public string? KeyOnlyGroupEmail { get; set; }

        [TlumachRequired(typeof(Strings), Strings.emailRequiredKey, CultureSource = TranslationCultureSource.Ambient)]
        public string? AmbientEmail { get; set; }

        [TlumachRequired(typeof(Strings), Strings.blankKey)]
        public string? BlankMessage { get; set; }

        [TlumachRequired]
        public string? Unconfigured { get; set; }

        [TlumachRequired(TranslationKey = Strings.emailRequiredKey)]
        public string? KeyWithoutClass { get; set; }

        [TlumachRequired(typeof(Strings), Strings.emailRequiredKey, ErrorMessage = ""A literal message"")]
        public string? KeyAndLiteral { get; set; }

        [TlumachRequired(typeof(Strings), ""thereIsNoSuchKey"")]
        public string? MissingKey { get; set; }

        [TlumachRequired(typeof(Test.Broken.NoManager), ""emailRequired"")]
        public string? WrongMemberType { get; set; }

        [TlumachRequired(typeof(Test.Broken.NothingAtAll), ""emailRequired"")]
        public string? NoMemberAndNoManager { get; set; }

        [TlumachRequired(typeof(Test.Broken.Derived), ""Ambiguous"")]
        public string? AmbiguousMember { get; set; }
    }

    public class ShortModel
    {
        [TlumachStringLength(10, typeof(Strings), Strings.passwordLengthKey, MinimumLength = 6)]
        public string? Password { get; set; }

        [TlumachCompare(nameof(Password), typeof(Strings), Strings.passwordMismatchKey)]
        [Display(ResourceType = typeof(Strings.Texts), Name = Strings.emailLabelKey)]
        public string? ConfirmPassword { get; set; }
    }
}

namespace Test.Broken
{
    // A member of the right name but of a type that is not a translation unit.
    public static class NoManager
    {
        public static readonly string emailRequired = ""not a translation unit"";
    }

    // Neither a member of that name nor a translation manager anywhere.
    public static class NothingAtAll
    {
    }

    public class BaseWithMember
    {
        public static string Ambiguous => ""from the base class"";
    }

    // A static member that hides one of a base class. Reflection resolves it to the most derived one rather than reporting an ambiguity.
    public class Derived : BaseWithMember
    {
        public static new string Ambiguous => ""from the derived class"";
    }
}";

        private static readonly object CompilationLock = new();

        private static Assembly? _assembly;

        private static TranslationManager? _manager;

        public DataAnnotationsTests()
        {
            TlumachValidationDefaults.CultureSource = TranslationCultureSource.TranslationManager;
            TlumachValidationDefaults.ResetResolutionCache();
            Manager.CurrentCulture = CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// Gets the assembly with the generated classes and the annotated models. It is built once and shared, which is also what lets the tests observe the cache of the resolver.
        /// </summary>
        private static Assembly Assembly
        {
            get
            {
                lock (CompilationLock)
                {
                    if (_assembly is null)
                    {
                        Tlumach.Generator.IniParser.Use();
                        Tlumach.Generator.TomlParser.Use();
                        Tlumach.Base.IniParser.Use();
                        Tlumach.Base.TomlParser.Use();

                        List<string> sources =
                        [
                            Generate("Annotations.cfg", accessors: true),
                            Generate("AnnotationsOther.cfg", accessors: false),
                            Generate("AnnotationsKeysOnly.cfg", accessors: false),
                            ConsumerSource,
                        ];

                        _assembly = StringAccessorTests.Compile(sources);
                    }

                    return _assembly;
                }
            }
        }

        private static TranslationManager Manager
        {
            get
            {
                if (_manager is null)
                {
                    _manager = ManagerOf("Test.Translations.Strings");
                    ManagerOf("Test.Translations.OtherStrings");
                    ManagerOf("Test.Translations.KeyOnlyStrings");
                }

                return _manager;
            }
        }

        [Fact]
        public void ShouldTakeTheMessageFromTheTranslation()
        {
            Assert.Equal("The E-mail field is required.", Format<TlumachRequiredAttribute>("Email", "E-mail"));
        }

        [Fact]
        public void ShouldTellClassesWithTheSameKeyApart()
        {
            // Both keys are named emailRequired; only the class in the annotation decides which text is used.
            Assert.Equal("The Address field is required.", Format<TlumachRequiredAttribute>("Email", "Address"));
            Assert.Equal("Another Address rule.", Format<TlumachRequiredAttribute>("OtherEmail", "Address"));
        }

        [Fact]
        public void ShouldResolveAKeyOfANestedGroupThroughItsMember()
        {
            Assert.Equal("An account e-mail address is required for sign-up.", Format<TlumachRequiredAttribute>("GroupEmail", "sign-up"));
        }

        [Fact]
        public void ShouldResolveThroughTheManagerWhenThereIsNoUnit()
        {
            // KeyOnlyStrings is generated with onlyDeclareKeys, so it has key constants but no translation units and the resolver has to go through its translation manager.
            Assert.Equal("The E-mail field is required.", Format<TlumachRequiredAttribute>("KeyOnlyEmail", "E-mail"));
        }

        [Fact]
        public void ShouldResolveAGroupQualifiedKeyThroughTheManager()
        {
            Assert.Equal("An account e-mail address is required for sign-up.", Format<TlumachRequiredAttribute>("KeyOnlyGroupEmail", "sign-up"));
        }

        [Fact]
        public void ShouldPassTheArgumentsInTheOrderOfTheStockAttribute()
        {
            Assert.Equal("The Password must be at least 6 and at max 100 characters long.", Format<TlumachStringLengthAttribute>("Password"));
            Assert.Equal("The Age must be between 18 and 120.", Format<TlumachRangeAttribute>("Age"));
            Assert.Equal("The Digits must match ^[0-9]+$.", Format<TlumachRegularExpressionAttribute>("Digits"));
            Assert.Equal("ConfirmPassword and Password do not match.", Format<TlumachCompareAttribute>("ConfirmPassword", "ConfirmPassword"));
            Assert.Equal("The Email is not a valid e-mail address.", Format<TlumachEmailAddressAttribute>("Email"));
            Assert.Equal("The Phone is not a valid telephone number.", Format<TlumachPhoneAttribute>("Phone"));
        }

        [Fact]
        public void ShouldFollowTheCultureOfTheManager()
        {
            TlumachRequiredAttribute attribute = Attribute<TlumachRequiredAttribute>("Email");

            Assert.Equal("The E-mail field is required.", attribute.FormatErrorMessage("E-mail"));

            Manager.CurrentCulture = new CultureInfo("de");

            // The very same attribute instance is reused: a binding refers to the unit and never holds text, so the change of the culture is visible at once.
            Assert.Equal("Das Feld E-mail ist erforderlich.", attribute.FormatErrorMessage("E-mail"));
        }

        [Fact]
        public void ShouldFollowTheAmbientCultureWhenTheAttributeAsksForIt()
        {
            TlumachRequiredAttribute attribute = Attribute<TlumachRequiredAttribute>("AmbientEmail");

            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de");
                Assert.Equal("Das Feld E-mail ist erforderlich.", attribute.FormatErrorMessage("E-mail"));

                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                Assert.Equal("The E-mail field is required.", attribute.FormatErrorMessage("E-mail"));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }

            // The culture of the manager was never touched.
            Assert.Equal(CultureInfo.InvariantCulture.Name, Manager.CurrentCulture.Name);
        }

        [Fact]
        public void ShouldFollowTheProcessDefaultCultureSource()
        {
            TlumachRequiredAttribute attribute = Attribute<TlumachRequiredAttribute>("Email");

            TlumachValidationDefaults.CultureSource = TranslationCultureSource.Ambient;

            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de");

                // The attribute leaves CultureSource at Default, so the process-wide default decides and the culture of the manager, still invariant, is not used.
                Assert.Equal("Das Feld E-mail ist erforderlich.", attribute.FormatErrorMessage("E-mail"));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void ShouldRejectAnUndefinedProcessDefault()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TlumachValidationDefaults.CultureSource = TranslationCultureSource.Default);
            Assert.Throws<ArgumentOutOfRangeException>(() => TlumachValidationDefaults.CultureSource = (TranslationCultureSource)42);
        }

        [Fact]
        public void ShouldCacheTheResolutionAcrossCalls()
        {
            TlumachRequiredAttribute attribute = Attribute<TlumachRequiredAttribute>("Email");

            Assert.False(TlumachValidationDefaults.IsResolutionCached(TranslationClassOf(attribute), "emailRequired"));

            for (int i = 0; i < 5; i++)
                Assert.Equal("The E-mail field is required.", attribute.FormatErrorMessage("E-mail"));

            Assert.True(TlumachValidationDefaults.IsResolutionCached(TranslationClassOf(attribute), "emailRequired"));

            // A reset must not change the outcome; the annotation is simply resolved again.
            TlumachValidationDefaults.ResetResolutionCache();
            Assert.Equal("The E-mail field is required.", attribute.FormatErrorMessage("E-mail"));
        }

        [Fact]
        public void ShouldFallBackToTheStockMessageWithoutConfiguration()
        {
            string message = Format<TlumachRequiredAttribute>("Unconfigured", "Unconfigured");

            Assert.Equal(new RequiredAttribute().FormatErrorMessage("Unconfigured"), message);
        }

        [Fact]
        public void ShouldUseTheStockMessageOfTheSealedAttributesWithoutConfiguration()
        {
            // The default message of these two lives in the sealed attribute of the framework, which Tlumach cannot derive from, so the message is borrowed from an instance of it.
            Assert.Equal(new EmailAddressAttribute().FormatErrorMessage("Email"), new TlumachEmailAddressAttribute().FormatErrorMessage("Email"));
            Assert.Equal(new PhoneAttribute().FormatErrorMessage("Phone"), new TlumachPhoneAttribute().FormatErrorMessage("Phone"));
        }

        [Fact]
        public void ShouldValidateExactlyLikeTheSealedAttributes()
        {
            TlumachEmailAddressAttribute email = new();
            Assert.Equal(new EmailAddressAttribute().IsValid("nobody@example.com"), email.IsValid("nobody@example.com"));
            Assert.Equal(new EmailAddressAttribute().IsValid("not an address"), email.IsValid("not an address"));

            TlumachPhoneAttribute phone = new();
            Assert.Equal(new PhoneAttribute().IsValid("+1 555 0100"), phone.IsValid("+1 555 0100"));
            Assert.Equal(new PhoneAttribute().IsValid("not a number"), phone.IsValid("not a number"));
        }

        [Fact]
        public void ShouldRejectAKeyWithoutAClass()
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Format<TlumachRequiredAttribute>("KeyWithoutClass"));

            Assert.Contains("must be set", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldRejectAKeyCombinedWithALiteralMessage()
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Format<TlumachRequiredAttribute>("KeyAndLiteral"));

            Assert.Contains("cannot be combined", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldRejectAMissingKey()
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Format<TlumachRequiredAttribute>("MissingKey"));

            Assert.Contains("was not found", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldRejectAMemberThatIsNotATranslationUnit()
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Format<TlumachRequiredAttribute>("WrongMemberType"));

            Assert.Contains("not a translation unit", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldRejectAClassWithNeitherAUnitNorAManager()
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Format<TlumachRequiredAttribute>("NoMemberAndNoManager"));

            Assert.Contains("no public static member", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldResolveAHidingMemberToTheMostDerivedOne()
        {
            // A static member that hides one of a base class is not ambiguous to reflection: the most derived one wins. Since it is not a translation unit, the wrong type is what gets reported.
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Format<TlumachRequiredAttribute>("AmbiguousMember"));

            Assert.Contains("not a translation unit", ex.Message, StringComparison.Ordinal);
            Assert.Contains("Test.Broken.Derived", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldRejectAKeyWithoutText()
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Format<TlumachRequiredAttribute>("BlankMessage"));

            Assert.Contains("provides no text", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldValidateAModelEndToEnd()
        {
            Type modelType = Assembly.GetType("Test.Consumer.ShortModel")!;
            object model = Activator.CreateInstance(modelType)!;

            modelType.GetProperty("Password")!.SetValue(model, "short");
            modelType.GetProperty("ConfirmPassword")!.SetValue(model, "another");

            List<ValidationResult> results = [];
            bool valid = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

            Assert.False(valid);

            // Both messages come from the translation, and the display name of the second property comes from the string accessors through the stock DisplayAttribute.
            Assert.Contains("The Password must be at least 6 and at max 10 characters long.", results.Select(r => r.ErrorMessage));
            Assert.Contains("E-mail and Password do not match.", results.Select(r => r.ErrorMessage));
        }

        [Fact]
        public void ShouldValidateAModelEndToEndInAnotherCulture()
        {
            Manager.CurrentCulture = new CultureInfo("de");

            Type modelType = Assembly.GetType("Test.Consumer.ShortModel")!;
            object model = Activator.CreateInstance(modelType)!;

            modelType.GetProperty("Password")!.SetValue(model, "short");

            List<ValidationResult> results = [];
            Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true));

            Assert.Contains("Password muss mindestens 6 und höchstens 10 Zeichen lang sein.", results.Select(r => r.ErrorMessage));
        }

        public void Dispose()
        {
            TlumachValidationDefaults.CultureSource = TranslationCultureSource.TranslationManager;
            GC.SuppressFinalize(this);
        }

        private static string Generate(string configFile, bool accessors)
            => StringAccessorTests.Generate(configFile, accessors);

        private static TranslationManager ManagerOf(string typeName)
        {
            Type type = Assembly.GetType(typeName)!;
            Assert.NotNull(type);

            TranslationManager manager = (TranslationManager)type.GetProperty("TranslationManager", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;

            return manager;
        }

        private static T Attribute<T>(string propertyName)
            where T : Attribute
        {
            Type modelType = Assembly.GetType("Test.Consumer.RegistrationModel")!;
            PropertyInfo property = modelType.GetProperty(propertyName)!;
            Assert.NotNull(property);

            T? attribute = property.GetCustomAttribute<T>();
            Assert.NotNull(attribute);

            return attribute;
        }

        private static string Format<T>(string propertyName, string? displayName = null)
            where T : ValidationAttribute
            => Attribute<T>(propertyName).FormatErrorMessage(displayName ?? propertyName);

        private static Type TranslationClassOf(ITlumachLocalizedErrorMessage attribute) => attribute.TranslationClass!;
    }
}
