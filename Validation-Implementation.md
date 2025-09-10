# Validation Implementation with FluentValidation

## Overview
This document outlines the comprehensive validation system implemented using FluentValidation for all create commands in the BookSpot application. The validation occurs automatically before command handlers execute, ensuring data integrity and providing clear error messages.

## Architecture

### Validation Pipeline
1. **Request Received** → API Controller
2. **Command Created** → MediatR Command
3. **Validation Executed** → FluentValidation Validators (via ValidationBehavior)
4. **Handler Executed** → Only if validation passes
5. **Response Returned** → Success or validation errors

### Components

#### 1. ValidationBehavior
- **Location**: `BookSpot.Application/Behaviors/ValidationBehavior.cs`
- **Purpose**: MediatR pipeline behavior that automatically validates commands
- **Execution**: Runs before command handlers
- **Error Handling**: Throws `ValidationException` with detailed error information

#### 2. Custom ValidationException
- **Location**: `BookSpot.Application/Exceptions/ValidationException.cs`
- **Features**: 
  - Supports FluentValidation error format
  - Groups errors by property name
  - Integrates with GlobalExceptionHandler

#### 3. DependencyInjection
- **Location**: `BookSpot.Application/DependencyInjection.cs`
- **Registers**: MediatR, FluentValidation validators, and ValidationBehavior

## Implemented Validators

### 1. CreateBusinessCommandValidator

**Validates**: Business creation requests

```csharp
public record CreateBusinessCommand(string BusinessName, string City) : IRequest<Business>;
```

**Validation Rules**:
- ✅ **BusinessName**: Required, 2-100 characters, alphanumeric with common business characters
- ✅ **City**: Required, 2-50 characters, alphabetic with common city characters

**Example Valid Request**:
```json
{
  "businessName": "Sunny Day Spa & Wellness",
  "city": "New York"
}
```

**Example Validation Errors**:
```json
{
  "status": 400,
  "title": "Validation Error",
  "errors": {
    "BusinessName": ["Business name is required."],
    "City": ["City must be between 2 and 50 characters."]
  }
}
```

### 2. CreateServiceCommandValidator

**Validates**: Service creation requests

```csharp
public record CreateServiceCommand(string BusinessId, string Name, decimal Price, int DurationMinutes) : IRequest<Service>;
```

**Validation Rules**:
- ✅ **BusinessId**: Required, valid GUID format
- ✅ **Name**: Required, 2-100 characters, alphanumeric with common service characters
- ✅ **Price**: Greater than 0, max $10,000, max 2 decimal places
- ✅ **DurationMinutes**: Greater than 0, max 8 hours (480 minutes), 15-minute increments

**Example Valid Request**:
```json
{
  "businessId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Deep Tissue Massage",
  "price": 85.50,
  "durationMinutes": 90
}
```

### 3. CreateBusinessHourCommandValidator

**Validates**: Business hours creation requests

```csharp
public record CreateBusinessHourCommand(string BusinessId, int DayOfWeek, string OpenTime, string CloseTime, bool IsClosed) : IRequest<BusinessHour>;
```

**Validation Rules**:
- ✅ **BusinessId**: Required, valid GUID format
- ✅ **DayOfWeek**: 0-6 (Sunday-Saturday)
- ✅ **OpenTime**: Required when not closed, valid time format (HH:mm)
- ✅ **CloseTime**: Required when not closed, valid time format (HH:mm)
- ✅ **Time Range**: Close time must be after open time (handles overnight hours)
- ✅ **Duration**: Business hours cannot exceed 16 hours per day

**Example Valid Request**:
```json
{
  "businessId": "123e4567-e89b-12d3-a456-426614174000",
  "dayOfWeek": 1,
  "openTime": "09:00",
  "closeTime": "17:00",
  "isClosed": false
}
```

### 4. CreateBookingCommandValidator

**Validates**: Booking creation requests

```csharp
public record CreateBookingCommand(string ServiceId, DateTime StartTime, DateTime EndTime) : IRequest<Booking>;
```

**Validation Rules**:
- ✅ **ServiceId**: Required, valid GUID format
- ✅ **StartTime**: Required, future date (min 30 minutes advance), max 1 year future, 15-minute intervals
- ✅ **EndTime**: Optional, must be after start time, reasonable duration (15 minutes - 8 hours)
- ✅ **Business Hours**: Start time within reasonable hours (6 AM - 11 PM)

**Example Valid Request**:
```json
{
  "serviceId": "123e4567-e89b-12d3-a456-426614174000",
  "startTime": "2024-01-15T14:00:00Z",
  "endTime": "2024-01-15T15:30:00Z"
}
```

## Validation Features

### 1. Data Type Validation
- **GUID Format**: Ensures IDs are valid GUIDs
- **Time Format**: Validates time strings (HH:mm)
- **Date Range**: Ensures dates are reasonable and future-dated
- **Decimal Precision**: Validates price precision and scale

### 2. Business Logic Validation
- **Service Duration**: Must be in 15-minute increments
- **Business Hours**: Cannot exceed 16 hours per day
- **Booking Advance**: Minimum 30-minute advance booking
- **Time Intervals**: Bookings must be on 15-minute intervals

### 3. Security Validation
- **Character Restrictions**: Prevents injection attacks
- **Length Limits**: Prevents buffer overflow attacks
- **Range Validation**: Prevents unreasonable values

### 4. User Experience
- **Clear Error Messages**: Descriptive validation messages
- **Field-Specific Errors**: Errors grouped by field
- **Multiple Errors**: Shows all validation errors at once

## Error Response Format

### Single Field Error
```json
{
  "status": 400,
  "title": "Validation Error",
  "detail": "One or more validation errors occurred.",
  "errors": {
    "Price": ["Service price must be greater than 0."]
  }
}
```

### Multiple Field Errors
```json
{
  "status": 400,
  "title": "Validation Error",
  "detail": "One or more validation errors occurred.",
  "errors": {
    "BusinessName": [
      "Business name is required.",
      "Business name must be between 2 and 100 characters."
    ],
    "City": ["City contains invalid characters."],
    "Price": ["Service price must be greater than 0."]
  }
}
```

## Integration with JWT Claims

The validation system works seamlessly with JWT claims integration:

1. **Authentication Check**: Validators don't need to check authentication (handled by handlers)
2. **Data Validation**: Validators focus purely on data format and business rules
3. **Authorization**: Handlers use claims service for ownership and role validation
4. **Clean Separation**: Clear separation between validation and authorization concerns

## Testing Validation

### Valid Requests
All validators allow reasonable, well-formatted data that follows business rules.

### Invalid Requests
Validators catch and provide clear errors for:
- Missing required fields
- Invalid formats (GUIDs, times, etc.)
- Out-of-range values
- Business rule violations
- Security concerns (invalid characters)

## Performance Considerations

1. **Early Validation**: Validation occurs before expensive operations
2. **Parallel Validation**: Multiple validators can run concurrently
3. **Fail Fast**: Stops processing on first validation failure
4. **Memory Efficient**: Minimal memory overhead for validation rules

## Future Enhancements

1. **Conditional Validation**: More complex conditional rules
2. **Cross-Field Validation**: Validation across multiple fields
3. **Async Validation**: Database-dependent validation rules
4. **Custom Validators**: Domain-specific validation logic
5. **Localization**: Multi-language error messages

## Best Practices

1. **Single Responsibility**: Each validator focuses on one command
2. **Clear Messages**: Error messages are user-friendly and actionable
3. **Business Rules**: Validation reflects real business constraints
4. **Security First**: Input validation prevents common attacks
5. **Maintainable**: Validation rules are easy to update and extend