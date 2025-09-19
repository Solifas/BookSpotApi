# CORS Configuration Guide for BookSpot API

## Problem Solved
The CORS (Cross-Origin Resource Sharing) error you encountered:
```
Access to fetch at 'http://localhost:5000/auth/login' from origin 'http://localhost:8080' has been blocked by CORS policy
```

This happens when your frontend application (running on `http://localhost:8080`) tries to make requests to your API (running on `http://localhost:5000`) without proper CORS configuration.

## Solution Implemented

### 1. **Flexible CORS Configuration**
The API now supports configuration-based CORS setup that works for both development and production environments.

### 2. **Development Configuration** (`appsettings.Development.json`)
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",    // React default
      "http://localhost:8080",    // Vue.js/Webpack dev server  
      "http://localhost:4200",    // Angular default
      "http://localhost:5173",    // Vite default
      "http://localhost:8000",    // Alternative dev server
      "https://localhost:3000",   // HTTPS versions
      "https://localhost:8080",
      "https://localhost:4200",
      "https://localhost:5173"
    ],
    "AllowCredentials": true,
    "AllowAllOriginsInDevelopment": true
  }
}
```

### 3. **Production Configuration** (`appsettings.json`)
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://www.yourdomain.com"
    ],
    "AllowCredentials": true,
    "AllowAllOriginsInDevelopment": false
  }
}
```

## How It Works

### Development Mode
- **Permissive**: When `AllowAllOriginsInDevelopment` is `true`, allows requests from any origin
- **Specific Origins**: Falls back to the configured origins list if needed
- **All Methods & Headers**: Allows any HTTP method and headers
- **Credentials**: Supports cookies and authentication headers

### Production Mode
- **Restricted**: Only allows requests from configured origins
- **Security**: Enforces strict CORS policy
- **Credentials**: Configurable credential support

## Configuration Options

### CORS Settings Explained

| Setting | Description | Development | Production |
|---------|-------------|-------------|------------|
| `AllowedOrigins` | List of allowed frontend URLs | Common dev ports | Your domain(s) |
| `AllowCredentials` | Allow cookies/auth headers | `true` | `true` |
| `AllowAllOriginsInDevelopment` | Bypass origin restrictions in dev | `true` | `false` |

### Common Frontend Ports Supported
- **React**: `http://localhost:3000`
- **Vue.js/Webpack**: `http://localhost:8080` ✅ (Your case)
- **Angular**: `http://localhost:4200`
- **Vite**: `http://localhost:5173`
- **Custom**: `http://localhost:8000`

## Testing the Fix

### 1. **Restart Your API**
```bash
dotnet run --project BookSpot.API
```

### 2. **Test from Your Frontend**
Your frontend on `http://localhost:8080` should now be able to make requests to `http://localhost:5000` without CORS errors.

### 3. **Verify CORS Headers**
Check the response headers in your browser's developer tools:
```
Access-Control-Allow-Origin: http://localhost:8080
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
Access-Control-Allow-Headers: *
Access-Control-Allow-Credentials: true
```

## Frontend Integration Examples

### JavaScript/Fetch
```javascript
// This should now work without CORS errors
fetch('http://localhost:5000/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  credentials: 'include', // Important for auth cookies
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'password123'
  })
})
.then(response => response.json())
.then(data => console.log(data));
```

### Axios
```javascript
// Configure axios with credentials
axios.defaults.withCredentials = true;

// Make requests
const response = await axios.post('http://localhost:5000/auth/login', {
  email: 'user@example.com',
  password: 'password123'
});
```

### Vue.js Example
```javascript
// In your Vue component
async login() {
  try {
    const response = await this.$http.post('http://localhost:5000/auth/login', {
      email: this.email,
      password: this.password
    });
    
    // Handle successful login
    this.token = response.data.token;
  } catch (error) {
    console.error('Login failed:', error);
  }
}
```

## Customizing for Your Frontend

### Add Your Custom Port
If your frontend runs on a different port, add it to `appsettings.Development.json`:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:8080",
      "http://localhost:YOUR_PORT",  // Add your port here
      // ... other origins
    ]
  }
}
```

### Production Deployment
Update `appsettings.json` with your production domain:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://your-frontend-domain.com",
      "https://www.your-frontend-domain.com"
    ],
    "AllowCredentials": true,
    "AllowAllOriginsInDevelopment": false
  }
}
```

## Security Considerations

### Development
- ✅ **Permissive**: Easy testing and development
- ⚠️ **Less Secure**: Allows requests from any origin when `AllowAllOriginsInDevelopment` is true

### Production
- ✅ **Secure**: Only allows requests from specified domains
- ✅ **Controlled**: Explicit origin whitelist
- ✅ **Credentials**: Secure handling of authentication

## Troubleshooting

### Still Getting CORS Errors?

1. **Check Configuration**: Ensure your frontend URL is in the `AllowedOrigins` list
2. **Restart API**: Restart the API server after configuration changes
3. **Clear Browser Cache**: Hard refresh your frontend application
4. **Check Network Tab**: Verify the preflight OPTIONS request is successful

### Common Issues

| Issue | Solution |
|-------|----------|
| CORS error persists | Add your frontend URL to `AllowedOrigins` |
| Credentials not working | Ensure `AllowCredentials` is `true` |
| OPTIONS request failing | Check that CORS middleware is before authentication |
| Production CORS issues | Update production `appsettings.json` with correct domains |

### Debug CORS
Enable detailed logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore.Cors": "Debug"
    }
  }
}
```

## Environment-Specific Behavior

### Development Environment
- Uses `appsettings.Development.json`
- More permissive CORS policy
- Allows all origins if configured
- Detailed logging available

### Production Environment
- Uses `appsettings.json`
- Strict CORS policy
- Only specified origins allowed
- Security-focused configuration

The CORS configuration is now properly set up to handle your frontend requests from `http://localhost:8080` and other common development ports, while maintaining security for production deployments.