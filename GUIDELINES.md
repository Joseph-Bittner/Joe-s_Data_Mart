# Joe's Data Mart - Development Guidelines

## Project Overview
Joe's Data Mart is an ASP.NET Core MVC web application that provides a user-friendly interface for interacting with SQL Server databases. The application features a modern, responsive design with Bootstrap styling and connects to the `DeveloperJosephBittner` database on the `vFS-SQL02-NS` server.

## 1. Project Structure Guidelines

### Directory Structure
```
DataMart/
├── Controllers/           # MVC Controllers
├── Views/                # Razor Views
│   ├── Shared/          # Layout and shared views
│   └── Home/            # Home controller views
├── wwwroot/             # Static files
│   ├── css/            # Stylesheets
│   └── js/             # JavaScript files
├── Models/              # Data models (if needed)
├── Services/            # Business logic services
├── Data/                # Data access layer
└── Utilities/           # Helper classes
```

### File Naming Conventions
- **Controllers**: `{Feature}Controller.cs` (e.g., `HomeController.cs`)
- **Views**: PascalCase with `.cshtml` extension
- **CSS Classes**: kebab-case (e.g., `datamart-container`)
- **C# Classes**: PascalCase
- **Methods**: PascalCase
- **Variables**: camelCase
- **Constants**: SCREAMING_SNAKE_CASE

## 2. Coding Standards

### C# Guidelines
- Use meaningful variable and method names
- Follow SOLID principles
- Use async/await for I/O operations
- Implement proper error handling with try/catch
- Use dependency injection
- Keep methods small and focused (Single Responsibility)
- Use LINQ for data manipulation when appropriate

### Database Interaction
- Always use parameterized queries to prevent SQL injection
- Use `using` statements for database connections
- Implement connection pooling
- Handle connection timeouts gracefully
- Log database errors appropriately
- Use transactions for multi-step operations

### Security Guidelines
- Never expose sensitive data in client-side code
- Validate all user inputs
- Use HTTPS in production
- Implement proper authentication/authorization if needed
- Sanitize database inputs
- Use environment variables for sensitive configuration

## 3. UI/UX Guidelines

### Design Principles
- **Centered Layout**: All content should be centered on the page
- **Responsive Design**: Must work on mobile, tablet, and desktop
- **Consistent Styling**: Use defined CSS classes, avoid inline styles
- **Accessible**: Follow WCAG guidelines for accessibility
- **Modern Look**: Use gradients, shadows, and smooth transitions

### Color Scheme
- Primary: Purple gradient (#667eea to #764ba2)
- Background: White with transparency effects
- Text: Dark gray (#333) for readability
- Accents: Bootstrap default colors for alerts

### Component Standards
- **Buttons**: Large, rounded, with hover effects
- **Cards**: Semi-transparent with blur effects
- **Alerts**: Centered with max-width constraints
- **Lists**: Styled with alternating rows and hover states

## 4. Database Guidelines

### Connection Management
- Server: `vFS-SQL02-NS`
- Database: `DeveloperJosephBittner`
- Authentication: Windows Authentication (Trusted Connection)
- Connection String: Use environment variables in production

### Query Standards
- Use `TOP` clause for limiting results
- Order results appropriately
- Include schema names in table references
- Use meaningful aliases for complex queries

### Data Access Layer
- Separate data access logic from business logic
- Use repository pattern if application grows
- Implement proper disposal of database resources
- Cache frequently accessed data when appropriate

## 5. Performance Guidelines

### Frontend
- Minimize HTTP requests
- Use CSS sprites for icons if needed
- Optimize images and assets
- Implement lazy loading for large datasets
- Use CDN for external libraries

### Backend
- Use async operations for I/O
- Implement caching strategies
- Optimize database queries
- Use connection pooling
- Monitor memory usage

## 6. Error Handling

### Client-Side
- Display user-friendly error messages
- Provide clear feedback for form submissions
- Handle network errors gracefully
- Show loading states during operations

### Server-Side
- Log errors with appropriate levels
- Return meaningful HTTP status codes
- Implement global error handling
- Don't expose internal error details to users

## 7. Testing Guidelines

### Unit Testing
- Test business logic thoroughly
- Mock external dependencies
- Use meaningful test names
- Cover edge cases and error scenarios

### Integration Testing
- Test database interactions
- Verify API endpoints
- Test user workflows end-to-end

## 8. Deployment Guidelines

### Development
- Use local SQL Server instance
- Run on localhost with HTTPS
- Use development certificates
- Enable detailed error pages

### Production
- Use production SQL Server
- Configure proper connection strings
- Enable HTTPS with valid certificates
- Set appropriate logging levels
- Implement monitoring and alerts

## 9. Maintenance Guidelines

### Code Reviews
- All changes require review
- Follow established coding standards
- Test changes thoroughly
- Document significant changes

### Documentation
- Keep README updated
- Document API endpoints
- Maintain code comments
- Update deployment guides

## 10. Version Control

### Git Guidelines
- Use descriptive commit messages
- Create feature branches for new work
- Merge to main only after review
- Tag releases appropriately

### Branching Strategy
- `main`: Production-ready code
- `develop`: Integration branch
- `feature/*`: New features
- `bugfix/*`: Bug fixes
- `hotfix/*`: Critical fixes

## 11. Monitoring and Logging

### Application Logs
- Log startup and shutdown events
- Log database connection issues
- Log user actions (anonymized)
- Log errors with stack traces

### Performance Monitoring
- Track response times
- Monitor database query performance
- Watch memory and CPU usage
- Set up alerts for critical issues

## 12. Future Enhancements

### Planned Features
- User authentication and authorization
- Advanced query builder
- Data export functionality
- Real-time data updates
- API endpoints for external integration

### Scalability Considerations
- Database connection pooling
- Caching layer implementation
- Load balancing preparation
- Microservices architecture planning

---

## Quick Reference Checklist

### Before Committing Code:
- [ ] Code follows naming conventions
- [ ] No inline styles in HTML
- [ ] Database queries are parameterized
- [ ] Error handling implemented
- [ ] Code is tested
- [ ] Documentation updated

### Before Deployment:
- [ ] Environment variables configured
- [ ] Database connection tested
- [ ] HTTPS enabled
- [ ] Security settings verified
- [ ] Performance tested

This document should be reviewed and updated as the project evolves. All team members should familiarize themselves with these guidelines to ensure consistent, high-quality development.