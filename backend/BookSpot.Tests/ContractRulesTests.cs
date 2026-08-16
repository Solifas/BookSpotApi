using BookSpot.Application.Features.Auth;
using BookSpot.Application.Features.Bookings;

namespace BookSpot.Tests;

public class ContractRulesTests
{
    [Theory]
    [InlineData("  User@Example.COM  ", "user@example.com")]
    [InlineData("TÉST@example.com", "tést@example.com")]
    public void NormalizeEmail_UsesTrimNfcAndInvariantLowercase(string input, string expected)
    {
        Assert.Equal(expected, AuthRules.NormalizeEmail(input));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("passwordpassword")]
    [InlineData("123456789012345")]
    public void PasswordPolicy_RejectsShortAndCommonPasswords(string password)
    {
        Assert.False(AuthRules.IsPasswordAllowed(password));
    }

    [Fact]
    public void PasswordPolicy_AcceptsLongNonCommonPassword()
    {
        Assert.True(AuthRules.IsPasswordAllowed("correct horse battery staple"));
    }

    [Theory]
    [InlineData("pending", "confirm", "provider", "confirmed")]
    [InlineData("pending", "decline", "provider", "declined")]
    [InlineData("pending", "cancel", "client", "cancelled")]
    [InlineData("confirmed", "cancel", "provider", "cancelled")]
    [InlineData("confirmed", "complete", "provider", "completed")]
    [InlineData("confirmed", "mark_no_show", "provider", "no_show")]
    [InlineData("confirmed", "reschedule", "client", "pending")]
    public void BookingLifecycle_AllowsDocumentedTransitions(string source, string action, string role, string target)
    {
        Assert.True(BookingLifecycle.TryTransition(source, action, role, out var actual));
        Assert.Equal(target, actual);
    }

    [Theory]
    [InlineData("pending", "confirm", "client")]
    [InlineData("completed", "cancel", "client")]
    [InlineData("declined", "reschedule", "provider")]
    [InlineData("pending", "complete", "provider")]
    public void BookingLifecycle_RejectsWrongActorAndInvalidTransitions(string source, string action, string role)
    {
        Assert.False(BookingLifecycle.TryTransition(source, action, role, out _));
    }
}
