# Requirements Specification: Skadeanmälningsapplikation

**Version:** 1.0  
**Datum:** Oktober 2025  
**Syfte:** Detta dokument specificerar VAD systemet ska göra

---

## Funktionella krav (User Stories)

### FR1: Hantera olika skadetyper

**Som** försäkringshandläggare  
**Vill jag** kunna ta emot tre olika typer av skadeanmälningar  
**Så att** kunder kan rapportera alla typer av skador vi täcker

**Acceptanskriterier:**

- Systemet ska kunna hantera **Fordonsskador** med fordonsspecifik information
- Systemet ska kunna hantera **Egendomsskador** med adress och skadekategori
- Systemet ska kunna hantera **Reseskador** med resmål och tidsperiod
- Varje skadetyp ska ha sina egna specifika datapunkter
- Alla skadetyper ska dela gemensam basinformation (datum, beskrivning)

**Datamodell (high-level):**

| Skadetyp    | Specifika datapunkter                          |
| ----------- | ---------------------------------------------- |
| **Fordon**  | Registreringsnummer, Datum, Plats              |
| **Egendom** | Adress, Typ av skada, Uppskattad kostnad       |
| **Resa**    | Resmål, Startdatum, Slutdatum, Typ av incident |

### FR2: Skapa skadeanmälan

**Som** kund  
**Vill jag** kunna skapa en ny skadeanmälan  
**Så att** jag kan rapportera en skada

**Acceptanskriterier:**

- Användaren kan välja skadetyp från dropdown
- Rätt formulär visas baserat på vald typ
- Validering av obligatoriska fält sker innan submit
- Bekräftelse visas efter lyckad skapelse
- Formuläret rensas efter submit

### FR3: Visa översikt och detaljer

**Som** försäkringshandläggare  
**Vill jag** kunna se alla inlämnade skadeanmälningar  
**Så att** jag kan få en översikt och hantera ärenden

**Acceptanskriterier:**

- **Lista-vy:** Visar alla skadeanmälningar med grundläggande information (typ, datum, ID)
- **Detaljvy:** Visar fullständig information för en specifik anmälan
- Listan uppdateras i realtid när nya skador rapporteras
- Användaren kan navigera från lista till detaljvy

---

## Affärsregler (Business Rules)

Systemet ska implementera minst tre av nedanstående affärsregler + minst en egen:

### BR1: Fordonsskada - Sen rapportering

**Regel:** Fordonsskador som rapporteras mer än 30 dagar efter skadetillfället kräver manuell granskning.

**Rationale:** Sent rapporterade skador kan innebära högre risk för bedrägeri eller att skadan har förvärrats.

**Beteende:**

- Skadan ska flaggas med "Kräver manuell granskning"
- Flaggan ska vara synlig för handläggare

---

### BR2: Egendomsskada - Högt skadeanspråk

**Regel:** Egendomsskador med uppskattad kostnad över 50,000 kr eskaleras automatiskt till senior-handläggare.

**Rationale:** Höga skadeanspråk kräver djupare granskning och auktorisation.

**Beteende:**

- Status sätts till "Eskalerad"
- Ska vara tydligt markerat i UI

---

### BR3: Reseskada - Rapporteringsfrist

**Regel:** Reseskador måste rapporteras inom 14 dagar efter hemkomst (slutdatum för resa).

**Rationale:** Sent rapporterade reseskador är svåra att verifiera och täcks inte av försäkringen.

**Beteende:**

- Om mer än 14 dagar: Avvisa med tydligt felmeddelande
- Förklara för användaren varför skadan inte kan tas emot

---

### BR4: Beskrivning - Kvalitetskrav

**Regel:** Om en beskrivning anges måste den vara minst 20 tecken lång.

**Rationale:** För korta beskrivningar saknar värde för utredning.

**Beteende:**

- Validering vid inskick
- Tydligt felmeddelande om kravet inte uppfylls

---

### BR5: Egen affärsregel (obligatorisk)

**Krav:** Systemet ska implementera minst en ytterligare verksamhetsspecifik affärsregel.

**Kriterier:**

- Regeln ska vara verksamhetsrelevant för försäkringsbranschen
- Den ska kräva koordination mellan flera datapunkter eller entities
- Regeln ska vara dokumenterad med verksamhetsmotivering

**Exempel på möjliga regler:**

- Fordonsskador på fordon äldre än 15 år: Maximal ersättning begränsas till 30,000 kr
- Flera skador på samma adress inom 6 månader: Flaggas för bedrägerikontroll
- Reseskador utan slutdatum: Kan inte godkännas förrän resan är avslutad

---

## Systemkrav (Constraints)

### SR1: Teknisk plattform

**Krav:**

- Systemet ska byggas med .NET 8 eller senare
- UI ska byggas med Blazor Server (inte WebAssembly)
- Data ska lagras in-memory (ingen extern databas)

