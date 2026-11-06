# Features

## Explicit project names

Projects normally use their project file's base name. Some project systems use
a fixed manifest filename, such as Rust's `Cargo.toml`, with the project name
defined inside the manifest rather than in its filename. For these projects,
use the four-argument `SolutionModel.AddProject` overload to supply a
`displayNameOverride` before duplicate-name validation:

```csharp
var solution = new SolutionModel();
const string projectType = "0D916ADF-294A-4D07-A543-8F58229C3415";
solution.AddProject(filePath: "app1/Cargo.toml", projectTypeName: projectType, folder: null, displayNameOverride: "app1");
solution.AddProject(filePath: "app2/Cargo.toml", projectTypeName: projectType, folder: null, displayNameOverride: "app2");
```

`SolutionProjectModel.DisplayNameOverride` can also change an existing project's
explicit name. Null restores the normal naming policy. Duplicate names remain
case-insensitively prohibited within a solution folder, and duplicate project
paths remain prohibited throughout a solution.

The XML serializer preserves explicit names using the existing `DisplayName`
attribute, including names equal to the current filename stem. The legacy
`DisplayName` model property retains its historical behavior: it does not
override a file-backed project's name. The classic `.sln` reader likewise
retains its legacy filename-based interpretation.
