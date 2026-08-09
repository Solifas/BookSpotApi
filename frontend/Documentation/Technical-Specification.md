
# HirePros - Technical Specification

## 1. System Architecture

### 1.1 High-Level Architecture
HirePros follows a modern web application architecture with the following components:

- **Frontend**: React-based single-page application (SPA)
- **Backend**: Supabase (PostgreSQL database with built-in API)
- **Authentication**: Supabase Auth
- **File Storage**: Supabase Storage
- **Hosting**: Lovable Platform (Vercel-based)
- **CDN**: Global content delivery network

### 1.2 Technology Stack

#### Frontend Technologies
- **Framework**: React 18.3.1
- **Build Tool**: Vite
- **Language**: TypeScript
- **Styling**: Tailwind CSS 3.x
- **UI Components**: shadcn/ui
- **Icons**: Lucide React
- **Routing**: React Router DOM 6.x
- **State Management**: TanStack Query (React Query)
- **Form Handling**: React Hook Form
- **Date Handling**: date-fns

#### Backend Technologies
- **Database**: PostgreSQL (via Supabase)
- **API**: Supabase Auto-generated REST API
- **Real-time**: Supabase Realtime
- **Authentication**: Supabase Auth
- **File Storage**: Supabase Storage
- **Edge Functions**: Supabase Edge Functions (Deno)

#### Development Tools
- **Version Control**: Git
- **Package Manager**: npm
- **Code Quality**: ESLint, TypeScript
- **CSS Framework**: Tailwind CSS
- **Component Library**: shadcn/ui

## 2. Database Design

### 2.1 Database Schema Overview
The application uses a PostgreSQL database with the following core tables:

#### Core Tables
- `profiles` - User accounts (providers and clients)
- `businesses` - Service provider business information
- `services` - Services offered by businesses
- `bookings` - Appointment bookings
- `business_hours` - Provider availability schedules
- `reviews` - Client reviews and ratings

### 2.2 Entity Relationship Diagram

```
profiles (1) ← (1) businesses (1) ← (M) services
    ↓                                      ↓
    └── (M) bookings (M) ──────────────────┘
         ↓
    (1) reviews
```

### 2.3 Detailed Table Specifications

