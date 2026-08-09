# JWT Claims Integration

## Overview
This document outlines how JWT claims have been integrated into the BookSpot application to automatically use the current logged-in user's context for creating bookings, businesses, services, and business hours.

## Key Changes Made

### 1. Business Creation (`CreateBusinessCommand`)
**Before**: Required `ProviderId` parameter
**After**: Automatically uses current user from JWT token

```csharp
// Old command
public record CreateBusinessCommand(string ProviderId, string BusinessName, string City) : IRequest<Business>;

// New command
public record CreateBusinessCommand(string BusinessName, string City) : IRequest<Business>;
```

**Security Enhancements**:
- ✅ Automatically validates user is authenticated
- ✅ Ensures only providers can create businesses
- ✅ Uses current user's ID from JWT claims
- ✅ Prevents users from creating businesses for other users

### 2. Service Creation (`CreateServiceCommand`)
**Enhanced with ownership validation**

**Security Enhancements**:
- ✅ Validates user is authenticated and is a provider
- ✅ Ensures user can only create services for their own businesses
- ✅ Prevents unauthorized service creation

### 3. Business Hours Creation (`CreateBusinessHourCommand`)
**Enhanced with ownership validation**

**Security Enhancements**:
- ✅ Validates user is authenticated and is a provider
- ✅ Ensures user can only create business hours for their own businesses
- ✅ Prevents unauthorized business hour modifications

### 4. Booking Creation (`CreateBookingCommand`)
**Before**: Required `ClientId` and `ProviderId` parameters
**After**: Automatically determines client and provider

```csharp
// Old command
public record CreateBookingCommand(string ServiceId, string ClientId, string ProviderId, DateTime StartTime, DateTime EndTime) : IRequest<Booking>;

// New command
public record CreateBookingCommand(string ServiceId, DateTime StartTime, DateTime EndTime) : IRequest<Booking>;
```

**Smart Logic**:
- ✅ Automatically uses current user as the client
- ✅ Automatically determines provider from the service's business
- ✅ Validates only clients can create bookings
- ✅ Calculates end time based on service duration
- ✅ Allows optional end time validation

## ClaimsService Integration

All handlers now use the `IClaimsService` to:

### Authentication Checks
```csharp
var currentUserId = _claimsService.GetCurrentUserId();
if (string.IsNullOrEmpty(currentUserId))
{
    throw new ValidationException("User must be authenticated to create a booking.");
}
```

### Role-Based Authorization
```csharp
// For provider-only operations
if (!_claimsService.IsProvider())
{
    throw new ValidationException("Only providers can create businesses.");
}

// For client-only operations
if (currentUser.UserType != "client")
{
    throw new ValidationException("Only clients can create bookings.");
}
```

### Ownership Validation
```csharp
// Ensure user owns the business
if (business.ProviderId != currentUserId)
{
    throw new ValidationException("You can only create services for your own businesses.");
}
```

## API Usage Examples

### Creating a Business (Provider)
```http
POST /api/businesses
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "businessName": "My Salon",
  "city": "New York"
}
```

### Creating a Service (Provider)
```http
POST /api/services
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "businessId": "business-123",
  "name": "Haircut",
  "price": 50.00,
  "durationMinutes": 60
}
```

### Creating a Booking (Client)
```http
POST /api/bookings
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "serviceId": "service-123",
  "startTime": "2024-01-15T14:00:00Z"
}
```

## Security Benefits

### 1. **Automatic User Context**
- No need to pass user IDs in requests
- Eliminates possibility of users impersonating others
- Reduces API surface area for potential attacks

### 2. **Role-Based Access Control**
- Providers can only perform provider operations
- Clients can only perform client operations
- Clear separation of concerns

### 3. **Ownership Validation**
- Users can only modify their own resources
- Prevents unauthorized access to other users' data
- Enforces proper business boundaries

### 4. **Simplified API Design**
- Cleaner request payloads
- Less room for user error
- More intuitive API usage

## Error Handling

### Authentication Errors
```json
{
  "error": "User must be authenticated to create a booking."
}
```

### Authorization Errors
```json
{
  "error": "Only providers can create businesses."
}
```

### Ownership Errors
```json
{
  "error": "You can only create services for your own businesses."
}
```

## JWT Token Requirements

For this integration to work properly, JWT tokens must contain:

```json
{
  "sub": "user-id-123",           // User ID (NameIdentifier claim)
  "email": "user@example.com",    // Email claim
  "user_type": "provider",        // Custom user_type claim
  "role": "Provider",             // Role claim
  "exp": 1640995200              // Expiration
}
```

## Future Enhancements

1. **Admin Override**: Allow admin users to perform operations on behalf of others
2. **Delegation**: Support for providers delegating access to staff members
3. **Multi-Business Support**: Enhanced support for providers with multiple businesses
4. **Audit Logging**: Track all operations with user context for compliance
5. **Rate Limiting**: Per-user rate limiting based on JWT claims