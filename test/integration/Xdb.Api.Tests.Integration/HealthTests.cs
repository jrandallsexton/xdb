using System.Net;
using System.Threading.Tasks;

using FluentAssertions;

using Xunit;

namespace Xdb.Api.Tests.Integration;

public class HealthTests(XdbApiFactory factory) : IClassFixture<XdbApiFactory>
{
    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }
}
