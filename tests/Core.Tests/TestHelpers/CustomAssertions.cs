using Core.Models;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace Core.Tests.TestHelpers;

public static class CustomAssertions
{
    public static NotificationResultAssertions Should(this NotificationResult instance)
    {
        return new NotificationResultAssertions(instance);
    }
}

public class NotificationResultAssertions : ReferenceTypeAssertions<NotificationResult, NotificationResultAssertions>
{
    public NotificationResultAssertions(NotificationResult instance)
        : base(instance)
    {
    }

    protected override string Identifier => "notification result";

    public AndConstraint<NotificationResultAssertions> BeSuccessful(string because = "", params object[] becauseArgs)
    {
        Subject.Success.Should().BeTrue(because, becauseArgs);
        Subject.Error.Should().BeNull(because, becauseArgs);
        Subject.MessageId.Should().NotBeNullOrWhiteSpace(because, becauseArgs);
        Subject.SentAt.Should().NotBeNull(because, becauseArgs);

        return new AndConstraint<NotificationResultAssertions>(this);
    }

    public AndConstraint<NotificationResultAssertions> BeFailure(string because = "", params object[] becauseArgs)
    {
        Subject.Success.Should().BeFalse(because, becauseArgs);
        Subject.Error.Should().NotBeNullOrWhiteSpace(because, becauseArgs);
        Subject.MessageId.Should().BeNull(because, becauseArgs);
        Subject.SentAt.Should().BeNull(because, becauseArgs);

        return new AndConstraint<NotificationResultAssertions>(this);
    }

    public AndConstraint<NotificationResultAssertions> HaveMessageId(string expectedMessageId, string because = "", params object[] becauseArgs)
    {
        Subject.MessageId.Should().Be(expectedMessageId, because, becauseArgs);
        return new AndConstraint<NotificationResultAssertions>(this);
    }

    public AndConstraint<NotificationResultAssertions> HaveError(string expectedError, string because = "", params object[] becauseArgs)
    {
        Subject.Error.Should().Be(expectedError, because, becauseArgs);
        return new AndConstraint<NotificationResultAssertions>(this);
    }
}