---

### SR2: Modularitet

**Krav:**

- Systemet ska vara uppdelat i separata moduler/projekt enligt Clean Architecture
- Varje lager ska ha tydligt ansvar
- Beroenden ska peka inåt (mot domain)

---

### SR3: Interface-baserad design

**Krav:**

- Externa beroenden (data access, services) ska exponeras via interfaces
- Dependency Injection ska användas för att koppla ihop komponenter

---

## Icke-funktionella krav (Quality Attributes)

### NFR1: Prestanda

**Krav:**

- Applikationen ska starta på < 5 sekunder
- UI ska reagera utan märkbar fördröjning (< 200ms)
- Systemet ska hantera minst 100 skadeanmälningar utan prestandaproblem

**Mätmetod:** Manuell testning, subjektiv bedömning.

---

### NFR2: Användbarhet

**Krav:**

- Felmeddelanden ska vara tydliga och förklara vad användaren ska göra
- Användaren ska få bekräftelse efter varje lyckad operation
- Formulär ska förhindra submit om obligatoriska fält saknas
- UI ska vara responsivt och fungera på både desktop och tablet

**Acceptanskriterier:**

- En ny användare ska förstå hur man skapar en skadeanmälan utan instruktioner
- Felmeddelanden ska vara på svenska och tydligt förklara problemet

---

### NFR3: Underhållbarhet

**Krav:**

- Kod ska vara self-documenting med tydliga namn
- Komplexa affärsregler ska ha förklarande kommentarer
- Projektet ska ha en README med setup-instruktioner
- Arkitektoniska beslut ska vara dokumenterade

**Acceptanskriterier:**

- En ny utvecklare ska kunna sätta upp projektet och köra det på < 10 minuter
- Affärsreglernas placering och implementation ska vara lätt att förstå

---

## Valideringsregler (Input Constraints)

### Fordonsskador

| Fält                   | Obligatoriskt | Valideringsregel                             |
| ---------------------- | ------------- | -------------------------------------------- |
| Registreringsnummer    | Ja            | Format: ABC123 eller ABC12D                  |
| Skadetillfälle (datum) | Ja            | Får inte vara i framtiden, max 365 dagar bak |
| Plats                  | Nej           | -                                            |
| Beskrivning            | Nej           | Om angiven: minst 20 tecken (se BR4)         |

---

### Egendomsskador

| Fält               | Obligatoriskt | Valideringsregel                      |
| ------------------ | ------------- | ------------------------------------- |
| Adress             | Ja            | Minst 5 tecken                        |
| Typ av skada       | Ja            | En av: Brand, Vatten, Inbrott, Övrigt |
| Uppskattad kostnad | Nej           | Om angiven: måste vara > 0            |
| Beskrivning        | Nej           | Om angiven: minst 20 tecken           |

---

### Reseskador

| Fält            | Obligatoriskt | Valideringsregel                                             |
| --------------- | ------------- | ------------------------------------------------------------ |
| Resmål/land     | Ja            | Minst 2 tecken                                               |
| Startdatum      | Ja            | Får inte vara i framtiden                                    |
| Slutdatum       | Nej           | Om angiven: måste vara >= startdatum                         |
| Typ av incident | Ja            | En av: Försenad/Inställd, Medicinsk, Förlorat bagage, Övrigt |
| Beskrivning     | Nej           | Om angiven: minst 20 tecken                                  |

---

## Definition of Done

Ett requirement är uppfyllt när:

✅ **Funktionalitet:** Acceptanskriterier är implementerade och verifierade  
✅ **Kvalitet:** Systemet uppfyller icke-funktionella krav (prestanda, användbarhet)  
✅ **Validering:** Alla valideringsregler implementerade och testade  
✅ **Affärsregler:** Minst 3 standardregler + 1 verksamhetsspecifik regel implementerade korrekt  
✅ **Testbarhet:** Alla testscenarier kan genomföras utan fel  
✅ **Dokumentation:** Systemdokumentation beskriver hur kraven uppfylls

---

## Prioritering (MoSCoW)

### Must Have (Kärnfunktionalitet)

- ✅ FR1: Hantera alla tre skadetyper (Fordon, Egendom, Resa)
- ✅ FR2: Skapa skadeanmälan med validering
- ✅ FR3: Visa lista + detaljvy
- ✅ BR1-BR4: Implementera minst 3 affärsregler + 1 egen
- ✅ SR1-SR3: Uppfylla alla systemkrav
- ✅ NFR1-NFR3: Uppfylla alla icke-funktionella krav
- ✅ Alla valideringsregler implementerade

### Should Have (Viktiga tillägg)

- Sortering av lista (datum, typ)
- Filterering/sökning baserat på skadetyp eller datum
- Felhantering med användarvänliga meddelanden
- Responsiv design (fungerar på mobil)

### Could Have (Nice-to-have)

