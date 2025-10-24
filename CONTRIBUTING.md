# BMTP3

BMTP3 is an application for backing up using MTP (Media Transfer Protocol), PTP (Picture Transport Protocol), and MSC (Mass Storage Class) devices such as cell phones, tablets, and cameras.

## License

This project is licensed under the [GNU Affero General Public License v3.0](https://www.gnu.org/licenses/agpl-3.0.html).  
See [LICENSE.md](LICENSE.md) for full license details.

## Contributor License Agreement (CLA)

By contributing to this project, you agree that your contributions are licensed under the AGPL-3.0 license, and you grant the project owner the right to relicense your contributions under other terms in the future.  
See [CLA.md](CLA.md) for details.

## How to Contribute

1. **Fork the repository** and create your branch from `dev/develop_4_ai_refactor`.
2. **Describe your changes clearly** in the pull request description. Reference related issues if relevant.
3. **Keep pull requests focused**: Only address one feature, bugfix, or refactor per pull request.
4. **Follow the existing coding style and project conventions** (see below).
5. **Add or update tests** for any new or changed functionality.
6. **Ensure your code builds and passes all tests** before submitting.
7. **Update documentation** (README.md, code comments, etc.) if your changes affect usage or APIs.
8. **Sign your commits** if required by project policy.
9. **Be responsive to feedback**: Address review comments and suggestions promptly.

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

## Testing Guidelines

- Write unit tests for new features and bug fixes.
- Run all tests with `dotnet test` before submitting a pull request.
- Place tests in the appropriate test projects and follow naming conventions.

## Local Development Setup

1. Clone the repository:  
   `git clone https://github.com/preuss/BMTP3_CS.git`
2. Switch to the development branch:  
   `git checkout dev/develop_4_ai_refactor`
3. Build the project:  
   `dotnet build`
4. Run tests:  
   `dotnet test`

## Download

Future: [NuGet Package](https://www.nuget.org/packages/MediaDevices/)

## Documentation

<!-- Add contribution instructions, code style, and testing details here. -->

See README.md for usage and API details.  
Update this file if you add new contribution rules.

## Donate

You are welcome to support this project.

[![Donate](https://raw.githubusercontent.com/Bassman2/MediaDevices/master/.github/images/donate.gif)](https://www.paypal.me/GBassman)