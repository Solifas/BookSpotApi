# 🔄 Data Source Switch Implementation Guide

## ✅ **Implementation Complete**

I've successfully implemented a flexible data source switching system that allows you to toggle between **API** and **Mock Data** without breaking changes.

## 🎯 **How It Works**

### **1. Configuration System**
- **File**: `src/config/dataSource.ts`
- **Environment Variable**: `VITE_DATA_SOURCE_MODE`
- **Values**: `'api'` or `'mock'`

### **2. Data Source Adapter**
- **File**: `src/services/dataSourceAdapter.ts`
- **Purpose**: Routes all data requests to either API or Mock service
- **Logging**: Shows which data source is being used in console

### **3. Mock Data Service**
- **File**: `src/services/mockDataService.ts`
- **Features**: 
  - Realistic mock data
  - Simulated API delays
  - Optional error simulation
  - Proper data relationships

### **4. Visual Switcher (Development Only)**
- **Component**: `src/components/DataSourceSwitcher.tsx`
- **Location**: Bottom-right corner of the screen
- **Functionality**: Click to switch modes (reloads page)

## 🚀 **How to Use**

### **Method 1: Environment Variable**
```bash
# In your .env file
VITE_DATA_SOURCE_MODE=mock  # Use mock data
# or
VITE_DATA_SOURCE_MODE=api   # Use real API
```

### **Method 2: Visual Switcher**
1. Look for the **Data Source** panel in the bottom-right corner
2. Click **Mock Data** or **Live API** to switch
3. Page will reload automatically

### **Method 3: Runtime Switching (Development)**
```typescript
import { setDataSourceMode } from '../config/dataSource';

// Switch to mock data
setDataSourceMode('mock');

// Switch to API
setDataSourceMode('api');
```

## 📊 **What's Included**

### **Mock Data Available:**
- ✅ **8 Services** with realistic South African data
- ✅ **Multiple Businesses** with complete info
- ✅ **Booking Data** with different statuses
- ✅ **Dashboard Statistics** with calculated metrics
- ✅ **8 South African Cities** with service counts
- ✅ **Realistic Delays** (500ms) to simulate network
- ✅ **Error Simulation** (optional, 10% rate)

### **API Integration Ready:**
- ✅ All endpoints mapped to your OpenAPI spec
- ✅ Proper error handling
- ✅ TypeScript type safety
- ✅ TanStack Query integration
- ✅ Loading states and caching

## 🔧 **Updated Services**

All these services now use the DataSourceAdapter:
- ✅ `SearchService` - Service search and filtering
- ✅ `BusinessService` - Business management
- ✅ `BookingService` - Booking operations
- ✅ All custom hooks (`useServices`, `useBookings`, `useDashboard`, `useLocations`)

## 🎭 **Mock Data Features**

### **Realistic Data:**
```typescript
// Example mock service
{
  id: '1',
  name: 'Premium Men\'s Haircut',
  category: 'Hair Salon',
  price: 150,
  durationMinutes: 60,
  description: 'Professional men\'s haircut with styling and beard trim',
  tags: ['men', 'haircut', 'beard', 'styling'],
  isActive: true
}
```

### **Configurable Simulation:**
```typescript
// In src/config/dataSource.ts
mock: {
  simulateDelay: true,    // Add realistic delays
  delayMs: 500,          // 500ms delay
  simulateErrors: false, // Enable/disable errors
  errorRate: 0.1        // 10% error rate
}
```

## 🌐 **API Mode**

When `VITE_DATA_SOURCE_MODE=api`:
- All requests go to your backend API
- Uses `VITE_API_BASE_URL` for the base URL
- Proper error handling for network issues
- Real-time data updates

## 🧪 **Testing Both Modes**

### **Test Mock Mode:**
1. Set `VITE_DATA_SOURCE_MODE=mock`
2. Start your frontend: `npm run dev`
3. Navigate through the app - all data comes from mock service
4. Check browser console for `🎭 Using Mock data` logs

### **Test API Mode:**
1. Start your backend API server
2. Set `VITE_DATA_SOURCE_MODE=api`
3. Restart frontend
4. Navigate through the app - all data comes from your API
5. Check browser console for `🌐 Using API` logs

## 🔍 **Console Logging**

The system logs every data source call:
```
🎭 Using Mock data for searchServices
🌐 Using API for getProviderBookings
🎭 Using Mock data for getDashboardStats
```

## 📱 **Components Updated**

These components now work with both data sources:
- ✅ **Homepage** - Service listings
- ✅ **Dashboard** - Booking requests and stats
- ✅ **Search Results** - Service search
- ✅ **Location Search** - City autocomplete
- ✅ **Service Management** - CRUD operations
- ✅ **Calendar View** - Booking calendar

## 🚨 **Important Notes**

### **No Breaking Changes:**
- ✅ All existing components work unchanged
- ✅ All hooks have the same interface
- ✅ All TypeScript types remain the same
- ✅ TanStack Query integration preserved

### **Development vs Production:**
- **Development**: DataSourceSwitcher visible
- **Production**: DataSourceSwitcher hidden
- **Default Mode**: Mock (safe for development)

### **Environment Setup:**
```bash
# Development with mock data
VITE_DATA_SOURCE_MODE=mock

# Development with API
VITE_DATA_SOURCE_MODE=api
VITE_API_BASE_URL=http://localhost:5000

# Production (always API)
VITE_DATA_SOURCE_MODE=api
VITE_API_BASE_URL=https://your-api.com
```

## 🎯 **Next Steps**

1. **Test Mock Mode**: Start the app and verify mock data works
2. **Implement Missing API Endpoints**: Add the endpoints from your OpenAPI spec
3. **Test API Mode**: Switch to API mode and test with your backend
4. **Deploy**: Set production environment to API mode

## 🔧 **Troubleshooting**

### **Mock Data Not Loading:**
- Check `VITE_DATA_SOURCE_MODE=mock` in `.env`
- Look for console logs starting with `🎭`
- Verify no TypeScript errors in mock service

### **API Mode Not Working:**
- Check `VITE_DATA_SOURCE_MODE=api` in `.env`
- Verify `VITE_API_BASE_URL` is correct
- Ensure your backend API is running
- Look for console logs starting with `🌐`

### **Switcher Not Visible:**
- Only shows in development (`NODE_ENV=development`)
- Check bottom-right corner of the screen
- Refresh the page after environment changes

## 🎉 **Benefits**

- ✅ **Zero Breaking Changes** - Existing code works unchanged
- ✅ **Easy Testing** - Switch between modes instantly
- ✅ **Realistic Development** - Mock data simulates real API
- ✅ **Production Ready** - Seamless API integration
- ✅ **Type Safe** - Full TypeScript support
- ✅ **Developer Friendly** - Visual switcher and console logs

Your application now supports both mock and API data sources with a simple environment variable switch! 🚀