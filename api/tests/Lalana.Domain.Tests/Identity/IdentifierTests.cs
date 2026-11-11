using Lalana.Domain.Identity;
using Lalana.Domain.Profiles;
using Shouldly;

namespace Lalana.Domain.Tests.Identity;

public sealed class PhoneNumberTests
{
    [Theory]
    [InlineData("034 12 345 67")]
    [InlineData("0341234567")]
    [InlineData("34 12 345 67")]
    [InlineData("+261 34 12 345 67")]
    [InlineData("+261341234567")]
    [InlineData("00261 34 12 345 67")]
    [InlineData("261341234567")]
    [InlineData("034.12.345.67")]
    [InlineData("(034) 12-345-67")]
    public void Normalizes_local_and_international_forms(string text)
    {
        PhoneNumber.TryParse(text, out var phone).ShouldBeTrue();
        phone.E164.ShouldBe("+261341234567");
        phone.Local.ShouldBe("034 12 345 67");
        phone.Masked.ShouldBe("+261 34 ** *** 67");
        phone.ToString().ShouldBe("+261341234567");
    }

    [Theory]
    [InlineData("0321234567")]
    [InlineData("0331234567")]
    [InlineData("0371234567")]
    [InlineData("0381234567")]
    public void Accepts_every_mobile_operator_prefix(string text) => PhoneNumber.TryParse(text, out _).ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("034 12 34")]
    [InlineData("0351234567")]
    [InlineData("0201234567")]
    [InlineData("+33612345678")]
    [InlineData("03412345678")]
    [InlineData("03412a4567")]
    public void Refuses_what_is_not_a_malagasy_mobile(string? text)
    {
        PhoneNumber.TryParse(text, out _).ShouldBeFalse();
        if (text is not null)
        {
            Should.Throw<FormatException>(() => PhoneNumber.Parse(text));
        }
    }
}

public sealed class EmailAddressTests
{
    [Fact]
    public void Trims_and_lower_cases()
    {
        var email = EmailAddress.Parse("  Sitraka.Rabe@Exemple.MG ");

        email.Value.ShouldBe("sitraka.rabe@exemple.mg");
        email.Masked.ShouldBe("s***@exemple.mg");
        email.ToString().ShouldBe("sitraka.rabe@exemple.mg");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("pas-un-email")]
    [InlineData("@exemple.mg")]
    [InlineData("a@@exemple.mg")]
    [InlineData("a@b@exemple.mg")]
    [InlineData("a@exemple")]
    [InlineData("a@exemple.")]
    [InlineData("a@.exemple.mg")]
    [InlineData("a@exemple..mg")]
    [InlineData("a b@exemple.mg")]
    public void Refuses_malformed_addresses(string? text)
    {
        EmailAddress.TryParse(text, out _).ShouldBeFalse();
        if (text is not null)
        {
            Should.Throw<FormatException>(() => EmailAddress.Parse(text));
        }
    }

    [Fact]
    public void Refuses_addresses_longer_than_254_characters() =>
        EmailAddress.TryParse(new string('a', 250) + "@b.mg", out _).ShouldBeFalse();
}

public sealed class LoginIdentifierTests
{
    [Fact]
    public void Reads_a_value_with_an_at_sign_as_an_email()
    {
        var identifier = LoginIdentifier.Parse("Vous@Exemple.com").Identifier!;

        identifier.Kind.ShouldBe(IdentifierKind.Email);
        identifier.Value.ShouldBe("vous@exemple.com");
        identifier.Masked.ShouldBe("v***@exemple.com");
        identifier.ToString().ShouldBe("v***@exemple.com");
    }

    [Fact]
    public void Reads_anything_else_as_a_phone()
    {
        var identifier = LoginIdentifier.Parse("034 12 345 67").Identifier!;

        identifier.Kind.ShouldBe(IdentifierKind.Phone);
        identifier.Value.ShouldBe("+261341234567");
        identifier.Masked.ShouldBe("+261 34 ** *** 67");
    }

    [Theory]
    [InlineData(null, IdentifierError.Missing)]
    [InlineData(" ", IdentifierError.Missing)]
    [InlineData("034 12 34", IdentifierError.InvalidPhone)]
    [InlineData("vous@exemple", IdentifierError.InvalidEmail)]
    public void Explains_why_a_value_is_not_an_identifier(string? text, IdentifierError error)
    {
        var result = LoginIdentifier.Parse(text);

        result.Identifier.ShouldBeNull();
        result.Error.ShouldBe(error);
    }
}

public sealed class PasswordAndNameTests
{
    [Theory]
    [InlineData("sitraka2026", true)]
    [InlineData("12345678", true)]
    [InlineData("sitraka", false)]
    [InlineData("rabesitraka", false)]
    [InlineData("abc1", false)]
    [InlineData(null, false)]
    public void Needs_eight_characters_and_a_digit(string? password, bool acceptable) =>
        PasswordRules.IsAcceptable(password).ShouldBe(acceptable);

    [Fact]
    public void Refuses_passwords_longer_than_128_characters() =>
        PasswordRules.IsAcceptable(new string('a', 128) + "1").ShouldBeFalse();

    [Theory]
    [InlineData("  Hery   Nirina ", true, "Hery Nirina")]
    [InlineData("", false, "")]
    [InlineData(null, false, "")]
    public void Normalizes_names(string? text, bool valid, string expected)
    {
        PersonName.TryNormalize(text, out var name).ShouldBe(valid);
        name.ShouldBe(expected);
    }

    [Fact]
    public void A_profile_needs_both_names()
    {
        var id = UserId.From(Guid.NewGuid());

        Profile.Create(id, " Hery ", "Rakoto", DateTimeOffset.UnixEpoch).FirstName.ShouldBe("Hery");
        Should.Throw<ArgumentException>(() => Profile.Create(id, "Hery", " ", DateTimeOffset.UnixEpoch));
        Should.Throw<ArgumentException>(() => Profile.Create(id, "", "Rakoto", DateTimeOffset.UnixEpoch));
    }
}
