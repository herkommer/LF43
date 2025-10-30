using ClaimApp.Web.Components;
using ClaimApp.Application.Interfaces;
using ClaimApp.Application.Services;
using ClaimApp.Infrastructure.Repositories;
using ClaimApp.Domain.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// === DEPENDENCY INJECTION CONFIGURATION ===

// Domain Services - Singleton (stateless, pure logic)
// RATIONALE: Ingen state, kan återanvändas säkert mellan requests
builder.Services.AddSingleton<ClaimBusinessRules>();

// Application Services - Scoped (per Blazor circuit/request)
// RATIONALE: En instans per användarsession, håller request context
builder.Services.AddScoped<IClaimService, ClaimService>();

// Infrastructure - Singleton för in-memory (shared state)
// VIKTIGT: Detta skulle vara Scoped för SQL! (DbContext är scoped)
// RATIONALE: In-memory behöver dela data mellan alla användare
builder.Services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();

// DISKUSSIONSPUNKT: Singleton vs Scoped vs Transient
//
// Singleton:
// + En instans för hela app lifetime
// + Bra för stateless services (ClaimBusinessRules)
// + Bra för shared state (InMemoryRepository)
// - FARLIGT för stateful services
// - FARLIGT för SQL (DbContext är inte thread-safe)
//
// Scoped:
// + En instans per request/circuit
// + Perfect för services som håller request context
// + Perfect för DbContext (EF Core default)
// - Något overhead (skapas om för varje request)
//
// Transient:
// + Ny instans varje gång den injectas
// + Perfect för lightweight, stateless services
// - Mest overhead
// - Sällan nödvändigt
//
// VARNING: Scoped service injected i Singleton = problem!
// Singletonget kommer hålla samma Scoped instans forever
// Blazor Server: Be extra försiktig med lifetimes!

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
