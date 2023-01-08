using ProblemSourceModule.Services.Storage;
using Shouldly;
using System;
using System.Net.Http.Json;
using TrainingApi;
using TrainingApiTests.IntegrationHelpers;

namespace TrainingApiTests
{
    public class AccountsControllerTests
    {
        [Theory]
        //[InlineData(null)]
        //[InlineData(Roles.Teacher)]
        [InlineData(Roles.Admin)]
        public async Task Accounts_Authorize(string? role)
        {
            var ts = new MyTestServer();

            var client = ts.CreateClient(role == null ? null : new User { Role = role });

            var response = await client.GetAsync($"/api/accounts?id=jonas");
            //var response = await client.GetAsync($"/api/accounts/"); //getall
            response.StatusCode.ShouldBe(role != Roles.Admin ? System.Net.HttpStatusCode.Unauthorized : System.Net.HttpStatusCode.OK);
        }
    }
}
