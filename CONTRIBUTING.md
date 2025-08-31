# Contributing to BookSpot

Thank you for your interest in contributing to BookSpot! This document provides guidelines and information for contributors.

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Docker Desktop
- Git
- AWS CLI (optional)

### Development Setup
1. Fork the repository
2. Clone your fork: `git clone https://github.com/yourusername/bookspot.git`
3. Set up LocalStack: `.\scripts\start-localstack.ps1`
4. Run the API: `cd BookSpot.API && dotnet run`
5. Test with the HTTP file: `BookSpot.http`

## 📋 Development Guidelines

### Code Style
- Follow standard C# conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Keep methods focused and concise

### Project Structure
- **Controllers**: API endpoints and request handling
- **Models**: Data models and DTOs
- **Services**: Business logic and data access
- **Tests**: Unit and integration tests

### Commit Messages
Use clear, descriptive commit messages:
```
feat: add booking conflict detection
fix: resolve timezone handling in availability
docs: update API documentation
refactor: simplify service registration
```

## 🧪 Testing

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Guidelines
- Write unit tests for business logic
- Include integration tests for API endpoints
- Test both success and error scenarios
- Use meaningful test names that describe the scenario

### LocalStack Testing
- Always test against LocalStack before submitting
- Verify data persistence and retrieval
- Test error handling with invalid data

## 📝 Pull Request Process

### Before Submitting
1. **Test Locally**: Ensure all tests pass
2. **Run LocalStack**: Verify functionality with local AWS services
3. **Update Documentation**: Update README or API docs if needed
4. **Check Code Style**: Follow established conventions

### PR Guidelines
1. **Clear Title**: Describe what the PR does
2. **Detailed Description**: Explain the changes and why
3. **Link Issues**: Reference related issues with `Fixes #123`
4. **Small Changes**: Keep PRs focused and manageable
5. **Update Tests**: Add or update tests for new functionality

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
- [ ] Tests pass locally
- [ ] LocalStack testing completed
- [ ] Manual testing performed

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] Tests added/updated
```

## 🐛 Bug Reports

### Before Reporting
1. Check existing issues
2. Test with latest version
3. Verify with LocalStack setup

### Bug Report Template
```markdown
**Describe the Bug**
Clear description of the issue

**To Reproduce**
Steps to reproduce the behavior:
1. Start LocalStack
2. Make API call to '...'
3. See error

**Expected Behavior**
What should happen

**Environment**
- OS: [Windows/Mac/Linux]
- .NET Version: [8.0]
- Docker Version: [x.x.x]

**Additional Context**
Any other relevant information
```

## 💡 Feature Requests

### Feature Request Template
```markdown
**Feature Description**
Clear description of the proposed feature

**Use Case**
Why is this feature needed?

**Proposed Solution**
How should this be implemented?

**Alternatives Considered**
Other approaches you've thought about

**Additional Context**
Any other relevant information
```

## 🏗️ Architecture Guidelines

### API Design
- Follow RESTful conventions
- Use appropriate HTTP status codes
- Include proper error responses
- Validate input data

### Database Design
- Use DynamoDB best practices
- Design efficient access patterns
- Consider query performance
- Plan for scalability

### AWS Integration
- Use AWS SDK best practices
- Handle AWS service errors gracefully
- Consider cost optimization
- Plan for different environments

## 📚 Resources

### Documentation
- [.NET 8 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [AWS Lambda .NET](https://docs.aws.amazon.com/lambda/latest/dg/lambda-csharp.html)
- [DynamoDB Developer Guide](https://docs.aws.amazon.com/dynamodb/latest/developerguide/)
- [LocalStack Documentation](https://docs.localstack.cloud/)

### Tools
- [Visual Studio Code](https://code.visualstudio.com/)
- [Visual Studio](https://visualstudio.microsoft.com/)
- [JetBrains Rider](https://www.jetbrains.com/rider/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

## 🤝 Code of Conduct

### Our Standards
- Be respectful and inclusive
- Focus on constructive feedback
- Help others learn and grow
- Maintain a professional environment

### Unacceptable Behavior
- Harassment or discrimination
- Trolling or insulting comments
- Personal attacks
- Spam or off-topic content

## 📞 Getting Help

### Channels
- **Issues**: For bugs and feature requests
- **Discussions**: For questions and general discussion
- **Documentation**: Check README and setup guides first

### Response Times
- We aim to respond to issues within 48 hours
- PRs are typically reviewed within a week
- Complex features may take longer to review

## 🎉 Recognition

Contributors will be recognized in:
- README contributors section
- Release notes for significant contributions
- Special thanks for major features or fixes

Thank you for contributing to BookSpot! 🚀