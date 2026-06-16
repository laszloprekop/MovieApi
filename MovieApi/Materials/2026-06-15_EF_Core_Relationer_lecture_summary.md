# Lecture Summary — Entity Framework Core: Relationships (Code First)

**Course:** Lexicon LTU VT-2026 — Fullstack .NET Developer
**Date:** 2026-06-15 (Monday) · 10:24–12:19 (1h 55m)
**Instructor:** Michael Svensson
**Topic:** Modelling relationships (1:1, 1:M, N:M) in EF Core using conventions and Code First; introduction to the first large exercise (Övning 3 — Movie API)
**Reference links given:** `learn.microsoft.com/en-us/ef/core/modeling/` and `learnentityframeworkcore.com/configuration/fluent-api`

> **How to read this document.** It is organised as an importance pyramid: (1) the one-paragraph takeaway, (2) what you must actually *do* (the exercise, fully stepped out), (3) the conceptual core of the lecture, (4) the Q&A with corrections, and (5) a glossary. A *Corrections & nuances* callout is woven in wherever the live session simplified or slightly mis-stated something, so you get the full picture, including the blind spots. The lecture was Swedish; technical terms in the auto-captions were heavily garbled and have been reconstructed.

---

## 1. The one-paragraph takeaway

If you model your C# classes following EF Core's **conventions** — an `Id` (or `<ClassName>Id`) primary key, a foreign-key property named `<NavigationName>Id`, and **navigation properties** linking the classes — then EF Core discovers and configures most relationships for you automatically, including the join tables for many-to-many. You only need `OnModelCreating` / the **Fluent API** for the special cases conventions can't express (composite keys, extra columns on a join table, non-standard names, etc.). The rest of the lecture walked through three relationship families (Customer–Order–Product, Student–Class–Teacher, Book–Category–Author) to show the same pattern repeating, and ended by handing out the first substantial exercise: build a **Movie API** that exercises 1:1, 1:M and N:M relationships end to end.

---

## 2. What you must do — Exercise 3: Movie API (the afternoon's work)

This is the **first larger assignment** and is built directly on today's material. Michael set **no hard deadline** but expects you to start in the afternoon. The green-marked endpoints in the PDF are mandatory; everything else is optional practice. Work **one relationship at a time**: add it, create a migration, update the database, then *read the generated migration carefully* to confirm it does what you intended.

> **Version note (discrepancy worth catching):** the PDF says ".NET 9", but in the session Michael said use **.NET 8, 9 or 10 — not 6 or 7** — and recommended doing it on **.NET 10** first, then optionally redoing on .NET 8. Either supported version is fine.

> **PDF artifact:** Michael flagged a stray bullet reading "NAND" in the converted PDF that should be ignored/removed — a leftover from the PDF→Word conversion, not an instruction.

### Part 1 — Create the project
1. New **ASP.NET Core Web API** (.NET 8/9/10).
2. Name it **`MovieApi`**.
3. Project options: **Authentication type: None**, tick **Use controllers**, tick **Enable OpenAPI support**.

### Part 2 — Models and relationships
Create the entities in a **`Models`** folder. Build **one relationship at a time → migration → update-database → inspect the migration**.

- **`Movie`** — `Id`, `Title`, `Year`, `Genre`, `Duration`
  - Relationships: **1:1** with `MovieDetails`, **1:M** with `Review`, **N:M** with `Actor` via `MovieActor`.
  - *(Optional now, otherwise required later:)* normalise `Genre` out into its own table.
- **`MovieDetails`** — `Id`, `Synopsis`, `Language`, `Budget`
- **`Review`** — `Id`, `ReviewerName`, `Comment`, `Rating` (1–5)
- **`Actor`** — `Id`, `Name`, `BirthYear`
- **`MovieActor`** — `MovieId`, `ActorId` *(EF Core creates this join table automatically for a plain N:M — see the nuance below)*

