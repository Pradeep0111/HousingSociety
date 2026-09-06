using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Tests.TestHelpers
{
    public static class AppDbContextFactory
    {
        // EF Core InMemory (not SQLite in-memory) was chosen because ComplaintService only issues
        // straightforward LINQ (FirstOrDefault/Where/OrderBy/Skip/Take/Count) with no raw SQL,
        // relational-only functions, or cross-statement transaction semantics under test. The
        // lighter provider is sufficient, keeps the suite fast, and avoids a native SQLite
        // dependency. Each call gets its own uniquely named database so tests never leak state.
        public static AppDbContext Create()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }
    }
}
