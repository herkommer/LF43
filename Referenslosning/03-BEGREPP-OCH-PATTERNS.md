# Begrepp och Patterns - Referensguide

Detta dokument sammanfattar alla tekniska begrepp, design patterns och principer som används i referenslösningen och diskuteras i uppföljningen.

---

## Domain-Driven Design (DDD)

### Value Objects

Objekt som definieras av sina värden snarare än identitet. Två Value Objects med samma värde är identiska. Används för att undvika "primitive obsession" och kapsla in domänspecifik validering.

### Domain Services

Services som innehåller affärslogik som inte naturligt hör hemma på en specifik entity. Används när logik involverar flera entities eller när logiken är komplex nog att förtjäna sin egen klass.

### Entities

Objekt som har en unik identitet och livscykel. Två entities med samma värden men olika ID är olika objekt.

### Anemic Domain Model

Anti-pattern där domain models endast innehåller data utan beteende. All logik hamnar i services istället för på objekten själva.

### Rich Domain Model

Domain models som innehåller både data och beteende. Affärslogik placeras på de objekt den tillhör, vilket ger bättre inkapsling och undviker att logik sprids.

---

## Arkitekturmönster

### Clean Architecture

Arkitekturstil som organiserar kod i koncentriska lager med beroenden som pekar inåt. Domain Layer är kärnan och har inga externa beroenden, vilket ger testbarhet och flexibilitet.

### Layered Architecture

Applikationen delas upp i lager (Domain, Application, Infrastructure, Presentation) där varje lager har ett specifikt ansvar och endast kommunicerar med närliggande lager.

### Repository Pattern

Abstraherar datåtkomst genom ett interface som liknar en collection. Separerar affärslogik från datalagringsdetaljer och möjliggör enkel byte av datakälla.

### Generic Repository

Repository som kan hantera vilken entity-typ som helst genom generics. Kontroversielt - kan ses som antipattern då det gömmer domänspecifika query-behov.

### Specific Repository

Repository med metoder skräddarsydda för en specifik entity-typ. Exponerar endast de queries som behövs för den domänen.

### Unit of Work Pattern

Koordinerar arbete över flera repositories och säkerställer att alla förändringar sparas eller rullas tillbaka tillsammans. Viktigt för transaktionskonsistens.

---

## SOLID Principles

### Single Responsibility Principle (SRP)

Varje klass ska ha exakt ett ansvar - en anledning att ändras. Förbättrar kodförståelse, testbarhet och underhåll.

### Open/Closed Principle (OCP)

Kod ska vara öppen för utökning men stängd för modifiering. Nya funktioner läggs till genom att skapa nya klasser, inte genom att ändra befintliga.

### Liskov Substitution Principle (LSP)

Subtyper måste kunna ersätta sina bastyper utan att beteendet ändras. Viktig princip för korrekt användning av arv och polymorfism.

### Interface Segregation Principle (ISP)

Klienter ska inte tvingas bero på metoder de inte använder. Skapa smala, fokuserade interfaces istället för stora allomfattande.

### Dependency Inversion Principle (DIP)

High-level moduler ska inte bero på low-level moduler - båda ska bero på abstraktioner. Möjliggör loosely coupled design och testbarhet.

---

## Design Patterns

### Composition over Inheritance

Designprincip som föredrar att bygga funktionalitet genom att komponera objekt istället för arvskedjor. Ger mer flexibilitet och undviker rigida hierarkier.

### Polymorphism

Förmågan att behandla olika typer genom ett gemensamt interface. Används för att undvika switch-statements och möjliggöra Open/Closed Principle.

### Dependency Injection (DI)

Pattern där objekt får sina beroenden injicerade utifrån istället för att skapa dem själva. Centralt för testbarhet och loose coupling.

### Strategy Pattern

Kapslar in algoritmer/beteenden i separata klasser som kan bytas ut. Implicit använt när olika claim-typer har olika valideringslogik.

---

## Validation Strategies

### Multi-Layer Validation

Validering på flera nivåer (UI, Application, Domain) för defense-in-depth. Balans mellan duplicering och säkerhet.

### Client-Side Validation

Validering i browser/UI för omedelbar feedback. Primärt för användarupplevelse, inte säkerhet.

### Server-Side Validation

Validering på server för säkerhet och affärsregler. Måste alltid finnas oavsett client-side validering.

### FluentValidation

Library för att definiera valideringsregler i C# med fluent API. Separerar validering från domain models och ger läsbar syntax. (Alternativ till Data Annotation)

### Data Annotations

Attribut-baserad validering direkt på properties. Enklare men mindre flexibelt än FluentValidation och kopplar domain till framework.

### Domain Validation

Validering i domain layer, ofta i constructors eller setters. Säkerställer att invalid state aldrig kan skapas.

---

## Dependency Injection Lifetimes

### Singleton

En instans skapas och delas för hela applikationens livstid. Användbart för stateless services och in-memory data som ska delas.

### Scoped

En instans per request/scope. Standard för de flesta services i web-applikationer, matchar databas-transaktionens livstid.

### Transient

Ny instans skapas varje gång den begärs. Används för lightweight, stateless services där delning kan skapa problem.

---

## Blazor Concepts

### Component State

Data som lever i en Blazor-komponent. Försvinner när komponenten unmountas. Används för lokal UI-state.

### Cascading Parameters

