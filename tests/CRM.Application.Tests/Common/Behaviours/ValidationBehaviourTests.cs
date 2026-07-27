namespace CRM.Application.Tests.Common.Behaviours;

using CRM.Application.Common.Behaviours;
using CRM.Application.Common.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using Xunit;
using ValidationException = CRM.Application.Common.Exceptions.ValidationException;

public class ValidationBehaviourTests
{
    public class TestRequest { }
    public class TestResponse { }

    [Fact]
    public async Task Handle_WithValidationErrors_ThrowsValidationExceptionAndPreventsNext()
    {
        // Arrange
        var request = new TestRequest();
        var nextDelegate = Substitute.For<RequestHandlerDelegate<TestResponse>>();
        
        var validator = Substitute.For<IValidator<TestRequest>>();
        var validationFailure = new ValidationFailure("Property", "Error Message");
        var validationResult = new ValidationResult(new[] { validationFailure });
        
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(validationResult));

        var behaviour = new ValidationBehaviour<TestRequest, TestResponse>(new[] { validator });

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => 
            behaviour.Handle(request, nextDelegate, CancellationToken.None));

        // Ensure next() was never called
        await nextDelegate.DidNotReceive()();
    }
}
