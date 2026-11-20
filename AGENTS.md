# Copilot instructions for this repository

## High level guidance

* Review the `CONTRIBUTING.md` file for instructions to build and test the software.
* Run the `.github/Prime-ForCopilot.ps1` script (once) before running any `dotnet` or `msbuild` commands.
  If you see any build errors about not finding git objects or a shallow clone, it may be time to run this script again.

## Software Design

* Design APIs to be highly testable, and all functionality should be tested.
* Avoid introducing binary breaking changes in public APIs of projects under `src` unless their project files have `IsPackable` set to `false`.

## Testing

* Tests use TUnit and Microsoft.Testing.Platform, with xUnit assertions. Pass `--treenode-filter` after `--` instead of VSTest `--filter`.
* Run `tools/dotnet-test-cloud.ps1 -Configuration Release` after building to exclude tests marked `FailsInCloudTest`. Use `-IncludeNativeAOT` after publishing NativeAOT tests.
* The test project is `test/Microsoft.VisualStudio.SolutionPersistence.Tests/Microsoft.VisualStudio.SolutionPersistence.Tests.csproj`; its managed targets are `net10.0` and, on Windows, `net472`.

## Coding style

* Honor StyleCop rules and fix any reported build warnings *after* getting tests to pass.
* In C# files, use namespace *statements* instead of namespace *blocks* for all new files.
* Add API doc comments to all new public and internal members in shipping code under `src`. Tests and samples do not require XML API documentation; do not add XML docs to test or sample members solely to satisfy this rule.
