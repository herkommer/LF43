# Laboration: Modulär Skadeanmälningsapplikation

**Ämne:** Modulär applikationsstruktur i .NET och UI-ramverk  
**Tid:** 2 veckor (estimerad arbetstid: ~6 timmar)  
**Inlämning:** GitHub-repo länk + live-presentation + arkitekturdiskussion

---

## Bakgrund

Under lektionen byggde vi en enkel skadeanmälningsapplikation med två skadetyper (Fordon & Egendom). Denna övning går djupare: du ska designa och implementera en mer komplett lösning som provocerar arkitektoniska beslut och tvingar dig att hantera komplexitet.

Du har stor frihet att fatta egna designbeslut, men bör kunna motivera varje val under presentationen.

Fokus ligger på att utmana dina befintliga mentala modeller kring arkitektur, testbarhet och separation of concerns.

---

## Syfte

✅ **Designa och motivera** en lagerseparation i .NET enligt Clean Architecture  
✅ **Identifiera brytpunkter** mellan lager och motivera interface-design  
✅ **Hantera polymorfism och arv** i domänmodeller på ett underhållbart sätt  
✅ **Implementera Dependency Injection** och förstå skillnaden mellan lifestyle (Scoped/Singleton/Transient)  
✅ **Argumentera för trade-offs** mellan enkelhet och flexibilitet  
✅ **Reflektera över** hur samma problem skulle lösas i andra kodbaser

---

## Uppgift

### Kärnkrav (minimum viable product)

**1. Tre skadetyper med olika komplexitet**

Applikationen ska hantera tre typer av skadeanmälningar:

**Fordon** (enkel)

- Registreringsnummer (validering: ABC123 eller ABC12D)
- Datum för skada
- Plats där skadan inträffade
- Beskrivning av skadan

**Egendom** (medel)

- Adress
- Typ av skada: Brand, Vatten, Inbrott, Övrigt (enum)
- Uppskattad kostnad för skadan
- Beskrivning av skadan

**Resa** (komplex - har tidsspan och relationer)

- Resmål/land
- Startdatum + Slutdatum för resan
- Typ av incident: Försenad/Inställd, Medicinsk, Förlorat bagage, Övrigt
- Beskrivning av incidenten

**2. Formulär med smart rendering**

- Single-page formulär med dynamiska fält baserat på skadetyp
- Gemensamma fält ska inte dupliceras (DRY-princip)
- Validering på både client- och server-sida (diskutera var gränsen går)

**3. Lista och detaljvy**

- Översikt av alla anmälningar
- Möjlighet att filtrera/söka (valfri implementation)
- Detaljvy som visar alla properties för en specifik anmälan

**4. Affärsregler (implementera minst 3)**

Implementera **minst 3 affärsregler** och placera logiken där den arkitektoniskt hör hemma:

- Fordonsskador äldre än 30 dagar → flaggas för manuell granskning
- Egendomsskador över 50,000 kr → automatisk eskalering
- Reseskador måste rapporteras inom 14 dagar efter hemkomst
- Beskrivning måste vara minst 20 tecken
- **Lägg till minst 1 egen affärsregel** som kräver koordination mellan flera entities

---

### Arkitektoniska krav & utmaningar

**Separata projekt**

```
ClaimApp.sln
├── ClaimApp.Web/              # Blazor Server UI
├── ClaimApp.Application/      # Use cases, services, interfaces
├── ClaimApp.Domain/           # Entities, value objects, domain logic
└── ClaimApp.Infrastructure/   # Repository implementations, external concerns
```

**Valfri fördjupning (utmana dig själv):**

- **Utmaning A:** Lägg till API-lager (ClaimApp.API) och separera UI från backend
- **Utmaning B:** Lägg till ett test-projekt och skriv unit tests för affärsregler
- **Utmaning C:** Implementera CQRS-mönster med separata read/write models

---

### Tekniska komponenter

✅ **Domain Layer**

- Entity-klasser med inheritance eller composition (välj själv - motivera/resonera)
- **Rich domain models:** Affärslogik i domain-objekten, inte anemic models
- Value Objects för koncept som "Registreringsnummer" eller "DateRange"
- Enums för kategorier

✅ **Application Layer**

- Service med koordinering mellan repositories och domain logic
- **Interfaces för alla externa beroenden** (repository, notification, etc.)
- DTOs om vi behöver separera domain från presentation
- **Fundera över:** Validation strategy - FluentValidation? Data Annotations? Domain validation?

✅ **Infrastructure Layer**

- Repository-implementation (in-memory är OK, men överväg interface-design för framtida SQL)
- Håll implementation-detaljer borta från domain

✅ **Presentation Layer**

- Blazor Server-komponenter med komponentisering
- Smart conditional rendering utan duplicerad markup
- **Fundera över:** State management - var lever state? Component state? Service? SignalR?

✅ **Dependency Injection & Configuration**