- Redigera befintlig skadeanmälan
- Ta bort skadeanmälan
- Export till CSV/PDF
- Statistik-vy (antal per typ, genomsnittlig kostnad)
- Paginering av lista

### Won't Have (Utanför scope)

- Autentisering/användarhantering
- Extern databas (in-memory är OK)
- Email-notifikationer
- Automatiserade tester (valfri fördjupning)

---

## Testscenarier (Acceptance Testing)

### Scenario 1: Skapa fordonsskada (Happy Path)

**Förkrav:** Användaren är på startsidan

**Steg:**

1. Navigera till "Skapa skadeanmälan"
2. Välj "Fordon" från skadetyp-dropdown
3. Fyll i registreringsnummer: `ABC123`
4. Välj datum: dagens datum
5. Fyll i plats: "Stockholm City"
6. Fyll i beskrivning: "Kollision med annat fordon vid parkering på Drottninggatan"
7. Klicka "Skapa skadeanmälan"

**Förväntat resultat:**

- ✅ Bekräftelsemeddelande visas
- ✅ Formulär rensas/återställs
- ✅ Ny skada finns i listan

---

### Scenario 2: Validering - Gammal fordonsskada (BR1)

**Förkrav:** Användaren är på formuläret

**Steg:**

1. Välj "Fordon"
2. Välj datum: 40 dagar tillbaka i tiden
3. Fyll i registreringsnummer: `XYZ789`
4. Fyll i övriga obligatoriska fält
5. Klicka submit

**Förväntat resultat:**

- ✅ Skadan skapas
- ✅ Skadan flaggas med "Kräver manuell granskning"
- ✅ Flaggan är synlig i lista eller detaljvy

---

### Scenario 3: Validering - Hög egendomsskada (BR2)

**Förkrav:** Användaren skapar egendomsskada

**Steg:**

1. Välj "Egendom"
2. Fyll i adress: "Storgatan 1, Stockholm"
3. Välj typ: "Brand"
4. Fyll i uppskattad kostnad: `75000`
5. Submit

**Förväntat resultat:**

- ✅ Skadan skapas
- ✅ Status sätts till "Eskalerad"
- ✅ Markering är synlig i UI

---

### Scenario 4: Validering - För sen reseskada (BR3)

**Förkrav:** Användaren ska testa rapporteringsfristen

**Steg:**

1. Välj "Resa"
2. Fyll i resmål: "Thailand"
3. Välj startdatum: 20 dagar tillbaka
4. Välj slutdatum: 18 dagar tillbaka
5. Välj typ: "Medicinsk"
6. Submit

**Förväntat resultat:**

- ❌ Skadan avvisas
- ✅ Tydligt felmeddelande: "Reseskador måste rapporteras inom 14 dagar efter hemkomst"

---

### Scenario 5: Conditional rendering

**Förkrav:** Användaren är på formuläret

**Steg:**

1. Välj "Egendom" från dropdown
2. **Observera:** Adress-fält, Typ av skada, Uppskattad kostnad visas
3. Byt till "Resa"
4. **Observera:** Resmål, Start/Slutdatum, Typ av incident visas
5. **Observera:** Egendomsfält döljs

**Förväntat resultat:**

- ✅ Endast relevanta fält visas för vald skadetyp
- ✅ Ingen duplicering av gemensamma fält
- ✅ Formuläret reagerar direkt vid byte av typ

---

### Scenario 6: Visa lista och detaljvy

**Förkrav:** Minst 3 skador av olika typer finns i systemet

**Steg:**

1. Navigera till lista-vyn
2. **Observera:** Alla skador visas med typ, datum, ID
3. Klicka på en specifik skada
4. **Observera:** Detaljvy öppnas

**Förväntat resultat:**

- ✅ Lista visar alla skador
- ✅ Listan är läsbar och strukturerad
- ✅ Detaljvy visar all information för den valda skadan
- ✅ Användaren kan navigera tillbaka till listan

---

### Scenario 7: Validering - För kort beskrivning (BR4)

**Förkrav:** BR4 är implementerad

**Steg:**

1. Skapa valfri skadetyp
2. Fyll i beskrivning: "För kort" (10 tecken)
3. Submit

**Förväntat resultat:**

- ❌ Submit förhindras eller felmeddelande visas
- ✅ Meddelande: "Beskrivning måste vara minst 20 tecken"

---

## Sammanfattning av requirements

| Kategori                | Antal krav | Status      |
| ----------------------- | ---------- | ----------- |
| Funktionella krav (FR)  | 3          | Mandatory   |
| Affärsregler (BR)       | 3 + 1 egen | Mandatory   |
| Systemkrav (SR)         | 3          | Mandatory   |
| Icke-funktionella (NFR) | 3          | Mandatory   |
| Valideringsregler       | 15+        | Mandatory   |
| Testscenarier           | 7          | Verifiering |