Värden som propagerar ner genom komponentträdet utan att behöva passas explicit. Användbart för delad context som tema eller användarinfo.

### State Management

Strategi för att hantera applikationsstate i Blazor. Kan vara lokal komponentstate, shared services eller state management libraries.

### Two-Way Binding

Binding där värdet synkas både från kod till UI och från UI till kod. I Blazor med `@bind` directive.

---

## Application Architecture Patterns

### DTOs (Data Transfer Objects)

Objekt designade specifikt för att överföra data mellan lager. Skiljer sig från domain models och kan ha annorlunda struktur optimerad för transport.

### ViewModels

Objekt som representerar data specifikt för en view/sida. Kombinerar data från flera sources och formaterar för presentation.

### Application Services

Services i application layer som orkestrerar use cases. Koordinerar domain objects, repositories och validering för att utföra affärsoperationer.

### Domain Services

Services i domain layer som innehåller affärslogik som inte naturligt hör hemma på en entity. Ren domänlogik utan infrastruktur-beroenden.

---

## Testing Concepts

### Unit Testing

Testning av individuella komponenter isolerat. Kräver mockade dependencies och fokuserar på en klass/metod åt gången.

### Testability

Hur lätt kod är att testa. Påverkas av loose coupling, dependency injection och hur väl ansvar är separerade.

### Mocking

Att ersätta riktiga dependencies med test-versioner. Möjliggör isolerad testning och kontroll över test-scenarios.

### Test Doubles

Generellt begrepp för mock-objekt, stubs, fakes etc. Används för att ersätta riktiga dependencies i tester.

---

## Anti-Patterns

### Primitive Obsession

Överanvändning av primitiva typer (string, int) istället för domänspecifika typer. Leder till duplicerad validering och svag type safety.

### God Object / God Class

Klass som vet för mycket eller gör för mycket. Bryter mot Single Responsibility Principle.

### Anemic Domain Model

Domain models utan beteende. All logik hamnar i services vilket bryter inkapsling och skapar procedural kod i OO-språk.

### Switch Statement Smell

Upprepade switch/if-kedjor på samma typ-check. Indikerar behov av polymorfism och bryter Open/Closed Principle.

---

## Architecture Principles

### Separation of Concerns

Olika delar av systemet hanterar olika aspekter. Varje lager/klass har sitt fokusområde.

### Defense in Depth

Flera lager av skydd/validering. Redundans som säkerhet snarare än duplicering.

### YAGNI (You Aren't Gonna Need It)

Implementera inte funktionalitet förrän den verkligen behövs. Motverkar over-engineering.

### Pragmatism vs Purism

Balans mellan att följa best practices och att göra praktiska kompromisser för att leverera värde.

### Future-Proofing

Designa system för att vara flexibla inför framtida förändringar. Balans mot YAGNI - inte över-designa men inte måla in sig i hörn.

---

## Domain-Specific Patterns

### Business Rules

Regler som styr affärslogik. I denna lösning: BR1 (sen rapportering), BR2 (lågt belopp), BR3 (reseskada deadline), BR5 (fordonskada registreringsnummer).

### Business Rule Violation

När en affärsregel bryts. Kan hanteras med exceptions, validation results eller flaggning beroende på kontext.

### Manual Review

Affärsprocess där claims flaggas för manuell granskning istället för automatisk hantering. Exempel på affärsregel-implementation.

---

## Infrastructure Patterns

### In-Memory Repository

Repository som lagrar data i minnet istället för databas. Användbart för utveckling, tester och prototyper.

### Database Migration

Process att flytta från en datakälla till en annan (t.ex. in-memory till SQL). God arkitektur gör detta enklare genom abstraktioner.

### Connection String

Konfigurationssträng som specificerar hur man ansluter till databas. Hanteras typiskt i appsettings.json.

---

## Modern Architecture Concepts

### Microservices

Arkitekturstil där system delas upp i små, självständiga services. Varje service har sin egen datastore och kommunicerar via API.

### API Layer

Separat lager för att exponera funktionalitet via HTTP/REST. Oberoende av presentation layer (Blazor).

### Cross-Cutting Concerns

Aspekter som berör hela systemet: logging, error handling, säkerhet. Måste hanteras konsekvent över lager.

---

## Code Quality Concepts

### DRY (Don't Repeat Yourself)

Princip att undvika duplicerad kod. Varje bit av kunskap ska ha en enda representation i systemet.

### Tight Coupling

När komponenter är starkt beroende av varandra. Gör ändringar svåra och testning komplicerad.

### Loose Coupling

När komponenter interagerar genom abstraktioner. Möjliggör oberoende ändringar och enkel testning.

### Technical Debt

Medvetet eller omedvetet designval som gör framtida arbete svårare. Måste balanseras mot leverans av värde.

### Refactoring

Omstrukturering av kod för att förbättra design utan att ändra funktionalitet. Kontinuerlig process för att hantera technical debt.

---

## Blazor-Specific Patterns

### Conditional Rendering

Visa olika UI baserat på villkor. I Blazor med `@if` statements.

### Component Composition

Bygga komplexa UI genom att komponera mindre komponenter. Följer separation of concerns i UI-lager.

### Form Binding

Koppla formulärfält till C#-properties. I Blazor med `@bind` directive för two-way binding.

### Event Handling

Reagera på användarinteraktion. I Blazor med `@onclick`, `@onchange` etc.
