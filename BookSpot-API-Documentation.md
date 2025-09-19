# BookSpot API Documentation

## Overview
BookSpot is a comprehensive booking system API that connects service providers with clients. The API enables providers to manage their businesses, services, and schedules, while allowing clients to discover and book services.

## Base URL
- **Development**: `http://localhost:5000`
- **Production**: `https://api.bookspot.com`

## Authentication
The API uses JWT (JSON Web Token) for authentication. Include the token in the Authorization header:
```
Authorization: Bearer <your-jwt-token>
```

## API Endpoints

### Authentication Endpoints

#### POST /auth/register
Register a new user account.

**Request Body:**
```json
{
  "email": "john.doe@example.com",
  "fullName": "John Doe",
  "contactNumber": "+1 (555) 123-4567",
  "password": "SecurePass123!",
  "userType": "client"
}
```

**Response (200):**
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

#### POST /auth/login
Authenticate user and get JWT token.

**Request Body:**
```json
{
  "email": "john.doe@example.com",
  "password": "SecurePass123!"
}
```

**Response (200):**
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

### Profile Endpoints

#### GET /profiles/me
Get current authenticated user's profile.

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "email": "john.doe@example.com",
  "fullName": "John Doe",
  "contactNumber": "+1 (555) 123-4567",
  "userType": "client",
  "createdAt": "2024-01-01T10:00:00Z"
}
```

#### GET /profiles/{id}
Get user profile by ID (restricted access).

**Headers:** `Authorization: Bearer <token>`

### Business Endpoints

#### POST /businesses
Create a new business (providers only).

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "businessName": "Sunny Day Spa",
  "description": "A relaxing spa offering various wellness and beauty services",
  "address": "123 Main Street, Suite 100",
  "phone": "+1 (555) 123-4567",
  "email": "info@sunnydayspa.com",
  "city": "New York",
  "website": "https://www.sunnydayspa.com",
  "imageUrl": "https://example.com/spa-image.jpg",
  "isActive": true
}
```

**Response (201):**
```json
{
  "id": "business-123",
  "providerId": "provider-456",
  "businessName": "Sunny Day Spa",
  "description": "A relaxing spa offering various wellness and beauty services",
  "address": "123 Main Street, Suite 100",
  "phone": "+1 (555) 123-4567",
  "email": "info@sunnydayspa.com",
  "city": "New York",
  "website": "https://www.sunnydayspa.com",
  "imageUrl": "https://example.com/spa-image.jpg",
  "isActive": true,
  "rating": 0.0,
  "reviewCount": 0,
  "createdAt": "2024-01-01T10:00:00Z"
}
```

#### GET /businesses/{id}
Get business by ID.

**Response (200):**
```json
{
  "id": "business-123",
  "providerId": "provider-456",
  "businessName": "Sunny Day Spa",
  "city": "New York",
  "isActive": true,
  "createdAt": "2024-01-01T10:00:00Z"
}
```

#### GET /businesses/{id}/services
Get all services offered by a specific business.

**Parameters:**
- `id`: Business ID

**Response (200):**
```json
[
  {
    "id": "service-123",
    "businessId": "business-123",
    "name": "Deep Tissue Massage",
    "price": 85.50,
    "durationMinutes": 90,
    "createdAt": "2024-01-01T10:00:00Z"
  },
  {
    "id": "service-124",
    "businessId": "business-123",
    "name": "Swedish Massage",
    "price": 75.00,
    "durationMinutes": 60,
    "createdAt": "2024-01-01T11:00:00Z"
  }
]
```

**Response (404):**
```json
{
  "status": 404,
  "title": "Not Found",
  "detail": "Business with ID 'business-123' not found."
}
```

### Service Endpoints

#### GET /services
Get all available services.

**Response (200):**
```json
[
  {
    "id": "service-123",
    "businessId": "business-456",
    "name": "Deep Tissue Massage",
    "price": 85.50,
    "durationMinutes": 90,
    "createdAt": "2024-01-01T10:00:00Z"
  }
]
```

#### GET /services/search
Search services with filters.

**Query Parameters:**
- `name` (optional): Filter by service name (partial match)
- `city` (optional): Filter by business city (partial match)
- `minPrice` (optional): Minimum price filter
- `maxPrice` (optional): Maximum price filter
- `minDuration` (optional): Minimum duration in minutes
- `maxDuration` (optional): Maximum duration in minutes
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 20, max: 100)

**Example Request:**
```
GET /services/search?name=massage&city=new%20york&minPrice=50&maxPrice=100&page=1&pageSize=10
```

**Response (200):**
```json
[
  {
    "id": "service-123",
    "businessId": "business-456",
    "name": "Deep Tissue Massage",
    "price": 85.50,
    "durationMinutes": 90,
    "createdAt": "2024-01-01T10:00:00Z"
  },
  {
    "id": "service-124",
    "businessId": "business-457",
    "name": "Swedish Massage",
    "price": 75.00,
    "durationMinutes": 60,
    "createdAt": "2024-01-01T11:00:00Z"
  }
]
```

#### GET /services/{id}
Get service by ID.

**Response (200):**
```json
{
  "id": "service-123",
  "businessId": "business-456",
  "name": "Deep Tissue Massage",
  "price": 85.50,
  "durationMinutes": 90,
  "createdAt": "2024-01-01T10:00:00Z"
}
```

