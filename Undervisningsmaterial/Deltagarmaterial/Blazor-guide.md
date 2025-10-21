# Blazor Guide för utvecklare med JS-bakgrund

**Syfte:** Snabbreferens för dig som känner till React/Vue och vill lära dig Blazor

---

## Snabbstart: Blazor för JS-utvecklare

### Grundläggande jämförelse

| Koncept         | React/Vue              | Blazor                    |
| --------------- | ---------------------- | ------------------------- |
| **Fil-typ**     | `.jsx` / `.vue`        | `.razor`                  |
| **Språk**       | JavaScript/TypeScript  | C#                        |
| **Komponent**   | Function/Class         | Razor component med @code |
| **Template**    | JSX / Template string  | Razor syntax (HTML + C#)  |
| **State**       | `useState()`, `data()` | Private fields i @code    |
| **Props**       | Props parameter        | `[Parameter]` attribute   |
| **Events**      | `onClick={fn}`         | `@onclick="fn"`           |
| **Conditional** | `{condition && <div>}` | `@if (condition) { }`     |
| **Loop**        | `.map()`               | `@foreach`                |
| **Two-way**     | Value + onChange       | `@bind`                   |

---

## Vanliga frågor & svar

### Varför Blazor Server istället för WebAssembly?

**Skillnaden:**

- **Blazor Server:** Logiken körs på servern, UI-uppdateringar skickas via SignalR
- **Blazor WebAssembly:** Hela appen (inkl .NET runtime) laddas ned och körs i browsern

**Blazor Server fördelar:**

- **Snabbare initial load** - inget behov av att ladda .NET till klienten
- **Full .NET runtime** tillgänglig direkt, kan använda alla NuGet-paket
- **Enklare att komma igång med**
- Perfekt för **interna applikationer**

**WebAssembly fördelar:**

- Fungerar offline
- Mindre server-belastning
- Bättre för publika appar

---

### Vad är SignalR?

SignalR är Microsofts **realtids-kommunikationsbibliotek** över WebSockets.

I Blazor Server används det för att skicka UI-uppdateringar från server till browser:

1. Du klickar på en knapp
2. Event skickas till servern via SignalR
3. Servern re-renderar komponenten
4. Diff skickas tillbaka via SignalR
5. Browser uppdaterar DOM

**Jämförelse:**

- **React:** State ändras → React re-renderar → DOM uppdateras (allt på klienten)
- **Blazor Server:** State ändras → Server re-renderar → Diff skickas via SignalR → Browser uppdaterar DOM

**Ingen konfiguration behövs** - det fungerar automatiskt.

---

### Hur fungerar @bind egentligen?

Detta är en av de stora skillnaderna mot React.

**I React behöver du skriva:**

```jsx
const [name, setName] = useState("");
<input value={name} onChange={(e) => setName(e.target.value)} />;
```

**I Blazor räcker:**

```razor
<input @bind="name" />
```

**Under huven** skapar Blazor både:

- Value binding (visar värdet)
- Event binding (uppdaterar vid change)

**Detaljerad expansion:**

```razor
@bind="name"

// Expanderar till:
value="@name"
@onchange="@((ChangeEventArgs e) => name = e.Value?.ToString())"
```

**Du kan också välja event:**

```razor
@bind="name" @bind:event="oninput"  <!-- Uppdaterar varje tecken istället för onblur -->
```

---

### Vad är skillnaden mellan @code och code-behind?

**Alternativ 1: Allt i en fil**

```razor
@* MyComponent.razor *@
<h3>@Title</h3>

@code {
    [Parameter]
    public string Title { get; set; }
}
```

**Alternativ 2: Separata filer**

```
MyComponent.razor       // Endast markup
MyComponent.razor.cs    // Endast C#-kod
```

**Liknar:**

- Vue: Single File Components vs separata filer
- React: JSX i samma fil vs separata filer

**Rekommendation:**

- Enkel komponent: allt i en fil
- Komplex logik: separera till code-behind

---

### Hur fungerar routing?

I Blazor är routing **fil-baserat** (som Next.js):

```razor
@page "/claims/create"
```

Detta gör komponenten tillgänglig på `/claims/create`.

**Multipla routes:**

```razor
@page "/claims/create"
@page "/claims/new"
```

**Route parameters:**

```razor
@page "/claims/{id:guid}"

@code {
    [Parameter]
    public Guid Id { get; set; }
}
```

**Navigation programmatiskt:**

```razor
@inject NavigationManager Nav

<button @onclick="GoToClaims">Gå till lista</button>

@code {
    void GoToClaims() => Nav.NavigateTo("/claims");
}
```

---

### Vad är skillnaden mellan StateHasChanged() och automatisk re-rendering?

**Automatisk re-rendering** händer när:

- Event handler körs (`@onclick`, `@onchange`, etc.)
- Parameter uppdateras från parent

**Manuell `StateHasChanged()`** behövs när:

- Timer/callback från extern kod
- Background thread uppdaterar state
- Event från .NET event (inte Blazor event)

**Exempel:**

```csharp
private Timer? _timer;

protected override void OnInitialized()
{
    _timer = new Timer(1000);
    _timer.Elapsed += (s, e) =>
    {
        currentTime = DateTime.Now;
        StateHasChanged();  // Behövs! Event kommer från .NET Timer
    };
    _timer.Start();
}
```

**Jämförelse:**

- **React:** `setState()` triggar alltid re-render
- **Blazor:** Re-render är automatisk vid UI-events, manuell annars

---

## Lifecycle hooks

| React                  | Blazor                                         | Syfte                                 |
| ---------------------- | ---------------------------------------------- | ------------------------------------- |
| `componentDidMount`    | `OnInitialized()` / `OnInitializedAsync()`     | Körs när komponenten skapas           |
| `componentDidUpdate`   | `OnParametersSet()` / `OnParametersSetAsync()` | Körs när parameters uppdateras        |
| `componentWillUnmount` | `Dispose()` (implementera IDisposable)         | Städa upp resurser                    |
| -                      | `OnAfterRender()` / `OnAfterRenderAsync()`     | Körs efter rendering (för JS interop) |

**Exempel:**

```csharp
protected override async Task OnInitializedAsync()
{
    claims = await ClaimService.GetAllClaims();
}

protected override void OnParametersSet()
{
    // Körs när [Parameter] uppdateras
}

public void Dispose()
{
    // Städa upp subscriptions, timers, etc.
}
```

---

## Vanliga fallgropar

### 1. Glömma async/await

**Problem:**

```csharp
@code {
    private void LoadData()
    {
        claims = ClaimService.GetAllClaims();  // Fel! Returnerar Task
    }
}
```

**Lösning:**

```csharp
private async Task LoadData()
{
    claims = await ClaimService.GetAllClaims();
}
```

---

### 2. Försöka uppdatera state från non-UI thread

**Problem:**

```csharp
Task.Run(() => {
    myData = GetNewData();  // Kraschar eller funkar inte
});
```

**Lösning:**

```csharp
Task.Run(async () => {
    var data = GetNewData();
    await InvokeAsync(() => {
        myData = data;
        StateHasChanged();
    });
});
```

---

### 3. Null reference på parameters

**Problem:**

```razor
@code {
    [Parameter]
    public Claim Claim { get; set; }  // Kan vara null!

    protected override void OnInitialized()
    {
        var desc = Claim.Description;  // NullReferenceException
    }
}
```

**Lösning:**

```csharp
[Parameter]
public Claim? Claim { get; set; }  // Markera som nullable

// Eller:
[Parameter, EditorRequired]
public Claim Claim { get; set; } = null!;  // Kräv värde
```

---

### 4. Binda till property utan setter

**Problem:**

```razor
<input @bind="FullName" />

@code {
    public string FullName => $"{FirstName} {LastName}";  // Readonly!
}
```

**Lösning:**

```csharp
public string FullName
{
    get => $"{FirstName} {LastName}";
    set
    {
        var parts = value.Split(' ');
        FirstName = parts[0];
        LastName = parts.Length > 1 ? parts[1] : "";
    }
}
```

---

## Utvecklingsverktyg

### Hot Reload

Blazor har inbyggd hot reload - **behöver inte starta om appen**.

```bash
dotnet watch
```

Ändra kod → Spara → Browsern uppdateras automatiskt!

---

### Debugga med breakpoints

**Funkar i VS Code:**

1. Sätt breakpoint i .razor.cs-fil eller i @code-block
2. Tryck F5 (Start Debugging)
3. Interagera med appen
4. Debuggern stannar på breakpoint

---

### Console logging

```csharp
@inject ILogger<CreateClaim> Logger

@code {
    private void HandleClick()
    {
        Logger.LogInformation("Knappen klickades!");
        Console.WriteLine("Detta syns i terminal");
    }
}
```

**Loggarna visas i:**

- Terminal där `dotnet run` körs
- Browser DevTools Console (vissa logs)

---

### Visa felmeddelanden till användaren

```razor
@if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="alert alert-danger">@errorMessage</div>
}

@code {
    private string errorMessage = "";

    private async Task HandleSubmit()
    {
        try
        {
            await ClaimService.CreateClaim(claim);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
    }
}
```

---

## Felsökning

### Projektet bygger inte

1. Kolla att alla using-statements finns
2. Verifiera namespace-namn matchar mappstruktur
3. Kör `dotnet clean` och `dotnet build` igen

---

### Hot reload fungerar inte

1. Stoppa och starta om `dotnet watch`
2. Hård refresh i browsern (Ctrl+Shift+R)
3. Worst case: starta om helt

---

### "Service not found" error

1. Kolla att service är registrerad i `Program.cs`
2. Verifiera stavning av interface-namn
3. Kolla att rätt livscykel används (Scoped/Singleton)

---

### UI uppdateras inte

1. Kolla att `@bind` är rätt stavat
2. Verifiera att event handler faktiskt uppdaterar state
3. Prova manuell `StateHasChanged()` om inget annat funkar

---

## Resurser för fördjupning

**Officiell dokumentation:**

- https://learn.microsoft.com/en-us/aspnet/core/blazor/

**Tutorials:**

- https://dotnet.microsoft.com/learn/aspnet/blazor-tutorial/intro

**Comparison guide:**

- https://learn.microsoft.com/en-us/aspnet/core/blazor/components/

**Community:**

- Blazor University: https://blazor-university.com/
- Awesome Blazor: https://github.com/AdrienTorris/awesome-blazor