#### profiles Table
```sql
CREATE TABLE profiles (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email TEXT UNIQUE NOT NULL,
  name TEXT NOT NULL,
  phone TEXT,
  avatar_url TEXT,
  user_type TEXT CHECK (user_type IN ('provider', 'client')) NOT NULL,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

#### businesses Table
```sql
CREATE TABLE businesses (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  provider_id UUID REFERENCES profiles(id) ON DELETE CASCADE NOT NULL,
  business_name TEXT NOT NULL,
  description TEXT,
  address TEXT,
  city TEXT,
  province TEXT,
  postal_code TEXT,
  country TEXT DEFAULT 'South Africa',
  phone TEXT,
  email TEXT,
  website TEXT,
  logo_url TEXT,
  is_active BOOLEAN DEFAULT true,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

#### services Table
```sql
CREATE TABLE services (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  business_id UUID REFERENCES businesses(id) ON DELETE CASCADE NOT NULL,
  name TEXT NOT NULL,
  description TEXT,
  duration_minutes INTEGER NOT NULL CHECK (duration_minutes > 0),
  price DECIMAL(10,2) NOT NULL CHECK (price >= 0),
  currency TEXT DEFAULT 'ZAR',
  category TEXT,
  is_active BOOLEAN DEFAULT true,
  image_url TEXT,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

#### bookings Table
```sql
CREATE TABLE bookings (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  service_id UUID REFERENCES services(id) ON DELETE CASCADE NOT NULL,
  client_id UUID REFERENCES profiles(id) ON DELETE CASCADE NOT NULL,
  provider_id UUID REFERENCES profiles(id) ON DELETE CASCADE NOT NULL,
  booking_date DATE NOT NULL,
  start_time TIME NOT NULL,
  end_time TIME NOT NULL,
  status TEXT CHECK (status IN ('pending', 'confirmed', 'completed', 'cancelled')) DEFAULT 'pending',
  total_price DECIMAL(10,2) NOT NULL,
  currency TEXT DEFAULT 'ZAR',
  notes TEXT,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

### 2.4 Database Indexes
Key indexes for performance optimization:

```sql
-- Profile indexes
CREATE INDEX idx_profiles_email ON profiles(email);
CREATE INDEX idx_profiles_user_type ON profiles(user_type);

-- Business indexes
CREATE INDEX idx_businesses_provider_id ON businesses(provider_id);
CREATE INDEX idx_businesses_city ON businesses(city);
CREATE INDEX idx_businesses_is_active ON businesses(is_active);

-- Service indexes
CREATE INDEX idx_services_business_id ON services(business_id);
CREATE INDEX idx_services_category ON services(category);
CREATE INDEX idx_services_is_active ON services(is_active);

-- Booking indexes
CREATE INDEX idx_bookings_service_id ON bookings(service_id);
CREATE INDEX idx_bookings_client_id ON bookings(client_id);
CREATE INDEX idx_bookings_provider_id ON bookings(provider_id);
CREATE INDEX idx_bookings_date_status ON bookings(booking_date, status);
```

## 3. API Design

### 3.1 API Architecture
The application uses Supabase's auto-generated REST API with the following characteristics:

- **Base URL**: `https://[project-id].supabase.co/rest/v1/`
- **Authentication**: JWT tokens via Supabase Auth
- **Content Type**: `application/json`
- **CORS**: Configured for frontend domain

### 3.2 Authentication Endpoints

#### Register User
```
POST /auth/v1/signup
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "securepassword"
}
```

#### Login User
```
POST /auth/v1/token?grant_type=password
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "securepassword"
}
```

### 3.3 Core API Endpoints

#### Profile Management
```
GET    /profiles                    # Get all profiles
GET    /profiles?id=eq.{id}        # Get specific profile
POST   /profiles                   # Create profile
PATCH  /profiles?id=eq.{id}        # Update profile
DELETE /profiles?id=eq.{id}        # Delete profile
```

#### Business Management
```
GET    /businesses                 # Get all businesses
GET    /businesses?provider_id=eq.{id}  # Get businesses by provider
POST   /businesses                 # Create business
PATCH  /businesses?id=eq.{id}      # Update business
DELETE /businesses?id=eq.{id}      # Delete business
```

#### Service Management
```
GET    /services                   # Get all services
GET    /services?business_id=eq.{id}    # Get services by business
GET    /services?category=eq.{category} # Get services by category
POST   /services                   # Create service
PATCH  /services?id=eq.{id}        # Update service
DELETE /services?id=eq.{id}        # Delete service
```

#### Booking Management
```
GET    /bookings                   # Get all bookings
GET    /bookings?client_id=eq.{id} # Get bookings by client
GET    /bookings?provider_id=eq.{id} # Get bookings by provider
POST   /bookings                   # Create booking
PATCH  /bookings?id=eq.{id}        # Update booking
DELETE /bookings?id=eq.{id}        # Cancel booking
```

### 3.4 Row Level Security (RLS)
Supabase RLS policies ensure data security:

```sql
-- Profiles: Users can only access their own profile
CREATE POLICY "Users can view their own profile" ON profiles
FOR SELECT USING (auth.uid() = id);

-- Businesses: Providers can manage their own businesses
CREATE POLICY "Providers can manage their businesses" ON businesses
FOR ALL USING (auth.uid() = provider_id);

-- Services: Providers can manage services for their businesses
CREATE POLICY "Providers can manage their services" ON services
FOR ALL USING (
  business_id IN (
    SELECT id FROM businesses WHERE provider_id = auth.uid()
  )
);

-- Bookings: Users can access their own bookings
CREATE POLICY "Users can access their bookings" ON bookings
FOR ALL USING (
  auth.uid() = client_id OR auth.uid() = provider_id
);
```

## 4. Frontend Architecture

### 4.1 Project Structure
```
src/
├── components/          # Reusable UI components
│   ├── ui/             # shadcn/ui components
│   ├── Navigation.tsx  # Navigation component
│   ├── FeatureCard.tsx # Feature card component
│   └── ...
├── pages/              # Page components
│   ├── Homepage.tsx    # Landing page
│   ├── Dashboard.tsx   # User dashboard
│   ├── Login.tsx       # Authentication pages
│   └── ...
├── contexts/           # React contexts
│   └── AuthContext.tsx # Authentication context
├── hooks/              # Custom hooks
│   └── use-toast.ts    # Toast notification hook
├── lib/                # Utility libraries
│   └── utils.ts        # Utility functions
├── data/               # Static data and types
│   └── servicesData.ts # Sample service data
└── types/              # TypeScript type definitions
```

### 4.2 Component Architecture

#### Component Hierarchy
```
App
├── Navigation
├── Router
│   ├── Homepage
│   │   ├── FeatureCard
│   │   ├── PricingCard
│   │   ├── LocationSearch
│   │   └── SearchResults
│   ├── Dashboard
│   │   ├── BusinessInsights
│   │   ├── CalendarView
│   │   ├── ClientList
│   │   └── ServiceManagement
│   └── AuthModal
└── Toaster
```

#### Component Design Principles
- **Single Responsibility**: Each component has one clear purpose
- **Reusability**: Components are designed to be reused across pages
- **Type Safety**: All components use TypeScript interfaces
- **Accessibility**: Components follow WCAG guidelines
- **Performance**: Components use React.memo where appropriate

### 4.3 State Management

#### Local State
- Component-level state using `useState`
- Form state using `react-hook-form`
- Modal and UI state managed locally

#### Global State
- Authentication state via `AuthContext`
- Server state via `TanStack Query`
- Toast notifications via `sonner`

#### Data Fetching
```typescript
// Example: Fetching services
const { data: services, isLoading, error } = useQuery({
  queryKey: ['services', businessId],
  queryFn: () => fetchServices(businessId),
  enabled: !!businessId
});
```

### 4.4 Routing Configuration
```typescript
const router = createBrowserRouter([
  {
    path: "/",
    element: <Homepage />
  },
  {
    path: "/dashboard",
    element: <Dashboard />,
    loader: requireAuth
  },
  {
    path: "/login",
    element: <Login />
  },
  {
    path: "/register",
    element: <Register />
  },
  {
    path: "*",
    element: <NotFound />
  }
]);
```

## 5. Security Considerations

### 5.1 Authentication Security
- JWT tokens with automatic refresh
- Secure password requirements (minimum 8 characters)
- Password hashing using bcrypt
- Session timeout after inactivity

### 5.2 Data Security
- Row Level Security (RLS) policies
- API rate limiting
- Input validation and sanitization
- XSS protection via React's built-in escaping

### 5.3 Privacy Compliance
- GDPR-compliant data handling
- User consent for data collection
- Right to data deletion
- Data minimization principles

## 6. Performance Optimization

### 6.1 Frontend Optimization
- Code splitting using React.lazy()
- Image optimization and lazy loading
- Bundle size optimization
- Caching strategies for API responses

### 6.2 Database Optimization
- Proper indexing on frequently queried columns
- Query optimization using Supabase's query builder
- Connection pooling
- Read replicas for scaling

### 6.3 Caching Strategy
- Browser caching for static assets
- API response caching using React Query
- CDN caching for global asset delivery

## 7. Deployment and Infrastructure

### 7.1 Deployment Architecture
- **Frontend**: Deployed on Lovable platform (Vercel-based)
- **Backend**: Supabase managed infrastructure
- **Database**: PostgreSQL on Supabase
- **CDN**: Global content delivery network

### 7.2 Environment Configuration
```typescript
// Environment variables
interface Environment {
  VITE_SUPABASE_URL: string;
  VITE_SUPABASE_ANON_KEY: string;
  VITE_APP_ENV: 'development' | 'staging' | 'production';
}
```

### 7.3 CI/CD Pipeline
- Automatic deployment on code push
- Build verification and testing
- Environment-specific configurations
- Rollback capabilities

## 8. Monitoring and Analytics

### 8.1 Application Monitoring
- Error tracking and reporting
- Performance monitoring
- User session recording
- API response time monitoring

### 8.2 Business Analytics
- User engagement metrics
- Booking conversion rates
- Provider performance analytics
- Revenue tracking

## 9. Testing Strategy

### 9.1 Testing Types
- **Unit Tests**: Component and utility function testing
- **Integration Tests**: API integration testing
- **End-to-End Tests**: User journey testing
- **Performance Tests**: Load and stress testing

### 9.2 Testing Tools
- Jest for unit testing
- React Testing Library for component testing
- Cypress for E2E testing
- Lighthouse for performance testing

## 10. Maintenance and Support

### 10.1 Code Maintenance
- Regular dependency updates
- Code review processes
- Documentation updates
- Refactoring for performance

### 10.2 Database Maintenance
- Regular backup procedures
- Performance monitoring
- Index optimization
- Data archiving strategies

### 10.3 Support Procedures
- Issue tracking and resolution
- User feedback collection
- Feature request management
- Emergency response procedures
