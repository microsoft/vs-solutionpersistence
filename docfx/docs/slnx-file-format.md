# Solution (.slnx) file format

A solution organizes projects, solution folders, build configurations, and
solution-level settings. An `.slnx` file stores this shared information as XML.
Each project file still defines its own source files, references, and
project-specific build settings. User-specific Visual Studio settings are
stored separately, for example in the `.suo` file.

The XML format represents the same solution model as the traditional
[`.sln` format](https://learn.microsoft.com/visualstudio/extensibility/internals/solution-dot-sln-file),
but omits information that can be inferred from project paths and defaults.
Most solutions need only a list of projects:

```xml
<Solution>
  <Project Path="src/App/App.csproj" />
  <Project Path="test/App.Tests/App.Tests.csproj" />
</Solution>
```

Paths are relative to the solution file, not to the location of an enclosing
solution folder. Forward slashes are recommended for portable files; the
serializer also accepts backslashes. Loading a solution model does not require
opening the referenced project files.

## Document structure

The root is a single `Solution` element. An empty solution is valid:

```xml
<Solution />
```

The XML declaration is optional. The examples use no XML namespace.

| Child element | Purpose |
| --- | --- |
| `Configurations` | Solution build types, platforms, and project-type definitions. |
| `Folder` | A logical solution folder, its files, and its projects. |
| `Project` | A project at the solution root. |
| `Properties` | A named collection of persisted solution properties. |

`Solution` supports these optional attributes:

| Attribute | Meaning |
| --- | --- |
| `Description` | A user-visible description of the solution. |
| `Version` | The solution file format version, for example `1.0`. This is not a Visual Studio product version. |

The current format major version is `1`. Normally, `Version` can be omitted.
A reader rejects a version with a newer, unsupported major version.

## Projects

A `Project` can appear directly under `Solution` or inside a `Folder`.
Its required `Path` identifies the project throughout the solution. A project
path must be unique, even when projects are in different solution folders.
Path comparisons are case-insensitive.

| Attribute | Meaning |
| --- | --- |
| `Path` | Required project file path, or a URI for a project system that uses one. |
| `Type` | Project-type GUID or friendly name. Omit it when the file extension identifies the type. |
| `Id` | Optional project-instance GUID. If omitted, the model generates a deterministic identifier from the project path. |
| `DisplayName` | Name for a project whose path does not provide a filename-derived display name, such as a website URI ending in `/`. |
| `DefaultStartup` | Set to `true` to prefer this project as the default startup project when the solution is loaded. |

`Type` identifies a project system, while `Id` identifies one project instance.
They are different GUIDs with different purposes. An explicit `Id` can preserve
an existing project-instance identity when converting a traditional solution.

For file-backed projects, the filename without its extension determines the
display name. `DisplayName` does not override it. For example, a project at
`src/App/App.csproj` is named `App` even if `DisplayName` is supplied.
Projects in the same solution folder, including the solution root, must have
distinct display names, compared case-insensitively. Place projects with the
same name in different solution folders.

Common built-in type mappings include:

| Extension | Friendly type | Project-type GUID |
| --- | --- | --- |
| `.csproj` | `C#` | `FAE04EC0-301F-11D3-BF4B-00C04F79EFBC` |
| `.vbproj` | `VB` | `F184B08F-C81C-45F6-A57F-5ABD9991F28F` |
| `.fsproj` | `F#` | `F2A71F9B-5D33-465A-A702-920D77279786` |
| `.vcxproj` | `VC` | `8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942` |

Other types can use an explicit GUID or a `ProjectType` definition, described
below. A URI or an unrecognized extension generally needs an explicit type.

```xml
<Solution>
  <Project Path="src/App/App.csproj" DefaultStartup="true" />
  <Project Path="https://example.test/"
           Type="E24C65DC-7377-472B-9ABA-BC803B73C61A"
           DisplayName="Website" />
</Solution>
```

## Solution folders and files

A solution folder is a logical grouping, not a filesystem directory. Its
required `Name` is a full logical path that starts and ends with `/`.
Nested folder paths contain additional `/` separators. Declare folders as
siblings under `Solution`, rather than nesting `Folder` elements:

```xml
<Solution>
  <Folder Name="/Source/">
    <File Path="README.md" />
    <Project Path="src/App/App.csproj" />
  </Folder>
  <Folder Name="/Source/Libraries/">
    <Project Path="src/Library/Library.csproj" />
  </Folder>
</Solution>
```

The `/Source/` parent can also be created implicitly by declaring
`/Source/Libraries/`. A `File` requires a `Path`; it adds an existing file to
the logical folder, without making it part of a project or changing how it is
built. Its path, like a project's path, is relative to the solution file.

A folder can also have an optional instance `Id` and `Properties` collections.
Without an explicit `Id`, its identifier is derived from its logical path.
Folder names and file paths within a folder are compared case-insensitively.

## Solution configurations

A solution configuration is a build type and platform pair, such as
`Debug|Any CPU`. Define the available dimensions in the single optional
`Configurations` element:

```xml
<Solution>
  <Configurations>
    <BuildType Name="Debug" />
    <BuildType Name="Release" />
    <BuildType Name="Shipping" />
    <Platform Name="Any CPU" />
    <Platform Name="x64" />
  </Configurations>
  <Project Path="src/App/App.csproj" />
</Solution>
```

The combinations of the listed build types and platforms are the solution
configurations. These declarations describe the solution's choices; they do
not add configurations to the project files themselves.

For a solution containing projects, omitted dimensions have these defaults:

| Dimension | Default |
| --- | --- |
| No `BuildType` entries | `Debug` and `Release` |
| No `Platform` entries | `Any CPU` |

These defaults apply independently. For example, a `Configurations` element
containing only `Platform` entries still has `Debug` and `Release` build types.
The serializer can omit explicitly listed defaults when saving.

### Mapping solution configurations to projects

Place mapping rules inside a `Project` to override how a solution configuration
selects that project's configuration and build participation:

```xml
<Solution>
  <Configurations>
    <BuildType Name="Debug" />
    <BuildType Name="Release" />
    <Platform Name="Any CPU" />
    <Platform Name="x64" />
  </Configurations>
  <Project Path="src/App/App.csproj">
    <BuildType Solution="Release|*" Project="Release" />
    <Platform Solution="*|x64" Project="Any CPU" />
    <Build Solution="Debug|x64" Project="false" />
    <Deploy Solution="Release|x64" />
  </Project>
</Solution>
```

| Rule element | Meaning of `Project` |
| --- | --- |
| `BuildType` | Project build type to select. |
| `Platform` | Project platform to select. |
| `Build` | Whether the project participates in the solution build: `true` or `false`. |
| `Deploy` | Whether the project participates in solution deployment: `true` or `false`. |

The optional `Solution` selector has the form `BuildType|Platform`. Either
dimension can be `*`, meaning all values of that dimension. Omitting `Solution`
applies the rule to every solution configuration. Use a distinct selector for
each rule of the same element type within a project or project-type definition.

`BuildType` and `Platform` require a nonempty `Project` value. For these mappings,
`Project="*"` selects the corresponding solution value. `Build` and `Deploy`
default their `Project` value to `true` when the attribute is omitted, so
`<Deploy />` enables deployment for all solution configurations.

Without overriding rules, a project generally follows the solution build type
and platform, participates in the build, and does not deploy. Built-in
project-type rules refine these defaults: for example, C# projects map to
`AnyCPU`, Visual C++ projects map the solution platform `x86` to `Win32`, and
shared-code projects do not build separately.

### Project-type definitions and shared defaults

`ProjectType` entries in `Configurations` associate extensions or friendly names
with GUIDs and define configuration rules shared by projects of that type:

```xml
<Solution>
  <Configurations>
    <ProjectType Name="Custom" Extension=".customproj"
                 TypeId="B1CC274F-733D-46A5-B148-8309670B362D">
      <Platform Project="x64" />
    </ProjectType>
    <ProjectType Name="Custom library" Extension=".customlib"
                 BasedOn="Custom" />
  </Configurations>
  <Project Path="src/App/App.customproj" />
  <Project Path="src/Library/Library.customlib" />
</Solution>
```

| Attribute | Meaning |
| --- | --- |
| `Name` | Friendly type name that a project's `Type` can reference. |
| `Extension` | Extension used to infer the type when possible; a leading `.` is recommended. |
| `TypeId` | Project-type GUID. It can be inherited through `BasedOn`. |
| `BasedOn` | Base type from which to inherit its GUID and configuration rules. Use a declared type's friendly name, or a built-in type's name or extension. |
| `IsBuildable` | Defaults to `true`. Setting it to `false` supplies a no-build rule and ignores other rules on that type. |
| `SupportsPlatform` | Defaults to `true`. Setting it to `false` marks the project type as having no platform configuration. Ignored when `IsBuildable` is `false`. |

A named or extension-specific definition must be identifiable by `Name` or
`Extension`. Extension mappings and friendly names must be unique within the
solution's definitions. A `BasedOn` reference must resolve and must not form
a cycle.

A `ProjectType` with no `Name`, `Extension`, or `TypeId` defines solution-wide
default rules rather than a particular project type. For example, a
`<Build Project="false" />` rule inside such an entry disables building by
default; individual projects can opt back in.

Configuration rules are applied from general to specific: built-in defaults,
base project types, the selected project type, solution-wide defaults, and
finally the project's own rules. A project's explicit mapping therefore takes
precedence over a shared default. Shared defaults apply when the project type
resolves through the built-in table or the solution's definitions; a bare,
unregistered GUID has no project-type rule chain.

## Build dependencies

A project's `BuildDependency` elements identify other projects that must build
before it. The required `Project` attribute is the other project's solution
path, not its GUID or display name:

```xml
<Solution>
  <Project Path="src/App/App.csproj">
    <BuildDependency Project="src/Generator/Generator.csproj" />
  </Project>
  <Project Path="src/Generator/Generator.csproj" />
</Solution>
```

The referenced project must exist in the solution, and a project cannot depend
on itself. A dependency can reference a project in another solution folder.
Project-to-project references normally belong in the project files; use
solution dependencies for relationships that need to be persisted at solution
level.

## Persisted properties

`Solution`, `Folder`, and `Project` can contain named `Properties` collections.
Each collection contains `Property` elements with a required `Name` and an
optional `Value`. An omitted `Value` represents an empty string.

```xml
<Solution>
  <Project Path="src/App/App.csproj" />
  <Properties Name="ExampleExtension">
    <Property Name="Mode" Value="Shared" />
  </Properties>
  <Properties Name="ExampleExtension.AfterLoad" Scope="PostLoad">
    <Property Name="Enabled" Value="true" />
  </Properties>
</Solution>
```

`Properties.Name` identifies the collection to its consumer. Its optional
`Scope` defaults to `PreLoad`; `PostLoad` marks information for use after
project loading. These correspond to the traditional solution format's
pre-solution and post-solution property sections. Values are strings; their
interpretation belongs to the component that persists them.

## Editing and compatibility

Use ordinary XML escaping inside attribute values: for example, write `&amp;`
for `&`, `&lt;` for `<`, and `&quot;` for a double quote. XML comments can
describe solution-specific choices.

The serializer preserves existing comments, whitespace, path separator choices,
and unrecognized XML where possible when saving the loaded model. This
preservation is not a guarantee that every tool understands an extension.
Converting through another file format or constructing a new model can discard
XML-only information.

Compared with a traditional `.sln` file:

| Traditional `.sln` | XML `.slnx` |
| --- | --- |
| `Project` blocks with type and instance GUIDs | `Project` elements; type and instance IDs can be inferred or generated. |
| `NestedProjects` GUID mappings | Full logical paths in `Folder.Name` and projects contained in those folders. |
| Solution and project configuration sections | `Configurations` dimensions, defaults, and project mapping rules. |
| `ProjectDependencies` GUID entries | `BuildDependency` elements referencing project paths. |
| Named global or project property sections | Named `Properties` collections on solutions, folders, or projects. |

For the implementation's schema and examples, see
[Slnx.xsd](https://github.com/microsoft/vs-solutionpersistence/blob/main/src/Microsoft.VisualStudio.SolutionPersistence/Serializer/Xml/Slnx.xsd)
and the [sample solution files](https://github.com/microsoft/vs-solutionpersistence/tree/main/test/Microsoft.VisualStudio.SolutionPersistence.Tests/SlnAssets).
The schema is not exhaustive; for example, the reader also supports the `Id`
and `DefaultStartup` attributes described above.
