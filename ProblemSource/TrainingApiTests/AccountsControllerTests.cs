using ProblemSourceModule.Services.Storage;
using Shouldly;
using System;
using System.Net;
using System.Net.Http.Json;
using TrainingApi;
using TrainingApiTests.IntegrationHelpers;

namespace TrainingApiTests
{
    public class AccountsControllerTests
    {
        [Theory]
        [InlineData(null, true, HttpStatusCode.Unauthorized)]
        [InlineData(Roles.Teacher, true, HttpStatusCode.OK)]
        [InlineData(Roles.Teacher, false, HttpStatusCode.Forbidden)]
        [InlineData(Roles.Admin, false, HttpStatusCode.OK)]
        public async Task Accounts_Authorize_AdminOrTeacher(string? role, bool userIdSameAsRequested, HttpStatusCode expected)
        {
            var requestedId = "somename";
            var ts = new MyTestServer();
            var client = ts.CreateClient(role == null ? null : new User { Email = userIdSameAsRequested ? requestedId : "", Role = role });

            //var response = await client.GetAsync($"/api/accounts/jonas"); // Doesn't even reach AccountsController...
            var response = await client.GetAsync($"/api/accounts/getone?id={requestedId}");
            response.StatusCode.ShouldBe(expected);
        }

        [Theory]
        [InlineData(null, HttpStatusCode.Unauthorized)]
        [InlineData(Roles.Teacher, HttpStatusCode.Forbidden)]
        [InlineData(Roles.Admin, HttpStatusCode.OK)]
        public async Task Accounts_Authorize_AdminOnly(string? role, HttpStatusCode expected)
        {
            var ts = new MyTestServer();
            var client = ts.CreateClient(role == null ? null : new User { Role = role });

            //var responseX = await client.PostAsJsonAsync($"/api/trainings/refresh", new List<int> { 1,2,3 });

            var response = await client.GetAsync($"/api/accounts/");
            response.StatusCode.ShouldBe(expected);
        }

    }
}
