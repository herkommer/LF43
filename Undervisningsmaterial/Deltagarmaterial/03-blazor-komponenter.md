# Del 3: Blazor och Komponentisering

---

## Lektionsöversikt

- Introduktion till Blazor Server
- Komponenter och återanvändning
- State management
- Event handling
- Conditional rendering
- Formulär och validering

---

## 1. Vad är Blazor?

### Definition

Blazor är ett **komponentbaserat UI-ramverk** för .NET som låter dig bygga interaktiva webbapplikationer med **C# istället för JavaScript**.

### Blazor Server vs Blazor WebAssembly

**Blazor Server** (det vi använder idag):

- Körs på servern
- UI-uppdateringar skickas via SignalR (WebSocket)
- Snabb initial load
- Full .NET runtime tillgänglig
- Bra för interna applikationer

**Blazor WebAssembly:**

- Körs i browsern
- Laddar ner .NET runtime till klienten
- Kan köra offline
- Långsammare initial load
- Bra för publika applikationer

### Varför Blazor för .NET-utvecklare?

✅ **En enda teknologi** - C# överallt (backend + frontend)

✅ **Komponentbaserat** - liknar React/Vue/Angular

✅ **Starkt typat** - kompileringsfel istället för runtime-fel

✅ **Återanvändning** - samma kod kan användas i olika kontexter

✅ **Ecosystem** - NuGet-paket, tooling, community

---

## 2. Blazor-komponenter - grunder

### Vad är en komponent?

En **komponent** är en återanvändbar del av UI med egen logik och rendering.

**Enkel komponent:**

```razor
@* Fil: Components/Greeting.razor *@

<h3>Hej, @Name!</h3>

@code {
    [Parameter]
    public string Name { get; set; } = "Gäst";
}
```

**Användning:**

```razor
<Greeting Name="Anna" />
<Greeting Name="Erik" />
<Greeting />  @* Använder default-värdet "Gäst" *@
```

### Komponentstruktur

En Blazor-komponent har två delar:

**1. Markup (HTML + Razor-syntax)**

```razor
<div class="claim-card">
    <h4>@Claim.Type</h4>
    <p>@Claim.Description</p>
    <span class="date">@Claim.ReportedDate.ToShortDateString()</span>
</div>
```