#### POST /services
Create a new service (business owners only).

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "businessId": "business-123",
  "name": "Deep Tissue Massage",
  "description": "A therapeutic massage targeting deep layers of muscle and connective tissue",
  "category": "Massage Therapy",
  "price": 85.50,
  "durationMinutes": 90,
  "imageUrl": "https://example.com/massage-image.jpg",
  "tags": ["therapeutic", "deep tissue", "relaxation"],
  "isActive": true
}
```

**Response (201):**
```json
{
  "id": "service-789",
  "businessId": "business-123",
  "name": "Deep Tissue Massage",
  "description": "A therapeutic massage targeting deep layers of muscle and connective tissue",
  "category": "Massage Therapy",
  "price": 85.50,
  "durationMinutes": 90,
  "imageUrl": "https://example.com/massage-image.jpg",
  "tags": ["therapeutic", "deep tissue", "relaxation"],
  "isActive": true,
  "createdAt": "2024-01-01T10:00:00Z"
}

### Booking Endpoints

#### POST /bookings
Create a new booking (clients only).

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "serviceId": "service-123",
  "startTime": "2024-01-15T14:00:00Z",
  "endTime": "2024-01-15T15:30:00Z"
}
```

**Response (201):**
```json
{
  "id": "booking-789",
  "serviceId": "service-123",
  "clientId": "client-456",
  "providerId": "provider-789",
  "startTime": "2024-01-15T14:00:00Z",
  "endTime": "2024-01-15T15:30:00Z",
  "status": "pending",
  "createdAt": "2024-01-01T10:00:00Z"
}
```

#### GET /bookings/{id}
Get booking by ID.

### Business Hours Endpoints

#### POST /business-hours
Set business hours (business owners only).

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "businessId": "business-123",
  "dayOfWeek": 1,
  "openTime": "09:00",
  "closeTime": "17:00",
  "isClosed": false
}
```

#### GET /business-hours/{id}
Get business hours by ID.

### Review Endpoints

#### POST /reviews
Create a review for a completed booking.

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "bookingId": "booking-123",
  "rating": 5,
  "comment": "Excellent service!"
}
```

#### GET /reviews/{id}
Get review by ID.

## Error Responses

### Validation Error (400)
```json
{
  "status": 400,
  "title": "Validation Error",
  "detail": "One or more validation errors occurred.",
  "errors": {
    "Email": ["Email is required.", "Email format is invalid."],
    "Password": ["Password must be at least 8 characters long."]
  }
}
```

### Unauthorized (401)
```json
{
  "status": 401,
  "title": "Unauthorized",
  "detail": "Invalid or missing JWT token."
}
```

### Forbidden (403)
```json
{
  "status": 403,
  "title": "Forbidden",
  "detail": "Insufficient permissions to access this resource."
}
```

### Not Found (404)
```json
{
  "status": 404,
  "title": "Not Found",
  "detail": "The requested resource was not found."
}
```

### Internal Server Error (500)
```json
{
  "status": 500,
  "title": "Internal Server Error",
  "detail": "An unexpected error occurred."
}
```

## Data Models

### User Types
- `client`: Can book services
- `provider`: Can create businesses and offer services

### Booking Status
- `pending`: Booking created, awaiting confirmation
- `confirmed`: Booking confirmed by provider
- `completed`: Service completed
- `cancelled`: Booking cancelled

### Day of Week
- `0`: Sunday
- `1`: Monday
- `2`: Tuesday
- `3`: Wednesday
- `4`: Thursday
- `5`: Friday
- `6`: Saturday

## Business Rules

### Registration
- Email must be unique
- Password must be at least 8 characters with complexity requirements
- Full name is required
- Contact number is optional
- User type must be "client" or "provider"

### Business Creation
- Only providers can create businesses
- Business name and city are required
- Providers can own multiple businesses

### Service Creation
- Only business owners can create services for their businesses
- Price must be positive
- Duration must be in 15-minute increments
- Duration cannot exceed 8 hours

### Booking Creation
- Only clients can create bookings
- Bookings must be at least 30 minutes in advance
- Booking times must be on 15-minute intervals
- No overlapping bookings for the same provider
- Booking duration must match service duration

### Business Hours
- Only business owners can set hours for their businesses
- Day of week must be 0-6
- Business hours cannot exceed 16 hours per day
- Time format must be HH:mm

## Rate Limiting
- Authentication endpoints: 5 requests per minute per IP
- General API endpoints: 100 requests per minute per authenticated user

## Pagination
For endpoints that return lists, use query parameters:
- `page`: Page number (default: 1)
- `limit`: Items per page (default: 20, max: 100)

## Filtering and Sorting
Available on list endpoints:
- `sortBy`: Field to sort by
- `sortOrder`: `asc` or `desc`
- Various filter parameters specific to each endpoint

## Webhooks
BookSpot supports webhooks for real-time notifications:
- Booking created/updated/cancelled
- Review submitted
- Business status changes

## SDK and Libraries
Official SDKs available for:
- JavaScript/TypeScript
- Python
- C#/.NET
- Java

## Support
- Documentation: https://docs.bookspot.com
- Support Email: support@bookspot.com
- Status Page: https://status.bookspot.com