> **Nuance — when the N:M join is "automatic" vs. when it isn't.** A *pure* many-to-many (just two foreign keys, no extra data) lets EF Core create and manage the join table for you with only collection navigations on each side. The moment you need an **extra column on the join** (Part 8 adds `Role`), `MovieActor` must become a **fully-modelled entity** with its own `DbSet`, an explicit (usually **composite**) key, and explicit relationships — conventions can't carry payload on an implicit join. So treat the "automagic" join as a starting point that you graduate away from in Part 8.

### Part 3 — Scaffold the Movies controller
1. **`git commit` before scaffolding** (so you can diff/revert the generated code).
2. In Solution Explorer: **Controllers → right-click → Add → New Scaffolded Item**.
3. Choose **API Controller with actions, using Entity Framework**.
4. Pick **`Movie`** as the model, and create a **`MovieContext`** via the **+** button.

> **Reminder from a previous lecture:** scaffolding reads the *compiled assembly*, not your source files. If scaffolding throws a confusing error after you just edited a model, do a **clean rebuild first** — the error is often a stale-build artifact.

### Part 3.5 — Seed data (after scaffold + migration)
1. Create an **`Extensions`** folder.
2. In it, write an **extension method `SeedData`**.
3. Call **`app.SeedData();`** in `Program.cs`.
4. Write the seed so **all** entities (movies, details, reviews, actors, movie-actor links) hold relevant data.
5. Test with **Postman**.

> Michael said this part can **wait until Tuesday** if you don't reach it today. **Pagination ("sidning") is *not* covered yet** — he will teach it tomorrow, including how to avoid filling the database manually.

### Part 4 — DTOs and the detailed view
Create a **`DTOs`** folder containing:
- **`MovieCreateDto`** (for POST) — with `[Required]`, `[Range]`, etc.
- **`MovieUpdateDto`** (for PUT)
- **`MovieDto`** — a summary view
- **`MovieDetailDto`** — movie data **+** `MovieDetails` **+** `List<ReviewDto>` **+** `List<ActorDto>`
- **`ReviewDto`**, **`ActorDto`**

> AutoMapper is **optional**; you may map manually or write your own mapping logic.

### Part 5 — Required endpoints (green = mandatory; skip repetitive ones if you wish)
- **MoviesController:** `GET /api/movies` · `GET /api/movies/{id}` · `GET /api/movies/{id}/details` · `POST /api/movies` · `PUT /api/movies/{id}` · `DELETE /api/movies/{id}`
- **ActorsController:** `GET /api/actors` · `GET /api/actors/{id}` · `POST /api/actors` · `PUT /api/actors/{id}` · `POST /api/movies/{movieId}/actors/{actorId}` (add actor to a movie)
- **ReviewsController:** `GET /api/movies/{movieId}/reviews` · `POST /api/movies/{movieId}/reviews` · `DELETE /api/reviews/{id}`
- **Optional filtering** via query strings, e.g. `GET /api/movies?genre=Drama&year=2022`, `GET /api/movies?actor=Tom+Hanks` (the `+` is URL-encoded space), or `GET /api/movies/{id}?withactors=true`.

### Part 6 — The detailed endpoint
1. Implement `GET /api/movies/{id}/details`.
2. Return a `MovieDetailDto` containing the movie's details, actors and reviews.
3. **Use LINQ and `Select`.**

