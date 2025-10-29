using Core.Enums;
using Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NotificationService.Services;
using NotificationService.Strategies;

namespace NotificationService.UnitTests.Services;

public class NotificationStrategyFactoryTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly NotificationStrategyFactory _factory;

    public NotificationStrategyFactoryTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _factory = new NotificationStrategyFactory(_serviceProviderMock.Object);
    }

    [Theory]
    [InlineData(NotificationType.Email, typeof(EmailNotificationStrategy))]
    [InlineData(NotificationType.Sms, typeof(SmsNotificationStrategy))]
    [InlineData(NotificationType.Push, typeof(PushNotificationStrategy))]
    public void GetStrategy_WithValidType_ReturnsCorrectStrategy(NotificationType type, Type expectedType)
    {
        // Arrange
        var strategyMock = new Mock<INotificationStrategy>();
        strategyMock.Setup(x => x.Type).Returns(type);
        _serviceProviderMock.Setup(x => x.GetService(expectedType))
            .Returns(strategyMock.Object);

        // Act
        var strategy = _factory.GetStrategy(type);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Type.Should().Be(type);
        _serviceProviderMock.Verify(x => x.GetService(expectedType), Times.Once);
    }

    [Fact]
    public void GetStrategy_WithUnknownType_ThrowsArgumentException()
    {
        // Arrange
        var type = (NotificationType)999;

        // Act
        var act = () => _factory.GetStrategy(type);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"*No strategy found for notification type: {type}*");
    }

    [Theory]
    [InlineData(NotificationType.Email)]
    [InlineData(NotificationType.Sms)]
    [InlineData(NotificationType.Push)]
    public void GetStrategy_WhenStrategyNotRegistered_ThrowsInvalidOperationException(NotificationType type)
    {
        // Arrange
        _serviceProviderMock.Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns(null);

        // Act
        var act = () => _factory.GetStrategy(type);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Strategy for {type} is not registered*");
    }

    [Fact]
    public void GetSupportedTypes_ReturnsAllTypes()
    {
        // Act
        var types = NotificationStrategyFactory.GetSupportedTypes();

        // Assert
        types.Should().BeEquivalentTo(new[]
        {
            NotificationType.Email,
            NotificationType.Sms,
            NotificationType.Push
        });
    }
}
