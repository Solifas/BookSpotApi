# 🔄 Mock Data Replacement Plan

## 📊 **Current Mock Data Usage Analysis**

### 1. **Services Data** (`src/data/servicesData.ts`)
- ❌ `mockServices` array with 8 hardcoded services
- ❌ `getServicesByType()`, `getServicesByLocation()`, `searchServices()` functions using mock data
- **Used in**: Homepage, SearchResults, BookingPage

### 2. **Dashboard Bookings** (`src/pages/Dashboard.tsx`)
- ❌ `bookingRequests` array with 3 hardcoded pending bookings
- ❌ `upcomingBookings` array with 2 hardcoded confirmed bookings
- ❌ `stats` object with hardcoded metrics

### 3. **Calendar Bookings** (`src/components/CalendarView.tsx`)
- ❌ `bookings` object with hardcoded daily bookings

### 4. **Location Data** (`src/components/LocationSearch.tsx`)
- ❌ `mockLocations` array with 8 hardcoded South African cities

---

## 🚀 **Backend API Extensions Needed**

### **1. Enhanced Booking Endpoints**
Add these endpoints to your API:

```yaml
# Get bookings by provider with status filter
/bookings/provider/{providerId}:
  get:
    parameters:
      - name: providerId
        in: path
        required: true
        schema:
          type: string
      - name: status
        in: query
        schema:
          type: string
          enum: [pending, confirmed, completed, cancelled]
      - name: startDate
        in: query
        schema:
          type: string
          format: date
      - name: endDate
        in: query
        schema:
          type: string
          format: date
    responses:
      200:
        description: Success
        content:
          application/json:
            schema:
              type: array
              items:
                $ref: '#/components/schemas/BookingWithDetails'

# Get bookings by client
/bookings/client/{clientId}:
  get:
    parameters:
      - name: clientId
        in: path
        required: true
        schema:
          type: string
    responses:
      200:
        description: Success
        content:
          application/json:
            schema:
              type: array
              items:
                $ref: '#/components/schemas/BookingWithDetails'

# Get provider dashboard stats
/dashboard/provider/{providerId}/stats:
  get:
    parameters:
      - name: providerId
        in: path
        required: true
        schema:
          type: string
    responses:
      200:
        description: Success
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/DashboardStats'
```

### **2. Location/City Endpoints**
```yaml
# Get available cities/locations
/locations/cities:
  get:
    responses:
      200:
        description: Success
        content:
          application/json:
            schema:
              type: array
              items:
                type: object
                properties:
                  city:
                    type: string
                  province:
                    type: string
                  serviceCount:
                    type: integer
```

### **3. New Schema Definitions**
```yaml
# Dashboard statistics
DashboardStats:
  type: object
  properties:
    todayBookings:
      type: integer
    weekBookings:
      type: integer
    totalClients:
      type: integer
    monthlyRevenue:
      type: number
    pendingBookings:
      type: integer
    confirmedBookings:
      type: integer

# Enhanced booking with all details
BookingWithDetails:
  type: object
  properties:
    id:
      type: string
    serviceId:
      type: string
    clientId:
      type: string
    providerId:
      type: string
    startTime:
      type: string
      format: date-time
    endTime:
      type: string
      format: date-time
    status:
      type: string
      enum: [pending, confirmed, completed, cancelled]
    createdAt:
      type: string
      format: date-time
    # Embedded details
    service:
      $ref: '#/components/schemas/Service'
    client:
      type: object
      properties:
        id:
          type: string
        fullName:
          type: string
        email:
          type: string
        contactNumber:
          type: string
    business:
      type: object
      properties:
        id:
          type: string
        businessName:
          type: string
        city:
          type: string
        address:
          type: string
```

---

## 🔧 **Frontend Code Updates**

### **1. Update Services Data File**
Replace `src/data/servicesData.ts`:

```typescript
// Remove mock data, keep only interfaces and utility functions
export interface Service {
  id: string;
  name: string;
  type: string;
  description: string;
  price: string;
  rating: number;
  reviewCount: number;
  location: string;
  availability: string;
  image: string;
  phone: string;
  email: string;
  tags: string[];
}

// Remove mockServices array and mock functions
// These will be replaced by API calls using TanStack Query
```

### **2. Update Homepage Component**
Replace mock service usage:

```typescript
// OLD: Using mock data
import { mockServices } from '../data/servicesData';

// NEW: Using TanStack Query
import { useSearchServices } from '../hooks/useServices';

const Homepage = () => {
  // Replace mock services with API call
  const { data: servicesData, isLoading } = useSearchServices({
    page: 1,
    pageSize: 8 // Get first 8 services for homepage
  });

  const services = servicesData?.services || [];

  return (
    // ... rest of component
    <div className="grid grid-cols-2 md:grid-cols-2 lg:grid-cols-4 gap-2 md:gap-4">
      {isLoading ? (
        // Loading skeleton
        Array.from({ length: 8 }).map((_, i) => (
          <div key={i} className="bg-slate-200 animate-pulse rounded-lg h-32" />
        ))
      ) : (
        services.map((service) => (
          // ... service card JSX
        ))
      )}
    </div>
  );
};
```