**2. Code-behind (C#-logik)**

```razor
@code {
    [Parameter]
    public Claim Claim { get; set; } = null!;

    private void OnClaimClicked()
    {
        // Logik här
    }
}
```

**Alternativt: Separata filer**

```
ClaimCard.razor        // Markup
ClaimCard.razor.cs     // Code-behind
```

---

## 3. Parameters och Data Binding

### Parameters - data in i komponenten

```razor
@* ChildComponent.razor *@
<div>
    <p>Titel: @Title</p>
    <p>Count: @Count</p>
</div>

@code {
    [Parameter]
    public string Title { get; set; } = "";

    [Parameter]
    public int Count { get; set; }
}
```

**Användning från parent:**

```razor
<ChildComponent Title="Min Titel" Count="42" />
```

### Two-way binding med @bind

```razor
<input type="text" @bind="userName" />
<p>Du skrev: @userName</p>

@code {
    private string userName = "";
}
```

**Vad händer?**

- `@bind` skapar både value-binding och onchange-event
- När användaren skriver uppdateras `userName`
- UI re-renderas automatiskt

### Binding till olika element

```razor
@* Text input *@
<input type="text" @bind="name" />

@* Checkbox *@
<input type="checkbox" @bind="isAccepted" />

@* Select/dropdown *@
<select @bind="selectedType">
    <option value="Vehicle">Fordon</option>
    <option value="Property">Egendom</option>
</select>

@code {
    private string name = "";
    private bool isAccepted = false;
    private string selectedType = "Vehicle";
}
```

---

## 4. Events och State Management

### Event handling

```razor
<button @onclick="HandleClick">Klicka här</button>
<button @onclick="() => counter++">Öka räknare</button>

@code {
    private int counter = 0;

    private void HandleClick()
    {
        counter++;
        Console.WriteLine($"Knappen klickades! Counter: {counter}");
    }
}
```

**Vanliga events:**

- `@onclick` - klick
- `@onchange` - värde ändrat
- `@oninput` - input (varje tecken)
- `@onsubmit` - formulär submittas

### State i komponenter

**State** är data som påverkar vad som renderas.

```razor
<p>Du har klickat @clickCount gånger</p>
<button @onclick="IncrementCount">Klicka</button>

@if (clickCount > 5)
{
    <p class="warning">Det räcker nu!</p>
}

@code {
    private int clickCount = 0;  // State

    private void IncrementCount()
    {
        clickCount++;  // Uppdatera state
        // Blazor re-renderar automatiskt
    }
}
```

**Viktigt:**

- När state ändras, re-renderas komponenten automatiskt
- Endast data i komponenten är state
- För delad state mellan komponenter, använd services

---

## 5. Conditional Rendering

### @if, @else

```razor
@if (isLoading)
{
    <p>Laddar...</p>
}
else if (claims.Count == 0)
{
    <p>Inga skador att visa</p>
}
else
{
    <ul>
        @foreach (var claim in claims)
        {
            <li>@claim.Description</li>
        }
    </ul>
}
```

### Visa/dölja baserat på användarinput

```razor
<select @bind="claimType">
    <option value="">Välj typ</option>
    <option value="Vehicle">Fordon</option>
    <option value="Property">Egendom</option>
</select>

@if (claimType == "Vehicle")
{
    <VehicleClaimForm />
}
else if (claimType == "Property")
{
    <PropertyClaimForm />
}

@code {
    private string claimType = "";
}
```

**Detta är kärnan i vår demo-app!**

- Användaren väljer skadetyp
- Rätt formulär visas dynamiskt

---

## 6. Dependency Injection i Blazor

### Injicera services i komponenter

```razor
@page "/claims/create"
@inject IClaimService ClaimService

<h3>Skapa skadeanmälan</h3>

<button @onclick="CreateClaim">Spara</button>

@code {
    private async Task CreateClaim()
    {
        var request = new ClaimRequest { /* ... */ };
        await ClaimService.CreateClaim(request);
    }
}
```

**Vad händer?**

1. `@inject` ber Blazor om en instans av `IClaimService`
2. Blazor tittar i DI-containern (registrerad i Program.cs)
3. En instans skapas/hämtas och injiceras
4. Vi kan använda servicen direkt

### Varför detta är kraftfullt

✅ Komponenten vet inte VAR data kommer ifrån

✅ Lätt att testa - injicera mock-service

✅ Samma pattern som i backend-kod

---

## Jämförelse: Blazor vs React

För dig med React-bakgrund:

| Koncept         | React                  | Blazor                         |
| --------------- | ---------------------- | ------------------------------ |
| **Komponent**   | Function/Class         | .razor-fil med @code           |
| **Props**       | `props.name`           | `[Parameter]`                  |
| **State**       | `useState()`           | Private fields                 |
| **Event**       | `onClick={handler}`    | `@onclick="handler"`           |
| **Conditional** | `{condition && <div>}` | `@if (condition) { }`          |
| **Loop**        | `{items.map(item =>)}` | `@foreach (var item in items)` |
| **Two-way**     | Controlled component   | `@bind`                        |
| **DI**          | Context/Props drilling | `@inject`                      |

**Likheter:**

- Komponentbaserat tänk
- Unidirectional data flow (data ner, events upp)
- Återanvändning av komponenter
- State-driven rendering

**Skillnader:**

- Blazor är starkt typat
- Blazor har automatisk two-way binding
- Blazor använder C# istället för JSX

---

## Sammanfattning

**Blazor-komponenter:**

✅ Självständiga UI-delar med markup + logik

✅ Parameters för att ta emot data

✅ @bind för two-way data binding

✅ Event handlers för interaktion

✅ Conditional rendering för dynamiskt UI

✅ @inject för att använda services