> **Performance principle (you've verified this before, and it matters here):** put the `Select` projection **inside the LINQ query, before `ToListAsync()`**. EF then translates the projection into SQL — it fetches only the columns you need and skips change tracking. Put `Select` **after** materialising and EF first loads full entities into memory, then maps them client-side. For a "details" endpoint that fans out into reviews and actors, projecting in the query is the meaningful difference. Confirm by comparing the **logged SQL**.

### Part 7 — Validation and status codes
1. Put **`[ApiController]`** on each controller — it gives automatic model validation from `[Required]`/`[Range]` and an automatic **400 Bad Request** on invalid models.
2. Use **separate DTOs** for POST vs PUT so the rules can differ.
3. Check **manually for 404 Not Found** when a resource is missing (e.g. on `GET {id}`).
4. Return the right codes: **200 OK** (read), **201 Created** (created), **400 Bad Request** (validation), **404 Not Found** (missing id), **204 No Content** (successful delete).

### Part 8 — Extra challenges (optional)
1. **Give the join table payload.** Add a `Role` field to `MovieActor`, making it a first-class entity (`MovieId`, `ActorId`, `Role`). In `OnModelCreating` (in `MovieContext`), use the **Fluent API** to (a) define the **composite key** and (b) configure the two relationships.
   - *Note:* a **composite key cannot be configured by convention** in EF Core — Fluent API (`HasKey(ma => new { ma.MovieId, ma.ActorId })`) is the standard way. This is exactly the "special case" Michael said `OnModelCreating` exists for.
2. **Dedicated endpoint:** `POST /api/movies/{movieId}/actors` taking a `MovieActorCreateDto` (`{ "actorId": 4, "role": "Main antagonist" }`). `movieId` comes from the route; `actorId` and `role` from the body. Create the `MovieActorCreateDto` for this.
3. **LINQ report endpoints** (group in a separate `ReportsController` for separation of concerns): top-5-per-genre by rating, average ratings per genre, most-active actors, longest film per country, film with the most reviews, most-common genres. *Tip:* use **`SelectMany`** to flatten collections (e.g. all `MovieActors` across many movies into one list).
4. **More relationships:** `Movie ↔ Director` (M:1), `Movie ↔ Genre` (N:M via `MovieGenre`), `Movie ↔ Country` (M:1). Create `Director`, `Genre`, `Country` and the `MovieGenre` join, configure with Fluent API, and **make a separate migration after each major change**.

### Bonus — EF Core Power Tools (Michael agreed to demo it)
After Adam asked for a demo, Michael said he'd show it. Install via **Visual Studio → Extensions → Manage Extensions → search "EF Core Power Tools" → Install** (close VS to let it install). Then **Visual Studio Installer → Modify → Individual components → DGML editor → install**. Right-click the project → **EF Core Power Tools → Add DbContext Diagram** to get a visual diagram of all classes exposed as `DbSet` (and those referenced from them).

> This tool is **Visual Studio-only**. On **JetBrains Rider** (your primary IDE) it isn't available; Rider has its own database/diagram tooling, or you can generate a diagram from the model another way. Worth noting since you've moved to Rider.

> Adam asked whether he could **reuse an existing project** that already has much of this rather than start fresh — Michael said **yes, that's fine**.

---

## 3. The conceptual core of the lecture

### 3.1 Why relationships, and why now
So far the class has built trivial databases of one or two tables. A real Web API may have dozens of tables, and the **group final project** will too. To get there you must model relationships correctly. Michael's framing: some groups may be comfortable enough to design the schema in the database first, but the class will work **Code First** — define C# model classes and let EF Core build the database.

> **Terminology correction.** Michael said "model first or code first as it's now called." In EF Core the two approaches are **Code First** (classes → database) and **Database First** (existing database → scaffolded classes). The old EF6 "**Model First**" (visual EDMX designer) **does not exist in EF Core**; the modern pairing is Code First vs Database First.

### 3.2 The "four magic packages" (repetition)
For an ASP.NET Core Web API with EF Core against **SQL Server**, Michael's mnemonic "four magic packages":
1. `Microsoft.EntityFrameworkCore`
2. `Microsoft.EntityFrameworkCore.SqlServer`
3. `Microsoft.EntityFrameworkCore.Design`
4. `Microsoft.EntityFrameworkCore.Tools` — needed for the **Package Manager Console** commands `Add-Migration`, `Update-Database`.

Install in Visual Studio with `Install-Package …`, or via CLI with `dotnet add package …`. A class poll showed **~10–11 students use the Package Manager Console**; the rest use the `dotnet` CLI. Michael's point: know **both** ways, because you can't predict what a future employer uses.

> **Nuances on the packages.**
> - `…SqlServer` already depends on `…EntityFrameworkCore`, so installing SqlServer pulls Core in transitively. Installing Core explicitly is harmless but technically redundant.
> - `…Design` is the package that actually enables migrations at design time — needed by **both** PMC and CLI.
> - `…Tools` provides the **PMC cmdlets only**. If you use the **`dotnet ef` CLI** (which the majority do), you don't need `…Tools`; instead you need the **`dotnet-ef` global tool** (`dotnet tool install --global dotnet-ef`), which is often already in the .NET SDK. So the "four" is really the *Visual Studio / PMC* set; CLI users have a slightly different fourth piece.

### 3.3 Conventions do the heavy lifting
EF Core ships with **conventions** so most configuration is implicit. The key ones demonstrated:
- A property named **`Id`** (or **`<ClassName>Id`**) becomes the **primary key**.
- A property named **`<NavigationName>Id`** (e.g. `CustomerId` on `Order`) is recognised as the **foreign key**, *provided its type is compatible with the principal's key* (same type, or a nullable version of it).
- A **navigation property** (`public Customer Customer { get; set; }` and/or `public ICollection<Order> Orders { get; set; }`) tells EF the two classes are related and in which direction/multiplicity.

If your classes follow these, EF Core **automatically discovers and configures** one-to-one and one-to-many relationships and **creates join tables** for many-to-many (with auto names like `BookAuthors`, `StudentClasses`). You only write `OnModelCreating` / Fluent API for the cases conventions can't express. Michael stressed: **don't assume you must hand-write `OnModelCreating`** — that's a common misconception.

The exact FK-discovery naming rules (from the Microsoft docs Edvin Sanne posted in chat) are: `<navigation name><principal key name>`, `<navigation name>Id`, `<principal entity type name><principal key name>`, or `<principal entity type name>Id`.

### 3.4 The three worked examples (same pattern, three times)
Michael built these live in Visual Studio to show the repetition:

1. **Customer – Order – OrderItem – Product**
   - `Customer` 1—M `Order` (a customer places many orders).
   - `Order` 1—M `OrderItem` (an order has many line items).
   - `Product` 1—M `OrderItem` (a product appears in many line items).
   - `OrderItem` carries `Quantity`, `Price`, plus FKs `OrderId` and `ProductId`. The recurring sign of a "many" side is **`ICollection<…>`** on the parent.
   - *Note:* `OrderItem` here is a **join entity with payload** (quantity, price) — i.e. exactly the "graduated" N:M from Part 8, not a pure auto-join.

2. **Student – StudentClass – Class – Teacher**
   - `StudentClass` is the **join between Student and Class that also stores the grade ("betyg")** for that student in that class. Its primary key links a specific student to a specific class (composite), and the grade rides along.
   - `Teacher` (Id, name, email, specialisation) links 1—M (or M:N via Class) to the classes they teach.
   - Edvin Sanne flagged that the name `StudentClass` is confusing — it sounds like "a class full of students" rather than "one student's record in one class." Michael agreed clearer names are fine, with the caveat that longer entity names produce longer relationship/FK names.

3. **Book – BookCategory – Category (and Author)**
   - `Book` (Id, Title, ISBN, published date, price, stock).
   - `BookCategory` joins `Book` and `Category` (BookId + CategoryId).
   - `Category` (Id, Name, Description).
   - Authors would be added similarly (the lecture trailed off before finishing the Author side).

> Michael floated a possible future exercise: **re-model the recurring Garage project as relational classes** (vehicle categories, car details, etc.) to visualise it in a Web API.

### 3.5 Migrations as snapshots
`Add-Migration` produces a **snapshot** of the current model (the `ModelSnapshot.cs` file) plus an `Up`/`Down` pair; it does **not** touch the database. `Update-Database` applies it. The first migration is conventionally named **`Init`**. Each subsequent model change → new migration → new snapshot, all chained to your database. The snapshot is the **reference point**: delete it and then change the database, and EF has nothing to diff against. (Michael said he'd cover removing migrations and dropping the database tomorrow.)

---

## 4. Questions & answers (with corrections)

### Teacher → class
- **"Which packages do you need to build a .NET Core Web API with EF Core — without looking at my notes?"** — A recall check for the four packages above; the class answered collectively.
- **"How many of you use Package Manager Console in Visual Studio?"** — ~10–11 hands; the rest use the `dotnet` CLI.
- **"Does the exercise feel overwhelming?"** (to Christofer) — Christofer asked instead when it must be done; Michael set **no deadline** and advised starting from Part 1 and ticking off parts as "done / not done" (his personal plus/minus tracking habit, also a motivation tool).

### Students → teacher (spoken)
- **Bahador Nezakati:** *"Where did we say it means `CustomerId`? Just because it's prefixed with `Customer`?"* → Michael: it's the **`Id` + class-name conventions**; EF reads the **property/class names**. (Correct.)
- **Dragos Cuciureanu:** *"Do you need to write `public int CustomerId` yourself, or is just the `Customer` navigation property enough — does `CustomerId` get created automatically?"* → Michael: *"You don't even need to write it."*
  > **Clarification (the live answer was a bit muddled):** Dragos is right. With **only** the navigation property, EF Core creates a hidden **shadow foreign-key property** (`CustomerId`) in the database — you don't have to declare it. Most developers **do** declare the explicit FK property anyway, because it lets you read/set the foreign key in code without loading the navigation. So: not required, but commonly added on purpose.
- **Lars Karlqvist:** *"Is `Id` always chosen as the primary key if it exists? Can you control it with data annotations?"* → Michael: by convention yes; you can be explicit with the **`[Key]`** annotation, and **Fluent API** handles more complex cases.
  > **Note:** `[Key]` is **redundant** when the property is simply named `Id` (or `<Type>Id`) — the convention already makes it the PK. `[Key]` earns its keep on a *non-conventionally-named* single key. For **composite** keys you cannot use `[Key]` alone — use Fluent API `HasKey(…)`.
- **Amer Mauweyah:** *"I've seen navigation properties declared `public virtual Customer Customer` — does `virtual` do something?"* → Michael: that's **old (EF 5/6) style**.
  > **Clarification:** `virtual` enabled **lazy loading via proxies** in EF6. In **EF Core, navigation properties don't need `virtual`** unless you explicitly opt into lazy-loading proxies (`Microsoft.EntityFrameworkCore.Proxies` + `UseLazyLoadingProxies()` + `virtual` navigations). Without that, omit `virtual`.
- **Lars Karlqvist:** *"Many small tables vs one big table — is one slower to search? Any speed advantage to splitting?"* → Michael: with few rows it barely matters now; large tables are split, you **index** selected columns, and you **`SELECT` only the columns you need** rather than `SELECT *`. A good pattern is a **main table + a 1:1 details table** for fields you rarely query.
  > **Fuller picture (trade-off, not covered):** normalisation's main payoff is **avoiding duplicated data and update anomalies** (Peter Broman made this point and Michael agreed). The cost is that more tables mean more **JOINs**, which can slow *reads*. So it's a normalisation-vs-denormalisation trade-off, tuned with indexing and projection — not simply "more tables = faster."
- **Adam Matthews:** *"Can I reuse my existing project instead of a new one?"* → **Yes.**
- **Adam Matthews:** *"Can you demo Power Tools?"* → Michael agreed to demo it.

### Student chat (Teams) — questions and the answers that emerged there
- **Jonatan Streith:** *"How does it interpret names like `Idea` or `CustomerIdeologicalBeliefs` — does it still pull out the `Id` bit and treat it as the key?"*
  - **Alexander Stauch:** guessed no — *"it probably checks specifically against the `<class-name>Id` pattern."*
  - **Bahador Nezakati:** joked *"luckily you're not allowed to save such things."*
  > **Correct answer:** Alexander is essentially right. The PK convention matches a property named **exactly `Id`** or **exactly `<ClassName>Id`** — it does **not** substring-search for "Id" inside a longer word. So `Idea` and `CustomerIdeologicalBeliefs` are **not** treated as keys. EF does pattern-matching on the whole name, not a fuzzy contains-"Id".
- **Edvin Sanne** then posted the Microsoft Learn docs ("Conventions for relationship discovery") and quoted the exact FK-discovery naming rules (see §3.3).
- **Amos Persson:** noted that auto-generated keys not declared in your model are **harder to reach from code** — which is the practical reason to declare the explicit FK property (echoing the Dragos clarification above).
- **Christofer Nyström** (important, *not answered live*): *"In plain SQL you usually only put the link in the join table (e.g. `OrderItems`) for an N:M relation. Here you also have links from both `Order` and `Item` back to `OrderItems`, and `OrderItems` links back. So isn't it enough for the relationship to live in `OrderItems` in EF?"*
  - **Jonatan Streith:** *"Sounds more like a convention than a rule."*
  - **Martin Leo:** *"Maybe EF Core needs it this way; `StudentClass` becomes a join table in the database."*
  > **Correct answer:** Christofer's instinct is right. The relationship is **defined by the foreign keys in the join entity**. The **collection navigations on both parents are optional conveniences** for traversing the graph in code — EF Core does **not** require navigations on both sides. You can configure a relationship with a navigation on one side, both sides, or (via Fluent API) neither. So the FK in `OrderItem`/`StudentClass` is what matters; the back-references are for your code's convenience, not an EF requirement. (Also: because `OrderItem`/`StudentClass` carry extra columns, they're proper entities, not pure auto-joins.)
- **On the "one class per file" tangent:** Edvin Sanne preferred putting `StudentClass` in the same file as `Student`. **Amos Persson** said multiple classes per file is a strong "no-no" in C# (with a possible exception for a one-line `record`). Edvin asked whether this has a **direct effect or is just convention**. Amos answered (chat): it's a **convention, not compiler-enforced** — recommended because classes grow over time, and you don't want to be splitting files out of legacy code later. **Peter Broman** added these are conventions/ideas someone pushed, and "isn't necessarily right." (Technically: the C# compiler allows many types per file; one-type-per-file is a maintainability/style norm.)

---

## 5. Other things worth knowing about this session

- **Audio quality was poor**, especially for Peter Broman (multiple students said so in chat). Some of Peter's spoken points came through only partially; his chat messages are the more reliable record of his contributions.
- **A long off-topic stretch (~00:54–01:13):** while Michael was occupied (screen-share handling and converting the exercise PDF to Word), students chatted about running **local LLMs** (Ollama, Mistral), GPU/RAM for inference, crypto-mining, and various **Claude models** (the "Fable"/"Mythos" chatter). This is informal banter, not course content — treat any product specifics there as unverified hearsay rather than fact. Michael returned at ~01:12 and resumed with the exercise.
- **Course logistics mentioned:** Michael is compiling the class's self-assessments of "what I need to feel ready for a developer role," and may add a module (e.g. **Docker** was a common request) at some point. He also noted that course materials are always uploaded as **both PDF and Word** so they can be edited in future cohorts.

---

## 6. Glossary (newly used / reinforced terms)

| Term | Meaning in this context |
|---|---|
| **Convention (EF Core)** | A built-in default rule (naming, types) that lets EF configure the model without explicit code. Following conventions is how you avoid hand-writing configuration. |
| **Code First** | Approach where C# classes are the source of truth and the database is generated from them (via migrations). |
| **Database First** | Approach where an existing database is reverse-engineered into C# classes. (EF Core's pairing with Code First; the old EF6 "Model First"/EDMX designer is **not** in EF Core.) |
| **Navigation property** | A property on an entity that references a related entity (`public Customer Customer`) or a collection of them (`public ICollection<Order> Orders`). Tells EF the relationship exists and its direction. |
| **Foreign key (FK) property** | A scalar property (e.g. `CustomerId`) holding the key value of the related (principal) entity. By convention named `<NavigationName>Id` / `<PrincipalType>Id`. |
| **Shadow property** | A property EF maintains in the model/database but that is **not** declared in your C# class (e.g. an undeclared `CustomerId` when only the navigation exists). |
| **Principal / dependent** | In a relationship, the principal owns the key being referenced; the dependent holds the foreign key. (E.g. `Customer` principal, `Order` dependent.) |
| **One-to-one (1:1)** | E.g. `Movie`↔`MovieDetails`; often used to split rarely-queried fields into a separate "details" table. |
| **One-to-many (1:M)** | E.g. one `Customer` has many `Order`s; modelled with `ICollection<…>` on the "one" side and an FK on the "many" side. |
| **Many-to-many (N:M)** | E.g. `Movie`↔`Actor`; resolved through a **join table**. Pure N:M can be auto-managed by EF; with extra columns it must be a modelled **join entity**. |
| **Join entity / join table** | The table linking two sides of an N:M (e.g. `MovieActor`, `StudentClass`, `OrderItem`). Carries the two FKs and, optionally, payload columns. |
| **Composite key** | A primary key spanning multiple columns (e.g. `MovieId` + `ActorId`). **Must** be configured with Fluent API `HasKey(...)`; conventions/`[Key]` alone can't do it. |
| **`OnModelCreating`** | The `DbContext` method where you place explicit configuration (Fluent API) for cases conventions don't cover. |
| **Fluent API** | Method-chaining configuration inside `OnModelCreating` (e.g. `HasKey`, `HasOne`, `WithMany`) — more powerful than data annotations. |
| **Data annotation** | Attribute on a property/class (e.g. `[Key]`, `[Required]`, `[Range]`) used for simpler configuration and validation. |
| **Migration** | A generated `Up`/`Down` change set plus a model **snapshot**, recording how the schema should evolve. `Add-Migration` creates it; `Update-Database` applies it. |
| **Snapshot (`ModelSnapshot.cs`)** | EF's stored picture of the current model; the reference point each new migration diffs against. |
| **Scaffolding** | Auto-generating a controller (or model classes) from an entity + `DbContext`. Reads the **compiled assembly**, so clean-rebuild before scaffolding if you see stale errors. |
| **DTO (Data Transfer Object)** | A shape used for API input/output, distinct from the entity — e.g. separate `…CreateDto`/`…UpdateDto`/`…Dto` to apply different rules and avoid over-exposing the model. |
| **`[ApiController]`** | Controller attribute enabling automatic model validation and automatic 400 responses on invalid input. |
| **Projection (`Select` in LINQ)** | Mapping query results to a shape. Placed **before** `ToListAsync()` it's translated to SQL (column-selective, no tracking); **after**, it runs in memory on fully-loaded entities. |
| **`SelectMany`** | LINQ operator that flattens nested collections (e.g. all `MovieActors` across many movies into one list). |
| **EF Core Power Tools** | Visual Studio extension; "Add DbContext Diagram" visualises your entities. VS-only (not Rider). |
| **`virtual` navigation** | EF6-era marker enabling lazy-loading proxies; **not needed in EF Core** unless you opt into proxy lazy loading. |
| **Lazy loading** | Loading related data on first access. In EF Core it's opt-in (proxies + `virtual`), unlike EF6 where it was on by default. |
