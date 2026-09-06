using Application.Services;
using Infrastructure.Authorization;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Tests.TestHelpers
{
    // Wires the real ComplaintService against repositories and a UnitAccessGuard backed by the
    // same in-memory AppDbContext the test seeded, plus whatever IAuthorizationService double the
    // test supplies (typically FakeAuthorizationService), so tests keep exercising the real
    // production wiring instead of a hand-rolled substitute for ComplaintService itself.
    public static class ComplaintServiceFactory
    {
        public static ComplaintService Create(AppDbContext db, IAuthorizationService authorizationService) => new(
            new ComplaintRepository(db),
            new ComplaintCategoryRepository(db),
            new ComplaintStatusHistoryRepository(db),
            new UnitAccessGuard(db, authorizationService),
            new UserRepository(db),
            new UnitOfWork(db));
    }
}
