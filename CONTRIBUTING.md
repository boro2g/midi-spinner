# Contributing to Circular MIDI Generator

Thank you for your interest in contributing to Circular MIDI Generator! This document provides guidelines and information for contributors.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Contributing Guidelines](#contributing-guidelines)
- [Pull Request Process](#pull-request-process)
- [Coding Standards](#coding-standards)
- [Testing](#testing)
- [Documentation](#documentation)

## Code of Conduct

This project adheres to a code of conduct that we expect all contributors to follow. Please be respectful and inclusive in all interactions to help us maintain a welcoming community.

## Getting Started

### Ways to Contribute

- **Bug Reports**: Help us identify and fix issues
- **Feature Requests**: Suggest new features or improvements
- **Code Contributions**: Submit bug fixes, features, or optimizations
- **Documentation**: Improve guides, API docs, or examples
- **Testing**: Help test new features and report issues
- **Community Support**: Help other users in discussions

### Before You Start

1. **Check existing issues**: Look for existing bug reports or feature requests
2. **Discuss major changes**: Open an issue to discuss significant changes before implementing
3. **Read the documentation**: Familiarize yourself with the project structure and architecture

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- Git
- IDE: Visual Studio, VS Code, or JetBrains Rider
- Optional: MIDI device or software synthesizer for testing

### Setting Up the Development Environment

1. **Fork and Clone**
   ```bash
   git clone https://github.com/yourusername/circular-midi-generator.git
   cd circular-midi-generator
   ```

2. **Install Dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the Project**
   ```bash
   dotnet build
   ```

4. **Run Tests**
   ```bash
   dotnet test
   ```

5. **Run the Application**
   ```bash
   dotnet run --project src/CircularMidiGenerator
   ```

### Project Structure

```
circular-midi-generator/
├── src/
│   ├── CircularMidiGenerator/          # Main UI application
│   │   ├── Controls/                   # Custom Avalonia controls
│   │   ├── ViewModels/                 # MVVM view models
│   │   ├── Views/                      # XAML views
│   │   ├── Services/                   # UI-specific services
│   │   └── Converters/                 # Value converters
│   └── CircularMidiGenerator.Core/     # Core business logic
│       ├── Models/                     # Domain models
│       └── Services/                   # Core services
├── tests/                              # Unit and integration tests
├── docs/                               # Documentation
├── scripts/                            # Build and deployment scripts
└── .kiro/                             # Kiro configuration and specs
```

## Contributing Guidelines

### Issue Guidelines

#### Bug Reports
When reporting bugs, please include:
- **Clear title**: Descriptive summary of the issue
- **Environment**: OS, .NET version, hardware details
- **Steps to reproduce**: Detailed steps to recreate the issue
- **Expected behavior**: What should happen
- **Actual behavior**: What actually happens
- **Screenshots/logs**: Visual evidence or error logs
- **MIDI setup**: Device information if MIDI-related

#### Feature Requests
When requesting features, please include:
- **Clear description**: What you want to achieve
- **Use case**: Why this feature would be valuable
- **Proposed solution**: How you envision it working
- **Alternatives**: Other approaches you've considered

### Code Contributions

#### Branch Naming
- `feature/description`: New features
- `bugfix/description`: Bug fixes
- `hotfix/description`: Critical fixes
- `docs/description`: Documentation updates
- `refactor/description`: Code refactoring

#### Commit Messages
Follow conventional commit format:
```
type(scope): description

[optional body]

[optional footer]
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

Examples:
```
feat(ui): add multi-touch marker selection
fix(midi): resolve device reconnection issue
docs(readme): update installation instructions
```

## Pull Request Process

### Before Submitting

1. **Update your fork**: Sync with the latest main branch
2. **Create feature branch**: Branch from main for your changes
3. **Write tests**: Add tests for new functionality
4. **Update documentation**: Update relevant docs
5. **Test thoroughly**: Ensure all tests pass
6. **Check code style**: Follow project coding standards

### Submitting the PR

1. **Clear title**: Descriptive summary of changes
2. **Detailed description**: Explain what and why
3. **Link issues**: Reference related issues
4. **Screenshots**: Include UI changes visually
5. **Testing notes**: How to test your changes

### PR Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing completed
- [ ] Performance impact assessed

## Screenshots
(If applicable)

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] Tests added/updated
- [ ] No breaking changes (or documented)
```

### Review Process

1. **Automated checks**: CI/CD pipeline runs tests
2. **Code review**: Maintainers review code quality
3. **Testing**: Functionality and performance testing
4. **Approval**: At least one maintainer approval required
5. **Merge**: Squash and merge to main branch

## Coding Standards

### C# Guidelines

Follow Microsoft's C# coding conventions and our project-specific standards:

#### Naming Conventions
- **Classes**: PascalCase (`MidiService`)
- **Methods**: PascalCase (`SendNoteOn`)
- **Properties**: PascalCase (`CurrentAngle`)
- **Fields**: camelCase with underscore (`_midiDevice`)
- **Constants**: UPPER_CASE (`MAX_VELOCITY`)
- **Interfaces**: PascalCase with 'I' prefix (`IMidiService`)

#### Code Organization
- One class per file
- Meaningful names that describe purpose
- Keep methods small and focused
- Use dependency injection for services
- Prefer composition over inheritance

#### Documentation
- XML documentation for public APIs
- Inline comments for complex logic
- README files for major components

### XAML Guidelines

- Use meaningful names for controls (`x:Name`)
- Organize resources in ResourceDictionaries
- Use data binding over code-behind
- Follow MVVM pattern consistently

### Performance Guidelines

- Minimize allocations in hot paths
- Use async/await for I/O operations
- Profile performance-critical code
- Consider memory usage in real-time scenarios

## Testing

### Test Categories

#### Unit Tests
- Test individual components in isolation
- Mock external dependencies
- Fast execution (< 1ms per test)
- High code coverage target (> 80%)

#### Integration Tests
- Test component interactions
- Use real MIDI devices when possible
- Test error scenarios and edge cases
- Validate timing and performance

#### Performance Tests
- Measure MIDI latency and jitter
- Test with large numbers of markers
- Validate memory usage and leaks
- Ensure smooth UI performance

### Running Tests

```bash
# All tests
dotnet test

# Specific category
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
dotnet test --filter Category=Performance

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Writing Tests

#### Test Structure
```csharp
[Test]
public void MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var service = new MidiService();
    
    // Act
    var result = service.DoSomething();
    
    // Assert
    Assert.That(result, Is.EqualTo(expected));
}
```

#### Mocking
Use Moq for mocking dependencies:
```csharp
var mockDevice = new Mock<IMidiDevice>();
mockDevice.Setup(x => x.SendNote(It.IsAny<int>()))
         .Returns(true);
```

## Documentation

### Types of Documentation

#### Code Documentation
- XML comments for public APIs
- Inline comments for complex algorithms
- Architecture decision records (ADRs)

#### User Documentation
- Getting started guide
- Feature documentation
- Troubleshooting guides
- API reference

#### Developer Documentation
- Setup instructions
- Architecture overview
- Contributing guidelines
- Deployment procedures

### Documentation Standards

- Write for your audience (users vs developers)
- Use clear, concise language
- Include code examples
- Keep documentation up to date
- Use proper markdown formatting

### Building Documentation

```bash
# Generate API documentation
dotnet tool install -g docfx
docfx docs/docfx.json --serve
```

## Release Process

### Version Numbering

We use Semantic Versioning (SemVer):
- **Major** (X.0.0): Breaking changes
- **Minor** (0.X.0): New features, backward compatible
- **Patch** (0.0.X): Bug fixes, backward compatible

### Release Checklist

1. **Update version numbers** in project files
2. **Update CHANGELOG.md** with release notes
3. **Run full test suite** and performance tests
4. **Build release packages** for all platforms
5. **Test packages** on target platforms
6. **Create GitHub release** with release notes
7. **Update documentation** if needed

## Getting Help

### Community Resources

- **GitHub Discussions**: Ask questions and share ideas
- **GitHub Issues**: Report bugs and request features
- **Discord**: Real-time chat with the community
- **Email**: support@circularmidi.com for direct support

### Maintainer Contact

- **Project Lead**: See GitHub repository maintainers
- **Core Team**: Active contributors listed in GitHub insights

## Recognition

Contributors are recognized in several ways:

- **GitHub**: Contributor statistics, commit history, and badges
- **Release notes**: Mentioned in release announcements
- **Project README**: Acknowledgments section
- **Special recognition**: Outstanding contributions highlighted

## License

By contributing to Circular MIDI Generator, you agree that your contributions will be licensed under the MIT License. See [LICENSE](LICENSE) for details.

---

Thank you for contributing to Circular MIDI Generator! Your efforts help make music creation more accessible and enjoyable for everyone. 🎵