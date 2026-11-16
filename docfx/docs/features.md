# Features

## Duplicate project names

Project names are compared case-insensitively within the same solution folder,
including the solution root. The library maintains a project-type GUID allowlist
whose projects do not participate in this comparison. Initially, the list contains
only Rust (`0D916ADF-294A-4D07-A543-8F58229C3415`), whose projects can share the
fixed manifest filename `Cargo.toml` while defining their names inside the manifest.

The exemption uses the resolved project-type GUID. It applies both to explicitly
specified GUIDs and to types inferred from an existing `ProjectType` mapping:

```csharp
var solution = new SolutionModel
{
    ProjectTypes =
    [
        new ProjectType(new Guid("0D916ADF-294A-4D07-A543-8F58229C3415"), [])
        {
            Name = "Rust",
            Extension = ".toml",
        },
    ],
};
solution.AddProject(filePath: "app1/Cargo.toml");
solution.AddProject(filePath: "app2/Cargo.toml");
```

An exempt project does not conflict with either another exempt project or an
ordinary project. Ordinary projects must still have distinct sibling names,
and project paths must remain unique throughout the solution. Changing a
project's type to a non-exempt type validates its name and rejects the change
if it would introduce a conflict.

This exemption does not change display names. File-backed projects continue to
derive `ActualDisplayName` from their filename, ignoring `DisplayName`.
The library does not register `.toml` as a built-in Rust extension; the example
explicitly defines that association for its solution.
