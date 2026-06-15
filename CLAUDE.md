# CLAUDE.md

Guidance for Claude Code (and other AI assistants) working in this repository.

## What this is

CDP4-COMET BatchEditor Community Edition: a **.NET 8 command-line tool** that performs
fast, bulk administrative operations on an ECSS-E-TM-10-25 (Annex A / Annex C)
`EngineeringModel`, served over the CDP4-COMET web services. It is published as a global
dotnet tool (`cdp4-comet-be`, PackageId `cdp4-comet-be`).

The tool connects to a data source, resolves the requested action against the model, applies
changes through the CDP4-COMET SDK session, and writes them back. It can optionally emit a CSV
report of affected parameters.

## Solution layout

- `CDP-Batch-Editor.sln` — the solution.
- `CDPBatchEditor/` — the application (the only shipped project).
- `CDPBatchEditor.Tests/` — NUnit test suite (Moq for mocking, coverlet for coverage).

### Key directories in `CDPBatchEditor/`

- `Program.cs` — entry point. Parses args with `CommandLine` (CommandLineParser), builds the
  Autofac container, resolves `IApp`, runs, then stops (closes/saves the session).
- `App.cs` / `IApp.cs` — top-level run/stop lifecycle; sets up NLog.
- `AppContainer.cs` — **Autofac DI registration**. Every service and command is registered here.
- `CommandArguments/` — `Arguments` (parsed CLI options), `ArgumentsBase`, `ConnectionArguments`,
  and `CommandEnumeration` (the `--action` values).
- `Commands/`
  - `CommandDispatcher.cs` — maps a `CommandEnumeration` value to the matching command method
    (a big `switch`). This is the routing hub.
  - `Command/` — one class per area of behaviour (`ParameterCommand`, `OverrideCommand`,
    `OptionCommand`, `StateCommand`, `DomainCommand`, `ScaleCommand`, `SubscriptionCommand`,
    `ValueSetCommand`, `RequirementSimpleParameterValueCommand`), each with an `I*` interface in
    `Command/Interface/`.
  - `ReportGenerator.cs` — CSV report of affected parameters.
- `Services/` — `SessionService` (owns the CDP4 `ISession`, open/close/save), `FilterService`
  (selects the elements/parameters a command operates on), `CsvFileWriter`, each with an
  interface in `Services/Interfaces/`.
- `Resources/` — embedded resources (`ascii-art.txt`) and `ResourceLoader`.
- `Extensions/` — `DalSessionExtensions`, `HashSetExtensions`.

## How a command flows

1. `Program.Main` parses CLI args into an `Arguments` instance.
2. `AppContainer.BuildContainer` registers everything in Autofac.
3. `App.Run` calls `CommandDispatcher.Invoke`.
4. `Invoke` switches on `Arguments.Command` and calls the relevant `I*Command` method.
5. The command uses `ISessionService` (model data) and `IFilterService` (scoping) to build a
   transaction of changes and write them back.
6. If `--report` is set, `ReportGenerator.ParametersToCsv` runs.

### Adding a new action (the common change)

1. Add a value to `CommandArguments/CommandEnumeration.cs`.
2. Add the behaviour to an existing `*Command` class (or create a new `ICommand`/`Command` pair
   in `Commands/Command/` + `Commands/Command/Interface/`).
3. If you created a new command type, **register it in `AppContainer.cs`** and inject it into
   `CommandDispatcher` (constructor + private field).
4. Add a `case` in `CommandDispatcher.Invoke`.
5. Add a test fixture under `CDPBatchEditor.Tests/Commands/Command/` (subclass
   `BaseCommandTestFixture`, which builds an in-memory `EngineeringModel` test graph).
6. Document the new action with an example in `CommandExamples.md`.

## Build, test, run

```bash
dotnet restore
dotnet build                 # whole solution
dotnet test                  # run the NUnit suite

# Run the tool from source (args after --):
dotnet run --project CDPBatchEditor -- -s http://localhost:5000 -u admin -p pass \
  --action AddParameters -m LOFT --parameters n_items \
  --element-definition a1mil_layer_kapton_on_BEE_boxes --domain SYS
```

CI filters out integration-style tests; mirror it locally when you want the unit-only run:

```bash
dotnet test --filter "(TestCategory!~WebServicesDependent) & (TestCategory!~AppVeyorExclusion)"
```

See `CommandExamples.md` for an example invocation of every `--action`. Common options:
`-s` server URL, `-u`/`-p` credentials, `-m` engineering model, `--action`, `--parameters`,
`--element-definition`, `--domain`, `--report`.

## Code conventions (from `.github/CONTRIBUTING.md` — follow exactly)

- Every `.cs` file starts with the Starion Group LGPL **copyright header** (copy the block from
  any existing file; keep `<copyright file="...">` matching the filename).
- 4-space indentation, never tabs.
- `using` directives go **inside** the `namespace` block (this codebase uses block-scoped
  namespaces, not file-scoped).
- Always qualify instance members with `this.` (e.g. `this.sessionService`).
- **No** `_` prefix on fields; use long, descriptive names; no Hungarian notation.
- Prefer `var` unless the inferred type is non-obvious.
- Use C# aliases (`int`, `string`) not framework names (`Int32`, `String`).
- Always brace `if`/`else`/`using`/blocks, even single-line.
- **No `#region`s.**
- Public members carry XML-doc comments (`<summary>`, `<param>`, `<see cref=...>`).
- A `.DotSettings` (ReSharper) file at the repo root encodes much of this.

## Testing conventions

- NUnit 4 + Moq. Fixtures end in `TestFixture` and live mirroring the source tree under
  `CDPBatchEditor.Tests/`.
- Command tests subclass `BaseCommandTestFixture`, which constructs an in-memory site
  directory / engineering model object graph and a mocked `ISessionService`.
- A PR with tests is strongly preferred by the maintainers.

## Git / contribution workflow

- Default/integration branch is **`development`** (not `master`/`main`). PRs target `development`.
- Never commit directly to `development`; always branch (e.g. `feat/...`, `fix/...`).
- Do not commit/push unless explicitly asked. End commit messages with the
  `Co-Authored-By: Claude` trailer per the harness rules.
- Contributors must sign the CLA in `CLA/` (see `README.md` / `.github/CONTRIBUTING.md`).

## Gotchas

- `release.bat` references `net7.0` paths but the projects target `net8.0` — the script's
  archive path is stale; don't trust it as a source of the target framework.
- The version is set in `CDPBatchEditor/CDPBatchEditor.csproj` (`<Version>`); the runtime
  banner reads it from the executing assembly (`Program.QueryBatchEditorVersion`).
- This tool **writes to live model data** on the configured server. Treat any command that
  isn't clearly read-only as destructive; never run real commands against a production server
  without explicit authorization.
- Don't edit anything under `bin/` or `obj/` — build output (git-ignored).