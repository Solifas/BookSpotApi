# Registration Enhancement

## Overview
This document outlines the enhancements made to the user registration system in the BookSpot application, including the addition of required full name and optional contact number fields.

## Changes Made

### 1. Profile Entity Updates
**File**: `BookSpot.Domain/Entities/Profile.cs`

**New Fields Added**:
```csharp
[DynamoDBProperty]
public string FullName { get; set; } = string.Empty;

[DynamoDBProperty]
public string? ContactNumber { get; set; }
```

**Field Requirements**:
- ✅ **FullName**: Required field for user's complete name
- ✅ **ContactNumber**: Optional field for phone number (nullable)

### 2. RegisterCommand Updates
**File**: `BookSpot.Application/Features/Auth/Commands/RegisterCommand.cs`

**Updated Signature**:
```csharp
public record RegisterCommand(string Email, string FullName, string? ContactNumber, string Password, string UserType) : IRequest<AuthResponse>;
```

### 3. RegisterRequest DTO Updates
**File**: `BookSpot.Application/DTOs/Auth/RegisterRequest.cs`

**New Fields**:
```csharp
[Required]
public string FullName { get; set; } = string.Empty;

public string? ContactNumber { get; set; }
```

### 4. AuthResponse Updates
**File**: `BookSpot.Application/DTOs/Auth/AuthResponse.cs`

**Enhanced Response**:
```csharp
public string FullName { get; set; } = string.Empty;
public string? ContactNumber { get; set; }
```

### 5. Validation Rules
**File**: `BookSpot.Application/Features/Auth/Commands/RegisterCommandValidator.cs`

**Comprehensive Validation**:

#### Email Validation
- ✅ Required field
- ✅ Valid email format
- ✅ Maximum 100 characters

#### Full Name Validation
- ✅ Required field
- ✅ 2-100 characters length
- ✅ Only letters, spaces, hyphens, apostrophes, periods, commas, and parentheses
- ✅ Prevents injection attacks

#### Contact Number Validation (Optional)
- ✅ 10-20 characters when provided
- ✅ Numbers, spaces, hyphens, parentheses, periods, and plus sign only
- ✅ International format support (+1, country codes)

#### Password Validation
- ✅ Required field
- ✅ Minimum 8 characters
- ✅ Maximum 100 characters
- ✅ Must contain: lowercase, uppercase, digit, special character
- ✅ Allowed special characters: @$!%*?&

#### User Type Validation
- ✅ Required field
- ✅ Must be "client" or "provider"

## API Usage

### Registration Request
```http
POST /auth/register
Content-Type: application/json

{
  "email": "john.doe@example.com",
  "fullName": "John Doe",
  "contactNumber": "+1 (555) 123-4567",
  "password": "SecurePass123!",
  "userType": "client"
}
```

### Registration Response
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "email": "john.doe@example.com",
  "fullName": "John Doe",
  "contactNumber": "+1 (555) 123-4567",
  "userType": "client",
  "expiresAt": "2024-01-15T15:00:00Z"
}
```

### Login Response (Updated)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "email": "john.doe@example.com",
  "fullName": "John Doe",
  "contactNumber": "+1 (555) 123-4567",
  "userType": "client",
  "expiresAt": "2024-01-15T15:00:00Z"
}
```

## Validation Examples

### Valid Registration
```json
{
  "email": "jane.smith@example.com",
  "fullName": "Jane Smith-Johnson",
  "contactNumber": "+44 20 7946 0958",
  "password": "MySecure123!",
  "userType": "provider"
}
```

### Registration Without Contact Number
```json
{
  "email": "bob.wilson@example.com",
  "fullName": "Bob Wilson",
  "password": "StrongPass456@",
  "userType": "client"
}
```

### Validation Error Response
```json
{
  "status": 400,
  "title": "Validation Error",
  "detail": "One or more validation errors occurred.",
  "errors": {
    "FullName": ["Full name is required."],
    "Password": ["Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character (@$!%*?&)."],
    "ContactNumber": ["Contact number contains invalid characters. Only numbers, spaces, hyphens, parentheses, periods, and plus sign are allowed."]
  }
}
```

## Database Schema Impact

### DynamoDB Table: profiles
**New Attributes**:
- `FullName` (String) - Required
- `ContactNumber` (String) - Optional

**Migration Notes**:
- Existing profiles will have empty `FullName` fields
- Existing profiles will have null `ContactNumber` fields
- No data migration required for new fields
- Backward compatibility maintained

## Security Enhancements

### Input Sanitization
- **Full Name**: Restricted character set prevents XSS and injection attacks
- **Contact Number**: Numeric format validation prevents malicious input
- **Email**: Standard email validation with length limits

### Password Security
- **Complexity Requirements**: Enforces strong password policies
- **BCrypt Hashing**: Secure password storage with salt
- **Length Limits**: Prevents buffer overflow attacks

### Data Validation
- **Field Length Limits**: Prevents database overflow
- **Character Restrictions**: Reduces attack surface
- **Required Field Validation**: Ensures data completeness

## Business Benefits

### Enhanced User Experience
- **Complete Profiles**: Users provide comprehensive information
- **Contact Information**: Enables better communication between clients and providers
- **Professional Appearance**: Full names create more professional interactions

### Improved Business Operations
- **Customer Service**: Contact numbers enable direct communication
- **User Identification**: Full names improve user recognition
- **Marketing**: Complete profiles enable better customer segmentation

### Compliance and Trust
- **Data Completeness**: Ensures minimum required information
- **Professional Standards**: Meets business application standards
- **User Verification**: Full names aid in user verification processes

## Future Enhancements

### Profile Management
1. **Profile Update Endpoints**: Allow users to update their information
2. **Profile Picture**: Add avatar/profile image support
3. **Address Information**: Add location-based fields
4. **Social Media Links**: Professional profile enhancements

### Validation Improvements
1. **Phone Number Verification**: SMS verification for contact numbers
2. **Email Verification**: Email confirmation process
3. **Real-time Validation**: Client-side validation feedback
4. **International Support**: Enhanced international phone number validation

### Security Enhancements
1. **Two-Factor Authentication**: SMS or email-based 2FA
2. **Password Reset**: Secure password recovery process
3. **Account Lockout**: Brute force protection
4. **Audit Logging**: Track profile changes and access

## Testing Considerations

### Unit Tests
- Validator tests for all new validation rules
- Handler tests for profile creation with new fields
- Repository tests for saving/retrieving enhanced profiles

### Integration Tests
- End-to-end registration flow
- Authentication with enhanced profiles
- API contract validation

### Edge Cases
- Very long names (boundary testing)
- International phone numbers
- Special characters in names
- Empty optional fields