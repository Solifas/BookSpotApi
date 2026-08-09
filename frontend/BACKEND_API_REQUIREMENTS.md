# 🚀 Backend API Requirements for Dashboard Frontend

## 📋 **Required Endpoints for Dashboard.tsx**

The updated Dashboard component requires these specific API endpoints to function properly:

### **1. Provider Bookings Endpoint**
```http
GET /bookings/provider/{providerId}?status={status}&startDate={date}&endDate={date}
```

**Parameters:**
- `providerId` (path, required): The provider's user ID
- `status` (query, optional): Filter by booking status (`pending`, `confirmed`, `completed`, `cancelled`)
- `startDate` (query, optional): Filter bookings from this date (ISO format)
- `endDate` (query, optional): Filter bookings until this date (ISO format)

**Response Format:**
```json
[
  {
    "id": "booking-123",
    "serviceId": "service-456",
    "clientId": "client-789",
    "providerId": "provider-101",
    "startTime": "2024-01-15T14:00:00Z",
    "endTime": "2024-01-15T15:30:00Z",
    "status": "pending",
    "createdAt": "2024-01-10T10:00:00Z",
    "service": {
      "id": "service-456",
      "name": "Premium Men's Haircut",
      "price": 150,
      "durationMinutes": 90
    },
    "client": {
      "id": "client-789",
      "fullName": "John Smith",
      "email": "john@example.com",
      "contactNumber": "+27 11 555 0123"
    },
    "business": {
      "id": "business-202",
      "businessName": "Premium Cuts Barber Shop",
      "city": "Johannesburg"
    }
  }
]
```

### **2. Dashboard Statistics Endpoint**
```http
GET /dashboard/provider/{providerId}/stats
```

**Parameters:**
- `providerId` (path, required): The provider's user ID

**Response Format:**
```json
{
  "todayBookings": 5,
  "weekBookings": 23,
  "totalClients": 156,
  "monthlyRevenue": 12500.00,
  "pendingBookings": 3,
  "confirmedBookings": 8
}
```

### **3. Update Booking Status Endpoint**
```http
PUT /bookings/{bookingId}
```

**Parameters:**
- `bookingId` (path, required): The booking ID to update

**Request Body:**
```json
{
  "id": "booking-123",
  "status": "confirmed"
}
```

**Response Format:**
```json
{
  "id": "booking-123",
  "serviceId": "service-456",
  "clientId": "client-789",
  "providerId": "provider-101",
  "startTime": "2024-01-15T14:00:00Z",
  "endTime": "2024-01-15T15:30:00Z",
  "status": "confirmed",
  "createdAt": "2024-01-10T10:00:00Z"
}
```

---

## 🔧 **Implementation Requirements**

### **Authentication & Authorization:**
- All endpoints require JWT Bearer token authentication
- Providers can only access their own bookings and statistics
- Validate that the authenticated user matches the `providerId` in requests

