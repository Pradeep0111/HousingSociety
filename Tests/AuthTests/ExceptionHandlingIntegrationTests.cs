using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Tests.TestHelpers;
using Xunit;

namespace Tests.AuthTests
{
    /// HS-7 replaced the IExceptionHandler-based GlobalExceptionHandler with a real
    /// RequestDelegate middleware (Api/Middleware/ExceptionHandlingMiddleware, wired via
    /// app.UseMiddleware<>()). That is exactly the class of change (pipeline wiring, not service
    /// logic) that produced the HS-6 regression, and nothing exercised it over real HTTP yet --
    /// every Application-layer exception type is only proven to be thrown correctly at the unit
    /// level, never proven to reach the client as the right status code end-to-end. This closes
    /// that gap for the three status codes not already covered by AuthIntegrationTests (401, 403).
    public class ExceptionHandlingIntegrationTests : IClassFixture<ApiFactory>
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly ApiFactory _factory;

        public ExceptionHandlingIntegrationTests(ApiFactory factory) => _factory = factory;

        private async Task<HttpClient> AdminClientAsync()
        {
            var client = _factory.CreateClient();
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = ApiFactory.BootstrapAdminEmail,
                password = ApiFactory.BootstrapAdminPassword,
            }, JsonOptions);
            loginResponse.EnsureSuccessStatusCode();
            var body = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
            return client;
        }

        [Fact]
        public async Task GetUnknownComplaint_Returns404_AsProblemDetails()
        {
            var client = await AdminClientAsync();

            var response = await client.GetAsync($"/api/complaints/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
            Assert.Equal(404, problem!.Status);
            Assert.Equal("Not Found", problem.Title);
        }

        [Fact]
        public async Task CreateComplaint_MissingDescription_Returns400_AsProblemDetails()
        {
            var client = await AdminClientAsync();
            Guid unitId, categoryId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var unit = TestData.NewUnit();
                var category = TestData.NewCategory();
                db.Units.Add(unit);
                db.ComplaintCategories.Add(category);
                await db.SaveChangesAsync();
                unitId = unit.Id;
                categoryId = category.Id;
            }

            var response = await client.PostAsJsonAsync("/api/complaints", new
            {
                unitId,
                complaintCategoryId = categoryId,
                description = "",
            }, JsonOptions);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
            Assert.Equal(400, problem!.Status);
            Assert.Equal("Bad Request", problem.Title);
        }

        [Fact]
        public async Task TransitionIllegalEdge_Returns409_AsProblemDetails()
        {
            var client = await AdminClientAsync();
            Guid complaintId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var unit = TestData.NewUnit();
                var category = TestData.NewCategory();
                // Resolved -> Assigned is not a legal edge in ComplaintStateMachine.
                var complaint = TestData.NewComplaint(unit.Id, category.Id, status: Domain.Enums.ComplaintStatus.Resolved);
                db.Units.Add(unit);
                db.ComplaintCategories.Add(category);
                db.Complaints.Add(complaint);
                await db.SaveChangesAsync();
                complaintId = complaint.Id;
            }

            var response = await client.PostAsJsonAsync($"/api/complaints/{complaintId}/transition", new
            {
                toStatus = (int)Domain.Enums.ComplaintStatus.Assigned,
                remarks = (string?)null,
            }, JsonOptions);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
            Assert.Equal(409, problem!.Status);
            Assert.Equal("Conflict", problem.Title);
        }
    }
}
