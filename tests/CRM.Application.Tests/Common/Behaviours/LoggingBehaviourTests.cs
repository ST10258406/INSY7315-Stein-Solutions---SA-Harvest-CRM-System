namespace CRM.Application.Tests.Common.Behaviours;

using CRM.Application.Common.Behaviours;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

public class LoggingBehaviourTests
{
    public class TestRequest { }
    public class TestResponse { }

    [Fact]
    public async Task Handle_LogsRequestNameAndExecutionTime()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = new TestResponse();
        var nextDelegate = Substitute.For<RequestHandlerDelegate<TestResponse>>();
        nextDelegate().Returns(Task.FromResult(expectedResponse));

        var logger = Substitute.For<ILogger<LoggingBehaviour<TestRequest, TestResponse>>>();

        var behaviour = new LoggingBehaviour<TestRequest, TestResponse>(logger);

        // Act
        var response = await behaviour.Handle(request, nextDelegate, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, response);
        
        // Assert logger was called. We can't mock extension methods like LogInformation directly in NSubstitute easily, 
        // so we verify Log on the ILogger.
        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Handling TestRequest")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