### **Error Handling:**
Return appropriate HTTP status codes:
- `200` - Success
- `400` - Bad Request (invalid parameters)
- `401` - Unauthorized (missing/invalid token)
- `403` - Forbidden (accessing other provider's data)
- `404` - Not Found (booking/provider doesn't exist)
- `500` - Internal Server Error

### **Data Relationships:**
Ensure proper database joins to include:
- Service details in booking responses
- Client information in booking responses
- Business information in booking responses

---

## 📊 **Database Schema Considerations**

### **Bookings Table:**
```sql
CREATE TABLE bookings (
    id VARCHAR(255) PRIMARY KEY,
    service_id VARCHAR(255) NOT NULL,
    client_id VARCHAR(255) NOT NULL,
    provider_id VARCHAR(255) NOT NULL,
    start_time DATETIME NOT NULL,
    end_time DATETIME NOT NULL,
    status ENUM('pending', 'confirmed', 'completed', 'cancelled') DEFAULT 'pending',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (service_id) REFERENCES services(id),
    FOREIGN KEY (client_id) REFERENCES profiles(id),
    FOREIGN KEY (provider_id) REFERENCES profiles(id)
);
```

### **Indexes for Performance:**
```sql
-- For provider bookings queries
CREATE INDEX idx_bookings_provider_status ON bookings(provider_id, status);
CREATE INDEX idx_bookings_provider_date ON bookings(provider_id, start_time);

-- For dashboard statistics
CREATE INDEX idx_bookings_provider_created ON bookings(provider_id, created_at);
```

---

## 🎯 **Sample Implementation (Node.js/Express)**

### **Provider Bookings Controller:**
```javascript
// GET /bookings/provider/:providerId
async function getProviderBookings(req, res) {
  try {
    const { providerId } = req.params;
    const { status, startDate, endDate } = req.query;
    
    // Verify authorization
    if (req.user.id !== providerId) {
      return res.status(403).json({ error: 'Access denied' });
    }
    
    let query = `
      SELECT 
        b.*,
        s.name as service_name, s.price as service_price, s.duration_minutes,
        p.full_name as client_name, p.email as client_email, p.contact_number,
        bus.business_name, bus.city
      FROM bookings b
      JOIN services s ON b.service_id = s.id
      JOIN profiles p ON b.client_id = p.id
      JOIN businesses bus ON s.business_id = bus.id
      WHERE b.provider_id = ?
    `;
    
    const params = [providerId];
    
    if (status) {
      query += ' AND b.status = ?';
      params.push(status);
    }
    
    if (startDate) {
      query += ' AND b.start_time >= ?';
      params.push(startDate);
    }
    
    if (endDate) {
      query += ' AND b.start_time <= ?';
      params.push(endDate);
    }
    
    query += ' ORDER BY b.start_time ASC';
    
    const bookings = await db.query(query, params);
    
    // Transform to expected format
    const formattedBookings = bookings.map(booking => ({
      id: booking.id,
      serviceId: booking.service_id,
      clientId: booking.client_id,
      providerId: booking.provider_id,
      startTime: booking.start_time,
      endTime: booking.end_time,
      status: booking.status,
      createdAt: booking.created_at,
      service: {
        id: booking.service_id,
        name: booking.service_name,
        price: booking.service_price,
        durationMinutes: booking.duration_minutes
      },
      client: {
        id: booking.client_id,
        fullName: booking.client_name,
        email: booking.client_email,
        contactNumber: booking.contact_number
      },
      business: {
        id: booking.business_id,
        businessName: booking.business_name,
        city: booking.city
      }
    }));
    
    res.json(formattedBookings);
  } catch (error) {
    console.error('Get provider bookings error:', error);
    res.status(500).json({ error: 'Internal server error' });
  }
}
```

### **Dashboard Statistics Controller:**
```javascript
// GET /dashboard/provider/:providerId/stats
async function getDashboardStats(req, res) {
  try {
    const { providerId } = req.params;
    
    // Verify authorization
    if (req.user.id !== providerId) {
      return res.status(403).json({ error: 'Access denied' });
    }
    
    const today = new Date();
    const startOfDay = new Date(today.setHours(0, 0, 0, 0));
    const startOfWeek = new Date(today.setDate(today.getDate() - today.getDay()));
    const startOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    
    // Get statistics with parallel queries
    const [
      todayBookings,
      weekBookings,
      totalClients,
      monthlyRevenue,
      pendingBookings,
      confirmedBookings
    ] = await Promise.all([
      // Today's bookings
      db.query(
        'SELECT COUNT(*) as count FROM bookings WHERE provider_id = ? AND start_time >= ?',
        [providerId, startOfDay]
      ),
      
      // This week's bookings
      db.query(
        'SELECT COUNT(*) as count FROM bookings WHERE provider_id = ? AND start_time >= ?',
        [providerId, startOfWeek]
      ),
      
      // Total unique clients
      db.query(
        'SELECT COUNT(DISTINCT client_id) as count FROM bookings WHERE provider_id = ?',
        [providerId]
      ),
      
      // Monthly revenue (completed bookings only)
      db.query(`
        SELECT COALESCE(SUM(s.price), 0) as revenue 
        FROM bookings b 
        JOIN services s ON b.service_id = s.id 
        WHERE b.provider_id = ? AND b.status = 'completed' AND b.start_time >= ?
      `, [providerId, startOfMonth]),
      
      // Pending bookings
      db.query(
        'SELECT COUNT(*) as count FROM bookings WHERE provider_id = ? AND status = "pending"',
        [providerId]
      ),
      
      // Confirmed bookings
      db.query(
        'SELECT COUNT(*) as count FROM bookings WHERE provider_id = ? AND status = "confirmed"',
        [providerId]
      )
    ]);
    
    res.json({
      todayBookings: todayBookings[0].count,
      weekBookings: weekBookings[0].count,
      totalClients: totalClients[0].count,
      monthlyRevenue: parseFloat(monthlyRevenue[0].revenue || 0),
      pendingBookings: pendingBookings[0].count,
      confirmedBookings: confirmedBookings[0].count
    });
  } catch (error) {
    console.error('Get dashboard stats error:', error);
    res.status(500).json({ error: 'Internal server error' });
  }
}
```

---

## ✅ **Testing Checklist**

### **Provider Bookings Endpoint:**
- [ ] Returns bookings for authenticated provider only
- [ ] Filters by status correctly (`pending`, `confirmed`, etc.)
- [ ] Filters by date range correctly
- [ ] Includes service, client, and business details
- [ ] Returns 403 for unauthorized access
- [ ] Handles empty results gracefully

### **Dashboard Statistics:**
- [ ] Calculates today's bookings correctly
- [ ] Calculates weekly bookings correctly
- [ ] Counts unique clients correctly
- [ ] Calculates monthly revenue from completed bookings only
- [ ] Counts pending and confirmed bookings correctly
- [ ] Returns 0 values for new providers with no data

### **Update Booking Status:**
- [ ] Updates booking status successfully
- [ ] Only allows provider to update their own bookings
- [ ] Validates status values
- [ ] Returns updated booking data
- [ ] Handles non-existent booking IDs

---

## 🚀 **Quick Start Implementation**

1. **Add these routes to your API:**
   ```javascript
   app.get('/bookings/provider/:providerId', authenticateToken, getProviderBookings);
   app.get('/dashboard/provider/:providerId/stats', authenticateToken, getDashboardStats);
   app.put('/bookings/:bookingId', authenticateToken, updateBookingStatus);
   ```

2. **Test with sample data:**
   - Create a few test bookings with different statuses
   - Verify the endpoints return expected data format
   - Test error cases (unauthorized access, invalid IDs)

3. **Frontend will automatically work** once these endpoints return the correct data format!

The Dashboard component is now fully integrated with your data source switching system and will work with both mock data (for development) and real API data (for production). 🎉