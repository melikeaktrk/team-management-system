using TeamTaskManager.DTO.Auth;

namespace TeamTaskManager.Tests;

public class LoginPayloadTests
{
    [Fact]
    public void LoginRequest_AllowsEmailAndPassword()
    {
        var request = new LoginRequest
        {
            Email = "admin@test.com",
            Password = "Admin123!"
        };

        Assert.Equal("admin@test.com", request.Email);
        Assert.Equal("Admin123!", request.Password);
    }
}
