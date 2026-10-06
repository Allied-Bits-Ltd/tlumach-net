// <copyright file="CustomerValidator.cs" company="Allied Bits Ltd.">
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

using FluentValidation;

using Tlumach.FluentValidation;

namespace Tlumach.Sample.Validation;

internal sealed class CustomerValidator : AbstractValidator<Customer>
{
    public const decimal MaxDiscount = 1000m;

    public CustomerValidator()
    {
        // The message comes from FluentValidation.NotEmptyValidator in the translation, and the name from DisplayNames.Customer.Name.
        RuleFor(c => c.Name).NotEmpty();

        // No Tlumach text: the message built into FluentValidation is used, in the current language.
        RuleFor(c => c.Email).EmailAddress();

        // The message and the name of the rule come from translation units created by Generator.
        RuleFor(c => c.Nickname).MaximumLength(10).WithMessage(Strings.Messages.NicknameTooLong).WithName(Strings.Names.Nickname);

        // A placeholder of our own, {Limit:N0}, is filled here; FluentValidation fills {PropertyName}.
        RuleFor(c => c.Discount).LessThanOrEqualTo(MaxDiscount)
            .WithMessage(Strings.Messages.DiscountLimit, (customer, discount, formatter) => formatter.AppendArgument("Limit", MaxDiscount))
            .WithName(Strings.Names.Discount);
    }
}
