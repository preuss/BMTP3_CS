# BMTP3

BMTP3 is an application for backing up using MTP (Media Transfer Protocol), PTP (Picture Transport Protocol), and MSC (Mass Storage Class) devices such as cell phones, tablets, and cameras.

## License

This project is licensed under the [GNU Affero General Public License v3.0](https://www.gnu.org/licenses/agpl-3.0.html).  
See [LICENSE.md](LICENSE.md) for full license details.

## Contributor License Agreement (CLA)

By contributing to this project, you agree that your contributions are licensed under the AGPL-3.0 license, and you grant the project owner the right to relicense your contributions under other terms in the future.  
See [CLA.md](CLA.md) for details.

## How to Contribute

1. **Fork the repository** and create your branch from an appropriate `dev/*` branch.
2. **Describe your changes clearly** in the pull request description. Reference related issues if relevant.
3. **Keep pull requests focused**: Only address one feature, bugfix, or refactor per pull request.
4. **Follow the existing coding style and project conventions** (see below).
5. **Add or update tests** for any new or changed functionality.
6. **Ensure your code builds and passes all tests** before submitting.
7. **Update documentation** (README.md, code comments, etc.) if your changes affect usage or APIs.
8. **Sign your commits** if required by project policy.
9. **Be responsive to feedback**: Address review comments and suggestions promptly.

## Branching

- All branches under `dev/*` are considered **development** branches.
- Development branches follow the naming pattern: `dev/develop_n_description`
  - `n` is a **version or revision number** (incremented so it is clear which branch is newest/highest).
  - `description` is a **short description** so the purpose is visible directly in the branch name.

## Language Policy

- **Code, identifiers, and comments must be in English**.
- **Team communication** (issues, PR discussions, and general collaboration) is **primarily in Danish**.

## Issue Reporting

- Search for existing issues before creating a new one.
- Provide a clear and descriptive title and summary.
- For bugs: include steps to reproduce, expected and actual behavior, and relevant logs or screenshots.

## Code Style

- Follow .NET 8 conventions and C# best practices.
- Use `var` for local variables when the type is obvious.
- Name classes, methods, and variables descriptively in English.
- Indent with 4 spaces.
- Use XML documentation for public methods and classes.
- Avoid magic numbers and hardcoded paths.

## Project-specific conventions: Pipeline stages

To avoid ambiguity and compile-time conflicts (for example `CS0263`), the project standardizes pipeline-stage implementations as follows:

- Use `AbstractPipelineStage<TContext, TResult>` as the single canonical base class for pipeline stages. This base:
  - Implements `IPipelineStage<TContext, TResult>`.
  - Requires a `ProgressTracker` and updates progress inside the worker loop.
  - Provides the protected constructor:
    ```text
    protected AbstractPipelineStage(ILogger logger, int parallelism, TContext context, IBackupItemStep<TContext, TResult> step, ProgressTracker tracker)
    ```
- Concrete pipeline stage rules:
  - Inherit from `AbstractPipelineStage<TContext, TResult>`.
  - Accept `ProgressTracker tracker` as a constructor argument and pass it to `base(...)`.
  - Do not declare multiple partials with different base classes.
- When migrating existing code:
  - Update concrete stages that currently inherit `PipelineStageBase` to inherit `AbstractPipelineStage` and add the `ProgressTracker` parameter.
  - Update all construction sites to pass a `ProgressTracker` instance.
  - Remove or consolidate duplicate pipeline-stage class files to ensure one declaration per concrete class.

## Project Standards and Preferences

The following sections document project-wide preferences and guidelines contributors should follow. These are used by maintainers and automated tools.

### EditorConfig and Formatting

This repository includes an `.editorconfig` file at the repository root that defines indentation, newline, and C# analyzer preferences. Ensure your editor honors this configuration: in Visual Studio, confirm the __SettingName__ __Formatting > General__ is configured to respect `.editorconfig`.

### Automated Checks

Pull requests should pass CI checks including formatting, build, and unit tests. Fix any issues reported by the automated checks before requesting review.

## AI / KI (Kunstig Intelligens)

AI tools are allowed (code generation assistants, chat tools, automated refactoring).

- **You are responsible for the changes you submit**. Verify correctness, security, and licensing.
- Keep PRs focused and explain what was generated vs. what was hand-written when it helps review.
- Ensure generated code follows the repository standards (including `.editorconfig`) and is consistent with existing style.
- Do not paste proprietary or confidential code into external AI tools.
- Prefer documenting any AI-specific workflow notes in the PR description.

## Testing Guidelines

- Write unit tests for new features and bug fixes.
- Run all tests with `dotnet test` before submitting a pull request.
- Place tests in the appropriate test projects and follow naming conventions.

## Local Development Setup

1. Clone the repository:  
   `git clone https://github.com/preuss/BMTP3_CS.git`
2. Switch to a development branch (`dev/*`):  
   `git checkout dev/develop_n_description`
3. Build the project:  
   `dotnet build`
4. Run tests:  
   `dotnet test`

## Download

Future: [NuGet Package](https://www.nuget.org/packages/MediaDevices/)

## Documentation

See README.md for usage and API details.  
Update this file if you add new contribution rules.

## Donate

You are welcome to support this project.

[![Donate](https://raw.githubusercontent.com/Bassman2/MediaDevices/master/.github/images/donate.gif)](https://www.paypal.me/GBassman)

## Local testing

- During local/manual testing it is acceptable to keep short-circuit flags (e.g., `showAllTags`) in console helpers to limit output.