### **3. Update Dashboard Component**
Replace all mock data:

```typescript
// OLD: Mock data
const [bookingRequests, setBookingRequests] = useState([...]);

// NEW: TanStack Query hooks
import { useProviderBookings } from '../hooks/useBookings';
import { useDashboardStats } from '../hooks/useDashboard'; // New hook needed

const Dashboard = () => {
  const { user } = useAuth();
  
  // Get pending bookings
  const { data: pendingBookings = [] } = useProviderBookings(
    user?.id || '', 
    BookingStatus.PENDING
  );
  
  // Get confirmed bookings
  const { data: confirmedBookings = [] } = useProviderBookings(
    user?.id || '', 
    BookingStatus.CONFIRMED
  );
  
  // Get dashboard statistics
  const { data: stats } = useDashboardStats(user?.id || '');

  // ... rest of component
};
```

### **4. Update Calendar Component**
Replace mock bookings:

```typescript
// OLD: Mock bookings object
const bookings: Record<string, Booking[]> = { ... };

// NEW: TanStack Query with date filtering
import { useProviderBookings } from '../hooks/useBookings';

const CalendarView = () => {
  const [selectedDate, setSelectedDate] = useState<Date>(new Date());
  const { user } = useAuth();
  
  // Get bookings for selected date
  const { data: allBookings = [] } = useProviderBookings(user?.id || '');
  
  // Filter bookings by selected date
  const dayBookings = allBookings.filter(booking => {
    const bookingDate = new Date(booking.startTime);
    return format(bookingDate, 'yyyy-MM-dd') === format(selectedDate, 'yyyy-MM-dd');
  });

  // ... rest of component
};
```

### **5. Update Location Search Component**
Replace mock locations:

```typescript
// OLD: Mock locations array
const mockLocations = [...];

// NEW: API call for cities
import { useCities } from '../hooks/useLocations'; // New hook needed

const LocationSearch = ({ value, onChange, placeholder }: Props) => {
  const { data: cities = [] } = useCities();
  const [suggestions, setSuggestions] = useState<string[]>([]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const inputValue = e.target.value;
    onChange(inputValue);
    
    if (inputValue.length > 0) {
      const filtered = cities
        .map(city => `${city.city}, ${city.province}`)
        .filter(location =>
          location.toLowerCase().includes(inputValue.toLowerCase())
        );
      setSuggestions(filtered);
      setShowSuggestions(true);
    } else {
      setShowSuggestions(false);
    }
  };

  // ... rest of component
};
```

---

## 🎯 **New Hooks to Create**

### **1. Dashboard Stats Hook**
```typescript
// src/hooks/useDashboard.ts
export const useDashboardStats = (providerId: string) => {
  return useQuery({
    queryKey: ['dashboard', 'stats', providerId],
    queryFn: () => apiClient.getDashboardStats(providerId),
    enabled: !!providerId,
  });
};
```

### **2. Locations Hook**
```typescript
// src/hooks/useLocations.ts
export const useCities = () => {
  return useQuery({
    queryKey: ['locations', 'cities'],
    queryFn: () => apiClient.getCities(),
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
};
```

### **3. Enhanced Booking Hooks**
Update existing booking hooks to support the new endpoints.

---

## 📋 **Implementation Priority**

### **Phase 1: Critical (Implement First)**
1. ✅ **Services Search API** - Already implemented
2. 🔄 **Provider Bookings Endpoints** - Need to implement
3. 🔄 **Dashboard Stats Endpoint** - Need to implement
4. 🔄 **Update Homepage** - Replace mock services
5. 🔄 **Update Dashboard** - Replace mock bookings

### **Phase 2: Important**
1. 🔄 **Cities/Locations API** - For location search
2. 🔄 **Update Calendar Component** - Real booking data
3. 🔄 **Client Bookings Endpoint** - For client dashboard

### **Phase 3: Enhancement**
1. 🔄 **Real-time updates** - WebSocket for live booking updates
2. 🔄 **Caching optimization** - Better cache strategies
3. 🔄 **Offline support** - Service worker for offline functionality

---

## 🧪 **Testing Strategy**

1. **API Testing**: Test all new endpoints with Postman/Insomnia
2. **Frontend Testing**: Verify TanStack Query integration
3. **Error Handling**: Test network failures and loading states
4. **Performance**: Monitor query performance and caching
5. **User Experience**: Ensure smooth transitions from loading to data

---

## 📈 **Expected Benefits**

- ✅ **Real Data**: Live, up-to-date information
- ✅ **Better Performance**: Automatic caching and background updates
- ✅ **Scalability**: No hardcoded limits
- ✅ **User Experience**: Loading states and error handling
- ✅ **Maintainability**: Single source of truth (API)