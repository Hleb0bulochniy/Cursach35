using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using Moq;
using MS_Back_Auth.Controllers;
using MS_Back_Auth.Data;
using Xunit;

namespace MS_Back_Auth.Tests
{
    public class AuthControllerTests
    {
        [Fact]
        public async Task UserRegistrationPost_ShouldReturnBadRequest_WhenPasswordsDoNotMatch()
        {
            var options = new DbContextOptionsBuilder<AuthContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            await using var context = new AuthContext();

            var helpFuncsMock = new Mock<IHelpFuncs>();
            helpFuncsMock
                .Setup(x => x.LogModelCreate("UserRegistrationPost", "Registration successful"))
                .Returns(new LogModel());

            helpFuncsMock
                .Setup(x => x.LogEventAsync(It.IsAny<LogModel>()))
                .Returns(Task.CompletedTask);

            var controller = new AuthController(context, helpFuncsMock.Object);

            var dto = new RegistrationDTO("user1", "user1@example.com", "Password123!", "Password456!");

            var result = await controller.UserRegistrationPost(dto);

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().Be("Passwords don't match");
        }
    }
}
