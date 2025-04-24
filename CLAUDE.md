# OpenPrismNode Development Guide

## Build & Run Commands
- Build solution: `dotnet build OpenPrismNode.sln`
- Run web application: `dotnet run --project OpenPrismNode.Web`
- Run all tests: `dotnet test OpenPrismNode.sln`
- Run specific test class: `dotnet test --filter "FullyQualifiedName~OpenPrismNode.Core.IntegrationTests.CreateBlock.CreateBlockTests"`
- Run single test: `dotnet test --filter "FullyQualifiedName=OpenPrismNode.Core.IntegrationTests.IntegrationTests.CreateBlock_Succeeds_For_Default_Case"`

## Code Style Guidelines
- Language: C# 12 with .NET 9.0
- Naming: PascalCase for classes/methods/properties, camelCase for variables/parameters
- Prefer async/await over Task.Result or .Wait()
- Use nullable reference types with `Nullable enable` in project files
- Use FluentResults for return values when appropriate
- Handler pattern with MediatR for command/query handling
- Use dependency injection for services and handlers
- Organize code with folders by feature (Commands, Models, Services)
- Document public APIs with XML comments
- Prefer Task-based async patterns over callbacks
- Use xUnit for testing with AAA pattern (Arrange-Act-Assert)