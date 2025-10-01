# General Development Practices

## Documentation Standards

### Documentation Structure
- Keep all project documentation in `/docs` folder
- Use Markdown (.md) format for all documentation files
- Organize docs by topic with clear, descriptive filenames
- Include a `/docs/README.md` as the documentation index

### Documentation Types
- **API Documentation**: Generated from code comments, stored in `/docs/api`
- **User Guides**: Step-by-step instructions in `/docs/guides`
- **Architecture**: System design and technical decisions in `/docs/architecture`
- **Development**: Setup, building, testing instructions in `/docs/development`

### Markdown Best Practices
- Use consistent heading hierarchy (# ## ### ####)
- Include table of contents for longer documents
- Use code blocks with language specification for syntax highlighting
- Include screenshots and diagrams where helpful
- Keep line length reasonable (80-120 characters)

## Project Root README

### Essential README Sections
The root `README.md` must always include:

1. **Project Title and Description**
   - Clear, concise project overview
   - Key features and capabilities

2. **Quick Start**
   - Prerequisites and system requirements
   - Installation steps
   - Basic usage example

3. **Running the Project**
   - Development environment setup
   - Build commands
   - Run commands for different scenarios

4. **Project Structure**
   - Brief overview of main folders and their purpose
   - Link to detailed architecture docs

5. **Contributing**
   - Link to contribution guidelines
   - Development workflow overview

6. **License and Contact**
   - License information
   - How to get help or report issues

### README Template Structure
```markdown
# Project Name

Brief description of what the project does and its main purpose.

## Quick Start

### Prerequisites
- .NET 8.0 or later
- [Other requirements]

### Installation
```bash
git clone [repository]
cd [project-folder]
dotnet restore
```

### Running the Application
```bash
dotnet run --project src/ProjectName
```

## Project Structure
- `src/` - Source code
- `docs/` - Documentation
- `tests/` - Test projects
- `.kiro/` - Kiro configuration and specs

## Documentation
See [docs/README.md](docs/README.md) for detailed documentation.

## Contributing
See [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines.
```

## Project Organization

### Folder Structure Standards
```
project-root/
├── README.md                 # Project overview and quick start
├── CONTRIBUTING.md           # Development guidelines
├── LICENSE                   # License file
├── .gitignore               # Git ignore rules
├── src/                     # Source code
│   ├── ProjectName/         # Main application
│   ├── ProjectName.Core/    # Core business logic
│   └── ProjectName.Tests/   # Unit tests
├── docs/                    # All documentation
│   ├── README.md           # Documentation index
│   ├── guides/             # User guides
│   ├── architecture/       # Technical design docs
│   └── development/        # Development setup docs
├── scripts/                # Build and utility scripts
└── .kiro/                  # Kiro configuration
    ├── specs/              # Feature specifications
    └── steering/           # Development guidelines
```

### File Naming Conventions
- Use kebab-case for documentation files (`user-guide.md`, `api-reference.md`)
- Use PascalCase for C# project folders and files
- Use lowercase for script files and configuration
- Be descriptive but concise in naming

## Documentation Maintenance

### Keep Documentation Current
- Update README.md when adding new features or changing setup
- Review and update docs during code reviews
- Include documentation updates in feature branches
- Use TODO comments in code to track documentation needs

### Documentation in Code
- Write clear XML documentation comments for public APIs
- Include usage examples in code comments
- Document complex algorithms and business logic
- Explain "why" not just "what" in comments

### Version Documentation
- Tag documentation versions with releases
- Maintain changelog for user-facing changes
- Archive old documentation versions when needed
- Link to specific documentation versions in releases

## Development Workflow Integration

### Pre-commit Checks
- Verify README.md is updated for significant changes
- Check that new features have corresponding documentation
- Ensure code examples in docs are tested and working
- Validate markdown formatting and links

### Code Review Guidelines
- Review documentation changes alongside code changes
- Verify examples and instructions are accurate
- Check for broken links and outdated information
- Ensure consistent style and formatting

### Continuous Integration
- Include documentation builds in CI pipeline
- Test code examples in documentation
- Check for broken internal and external links
- Generate and publish API documentation automatically

## Examples

### Good README Quick Start Section
```markdown
## Quick Start

### Prerequisites
- .NET 8.0 SDK
- A MIDI-capable device or software synthesizer

### Run the Application
```bash
# Clone and build
git clone https://github.com/user/circular-midi-generator.git
cd circular-midi-generator
dotnet build

# Run the application
dotnet run --project src/CircularMidiGenerator

# Run tests
dotnet test
```

### First Time Setup
1. Install .NET 8.0 SDK from [dotnet.microsoft.com](https://dotnet.microsoft.com)
2. Clone this repository
3. Run `dotnet restore` to install dependencies
4. Run `dotnet build` to compile the project
5. Run `dotnet run --project src/CircularMidiGenerator` to start the application
```

### Documentation Index Example
```markdown
# Documentation Index

## User Documentation
- [Getting Started](guides/getting-started.md)
- [User Interface Guide](guides/ui-guide.md)
- [MIDI Configuration](guides/midi-setup.md)

## Developer Documentation
- [Architecture Overview](architecture/overview.md)
- [API Reference](api/README.md)
- [Contributing Guidelines](../CONTRIBUTING.md)

## Technical Specifications
- [MIDI Implementation](architecture/midi-implementation.md)
- [Real-time Audio Processing](architecture/audio-processing.md)
```

## Quality Standards

### Documentation Quality Checklist
- [ ] Clear, concise writing without jargon
- [ ] Accurate and tested instructions
- [ ] Proper grammar and spelling
- [ ] Consistent formatting and style
- [ ] Working links and references
- [ ] Up-to-date screenshots and examples
- [ ] Accessible to target audience

### Maintenance Schedule
- Review README.md monthly for accuracy
- Update documentation with each release
- Quarterly review of all documentation for relevance
- Annual review of documentation structure and organization