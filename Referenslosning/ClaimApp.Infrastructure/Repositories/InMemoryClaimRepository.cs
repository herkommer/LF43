using ClaimApp.Application.Interfaces;
using ClaimApp.Domain.Entities;
using ClaimApp.Domain.ValueObjects;

namespace ClaimApp.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of IClaimRepository
/// 
/// VARFÖR IN-MEMORY?
/// + Perfect för demo/development
/// + Inga dependencies (no database setup)
/// + Snabbt
/// + 100% testbart
/// - Data försvinner vid restart
/// - Inte thread-safe (problem vid concurrency)
/// - Skalar inte (allt i minnet)
/// 
/// MIGRATION TILL SQL:
/// 1. Skapa SqlClaimRepository : IClaimRepository
/// 2. Ändra DI registration från Singleton → Scoped
/// 3. Add EF Core DbContext
/// 4. Ingen ändring i Application eller Domain!
/// 
/// Detta visar värdet av Interface-based design
/// </summary>
public class InMemoryClaimRepository : IClaimRepository
{
    // Static list = delas mellan alla instanser
    // För demo: OK
    // För produktion: Problem! (måste vara thread-safe)
    private static readonly List<Claim> _claims = new();

    public Task<Claim> Save(Claim claim)
    {
        // Check om update eller create
        var existing = _claims.FirstOrDefault(c => c.Id == claim.Id);

        if (existing != null)
        {
            // Update: Ta bort gamla, lägg till nya
            _claims.Remove(existing);
        }

        _claims.Add(claim);

        // Task.FromResult eftersom vi är synkrona men interfacet är async
        return Task.FromResult(claim);
    }

    public Task<Claim?> GetById(Guid id)
    {
        var claim = _claims.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(claim);
    }

    public Task<IEnumerable<Claim>> GetAll()
    {
        // Returnera kopia för att undvika external modification
        return Task.FromResult<IEnumerable<Claim>>(_claims.ToList());
    }

    public Task<IEnumerable<Claim>> GetByRegistrationNumber(string registrationNumber)
    {
        // Skapa RegistrationNumber för value equality comparison
        var regNr = new RegistrationNumber(registrationNumber);

        var vehicleClaims = _claims
            .OfType<VehicleClaim>()
            .Where(c => c.RegistrationNumber == regNr)
            .ToList();

        return Task.FromResult<IEnumerable<Claim>>(vehicleClaims);
    }
}

// SQL IMPLEMENTATION EXAMPLE (för diskussion):
/*
public class SqlClaimRepository : IClaimRepository
{
    private readonly AppDbContext _context;
    
    public SqlClaimRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<Claim> Save(Claim claim)
    {
        var existing = await _context.Claims
            .FirstOrDefaultAsync(c => c.Id == claim.Id);
        
        if (existing == null)
            _context.Claims.Add(claim);
        else
            _context.Entry(existing).CurrentValues.SetValues(claim);
        
        await _context.SaveChangesAsync();
        return claim;
    }
    
    // ... resten
}

// I Program.cs:
// builder.Services.AddScoped<IClaimRepository, SqlClaimRepository>();
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(connectionString));
*/