- Korrekt lifestyle för services (Scoped/Singleton/Transient) - **resonera kring valmöjligheter**

---

### Arkitektoniska diskussionspunkter

Förbered dig att resonera kring dessa designbeslut under presentationen:

**🔴 Kritiska frågor att resonera kring:**

1. **Varför just denna lagerseparation?** Vad vinner vi? Vad förlorar vi?
2. **Arv vs Composition:** Hur designads domänmodellerna? Varför?
3. **Var lever affärsreglerna?** I domain entities? I service? Varför just där?
4. **Interfaces:** Hur många interfaces skapades? Är de alla nödvändiga?
5. **Testbarhet:** Hur enkelt är det att unit-testa? Vad är svårt att testa?
6. **Blazor state management:** Var lever formulär-state? Hur hanteras state mellan components?
7. **Validation strategy:** Client-side? Server-side? Båda? Hur undviker vi dubblering?
8. **DI Lifestyle:** Varför valdes Scoped/Singleton/Transient för tjänsterna?
9. **DTO vs Domain models:** Använder du samma modeller överallt eller separerar du?
10. **Future-proofing:** Hur svårt är det att byta till en riktig databas? Till ett API?

**🟡 Kodkvalitet & SOLID-principer:**

- **Single Responsibility:** Har varje klass ett tydligt, enskilt ansvar?
- **Open/Closed:** Hur enkelt är det att lägga till en 4:e skadetyp?
- **Liskov Substitution:** Fungerar polymorfism om vi har arv?
- **Interface Segregation:** Är interfaces fokuserade eller "fat"?
- **Dependency Inversion:** Beror high-level policies på low-level detaljer?

**🟢 Separation of Concerns:**

- Ingen affärslogik i UI-komponenter (men: var går gränsen för UI-validering?)
- Ingen dataåtkomst i Services (men: är repository bara en dataåtkomst-abstraction?)
- Domain-lagret får inte ha externa beroenden (men: hur hanterar du cross-cutting concerns som logging?)

---

## Inlämning

### 1. GitHub-repository

Skapa ett publikt GitHub-repo med:

- Komplett källkod
- README.md med:
  - Beskrivning av projektet
  - Instruktioner för att köra lokalt
  - Eventuella beroenden/versioner
- .gitignore för .NET-projekt
- Commit-historik som visar utvecklingsprocess

**Lämna in länken den 30/10 under morgonen**

---

### 2. Live-presentation (8-10 min)

Under uppföljningstillfället presenterar du:

**Demo:**

- Kör applikationen live
- Visa alla tre skadetyper med focus på en komplex case
- Demonstrera minst 2 affärsregler och hur de triggas
- Visa listan och detaljvy

**Arkitektur överflygning:**

- **Solution-struktur:** Visa projekt-referenser och beroenderiktning
- **Domain layer walkthrough:** Visa hur du designade entities (arv/composition)
- **Application layer:** Hur koordinerar servicen? Var lever validering?
- **Infrastructure:** Visa repository-interface och implementation
- **Presentation:** Hur hanterades conditional rendering och state management?
- **DI-setup:** Visa Program.cs - motivera dina lifestyle-val

**Arkitektonisk resonemang:**

- **Vad skulle du göra annorlunda nästa gång?** (trade-offs du gjorde)
- **Hur skulle detta skala?** (om 10 skadetyper? Om 100 000 användare?)
- **Vad är mest "code smell" i din lösning?** (var finns technical debt?)

---

### 3. Kort skriftlig reflektion

Fyll i reflektionsmallen (se `reflektion.md`) och lägg i GitHub-repot.

---

## Resurser & referenser

**Djupdyk i arkitektur:**

- Clean Architecture (Uncle Bob): https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html
- Domain-Driven Design patterns: https://martinfowler.com/tags/domain%20driven%20design.html
- SOLID principles: https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/may/csharp-best-practices-dangers-of-violating-solid-principles-in-csharp

**Blazor & .NET:**

- Blazor documentation: https://learn.microsoft.com/en-us/aspnet/core/blazor/
- Dependency Injection lifetimes: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection
- Value Objects in C#: https://enterprisecraftsmanship.com/posts/value-objects-explained/

---

## Vanliga frågor (FAQ)

**"Måste jag implementera en riktig databas?"**

- Nej, in-memory repository är OK. Men du måste visa att din arkitektur **gör det enkelt** att byta till SQL senare.

**"Får jag använda NuGet-paket som FluentValidation, MediatR, etc.?"**

- Ja, men varje dependency måste motiveras i README. Överdriv inte.

**"Kan jag arbeta i par eller grupp?"**

- Ja, men **alla** måste kunna förklara all kod vid presentation. En person kan inte "äga" ett lager.

**"Behöver jag göra det 'perfekt'?"**

- Nej. Perfekt finns inte. Sikta på **väl genomtänkt och motiverat**. Code smells är OK om du kan identifiera dem.

---
