# Business Rules Implementation

## Overview
This document outlines the business rules that have been enforced at the handler level in the BookSpot application to ensure data integrity and proper business logic flow.

## Implemented Business Rules

### 1. Business Creation Rules
**Handler**: `CreateBusinessHandler`
**Rules Enforced**:
- ✅ **Profile Existence**: A business can only be created if the associated profile exists
- ✅ **Provider Validation**: Only profiles with `UserType = "provider"` can create businesses
- ✅ **Exception Handling**: Throws `NotFoundException` for missing profiles and `ValidationException` for non-provider users

### 2. Service Creation Rules
**Handler**: `CreateServiceHandler`
**Rules Enforced**:
- ✅ **Business Existence**: A service can only be created if the associated business exists
- ✅ **Business Status**: Services can only be created for active businesses (`IsActive = true`)
- ✅ **Data Validation**: 
  - Service price cannot be negative
  - Service duration must be greater than 0 minutes
- ✅ **Exception Handling**: Throws `NotFoundException` for missing businesses and `ValidationException` for invalid data

### 3. Business Hours Creation Rules
**Handler**: `CreateBusinessHourHandler`
**Rules Enforced**:
- ✅ **Business Existence**: Business hours can only be created if the associated business exists
- ✅ **Day Validation**: Day of week must be between 0 (Sunday) and 6 (Saturday)
- ✅ **Time Validation**: 
  - Open and close times are required when business is not closed
  - Time format validation using `TimeSpan.TryParse()`
- ✅ **Exception Handling**: Throws `NotFoundException` for missing businesses and `ValidationException` for invalid data

### 4. Booking Creation Rules
**Handler**: `CreateBookingHandler`
**Rules Enforced**:
- ✅ **Service Availability**: Bookings can only be made for existing services
- ✅ **Profile Validation**: 
  - Client profile must exist
  - Provider profile must exist and have `UserType = "provider"`
- ✅ **Business Validation**:
  - Business associated with the service must exist and be active
  - Provider must own the business associated with the service
- ✅ **Time Validation**:
  - Start time must be before end time
  - Start time must be in the future
  - Booking duration must match service duration (±1 minute tolerance)
- ✅ **Conflict Prevention**: No overlapping bookings for the same provider
- ✅ **Exception Handling**: Comprehensive error handling with descriptive messages

## Repository Enhancements

### BookingRepository
**New Method Added**: `GetConflictingBookingsAsync(string providerId, DateTime startTime, DateTime endTime)`
- Scans for existing bookings by provider
- Excludes cancelled bookings
- Filters for time overlaps to prevent double-booking

## Exception Types Used

### NotFoundException
- Used when referenced entities (Profile, Business, Service) don't exist
- Provides clear error messages with entity type and ID

### ValidationException
- Used for business rule violations
- Used for data validation failures
- Provides descriptive error messages explaining the violation

## Benefits of This Implementation

1. **Data Integrity**: Ensures referential integrity in a NoSQL environment
2. **Business Logic Enforcement**: Prevents invalid business operations
3. **Clear Error Messages**: Provides meaningful feedback to API consumers
4. **Centralized Validation**: All validation logic is in the appropriate handlers
5. **Testability**: Each rule can be unit tested independently
6. **Maintainability**: Business rules are clearly documented in code

## Usage Examples

### Valid Business Creation
```csharp
// This will succeed if profile exists and is a provider
var command = new CreateBusinessCommand("provider-id", "My Business", "New York");
var business = await mediator.Send(command);
```

### Invalid Business Creation
```csharp
// This will throw NotFoundException if profile doesn't exist
var command = new CreateBusinessCommand("non-existent-id", "My Business", "New York");
// Throws: Profile with ID 'non-existent-id' not found.

// This will throw ValidationException if profile is not a provider
var command = new CreateBusinessCommand("client-id", "My Business", "New York");
// Throws: Profile with ID 'client-id' is not a provider and cannot create a business.
```

### Booking Conflict Prevention
```csharp
// If provider already has a booking from 2:00-3:00 PM
var command = new CreateBookingCommand("service-id", "client-id", "provider-id", 
    DateTime.Parse("2024-01-01 14:30:00"), DateTime.Parse("2024-01-01 15:30:00"));
// Throws: Provider 'provider-id' already has a booking during the requested time slot.
```

## Future Enhancements

1. **Business Hours Validation**: Check if booking time falls within business operating hours
2. **Advanced Conflict Detection**: Consider service-specific conflicts vs provider-wide conflicts
3. **Booking Capacity**: Support for multiple concurrent bookings per provider
4. **Time Zone Support**: Handle bookings across different time zones
5. **Recurring Bookings**: Support for recurring appointment patterns