# Reflektion: Modulär Skadeanmälningsapplikation

**Namn:**  
**Datum:**  
**GitHub-repo:**

---

## Syfte med denna reflektion

Vi går igenom alla lösningar tillsammans under uppföljningen, men den djupaste lärdomen sker när du själv artikulerar dina tankar. Dessa frågor är tänkta som förberedelse för diskussionen - och för din egen förståelse.

---

## 1. Arkitektoniska val

**Hur designade du domain-modellerna och varför?**

_Arv? Composition? Rich models eller anemic? Ge ett konkret exempel och resonera kring trade-offs._

[ Ditt svar här ]

---

**Var placerade du affärsreglerna?**

_I domain entities? I service? Båda? Beskriv en affärsregel och motivera var den ligger._

[ Ditt svar här ]

---

**Beskriv ett arkitektoniskt beslut du är nöjd med - och ett du skulle ändra.**

_Vad funkade bra? Vad blev "code smell"? Vad lärde du dig?_

**Nöjd med:**  
[ Ditt svar här ]

**Skulle ändra:**  
[ Ditt svar här ]

---

## 2. SOLID & Design Patterns

**Välj 2-3 SOLID-principer och ge konkreta exempel från din kod.**

_Följer du principen? Bryter du mot den? Varför blev det så? Inget svar är "fel" - vi vill höra ditt resonemang._

[ Ditt svar här ]

---

**Hur enkelt är det att lägga till en 4:e skadetyp?**

_Vilka filer behöver ändras? Vilka kan förbli oförändrade? Är systemet "open for extension"?_

[ Ditt svar här ]

---

## 3. Dependency Injection & Interfaces

**Motivera dina DI lifestyle-val (Scoped/Singleton/Transient).**

[ Din motivering här ]

---

**Hur många interfaces skapade du? Varför just det antalet?**

_Generisk IRepository<T>? Specifika per entity? Inget interface alls? Resonera kring för mycket vs för lite abstraktion._

[ Ditt svar här ]

---

## 4. Blazor & UI

**Hur löste du conditional rendering och state management?**

_Beskriv din approach. Vad funkade bra? Vad blev krångligt?_

[ Ditt svar här ]

---

## 5. Lärdomar & Transferability

**Vad är den viktigaste insikten från denna övning?**

_Något som utmanade dina tidigare antaganden? Något du kommer tänka på annorlunda framöver?_

[ Ditt svar här ]

**Om du började om från scratch imorgon - vad skulle du göra annorlunda?**

_Arkitektur? Approach? Något du lärde dig för sent?_

[ Ditt svar här ]

---

## 6. Testbarhet & Underhållbarhet

**Hur testbar är din lösning?**

_Teoretiskt - hur enkelt skulle det vara att skriva unit tests för affärsreglerna? Vad är enkelt? Vad är svårt?_

[ Ditt svar här ]

---

**Future-proofing: Hur enkelt är det att byta till SQL?**

_Testa din hypotes: vilka filer behöver ändras? Är din arkitektur flexibel eller tight coupled till in-memory?_

[ Ditt svar här ]

---

## 7. En sista reflektion

**Om du bara skulle ta med dig EN sak från denna övning till ditt nästa projekt - vad skulle det vara?**

[ Ditt svar här ]

---
