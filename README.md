# BookSpot 📚

A modern appointment booking system built with .NET 8 and AWS Lambda, designed for service-based businesses to manage bookings, clients, and schedules efficiently.

## 🚀 Features

- **User Management**: Separate profiles for clients and service providers
- **Business Management**: Complete business profile setup with services and hours
- **Appointment Booking**: Easy-to-use booking system with availability checking
- **Review System**: Customer feedback and rating system
- **Real-time Availability**: Dynamic scheduling with conflict prevention
- **AWS Integration**: Serverless architecture with DynamoDB storage

## 🏗️ Architecture

- **Backend**: .NET 8 with AWS Lambda Functions
- **Database**: Amazon DynamoDB
- **API**: RESTful API with comprehensive endpoints
- **Local Development**: LocalStack for AWS service emulation

## 📋 Prerequisites

- .NET 8 SDK
- Docker Desktop
- AWS CLI (optional, for verification)
- Git

## 🛠️ Quick Start

### 1. Clone the Repository
```bash
git clone https://github.com/solifas/bookspot.git
cd bookspot
```

### 2. Start LocalStack (Local Development)
```bash
# Windows
.\scripts\start-localstack.ps1

# Linux/Mac
./scripts/start-localstack.sh
```

### 3. Run the API
```bash
cd BookSpot.API
dotnet run
```

### 4. Test the API
Use the included `BookSpot.http` file to test all endpoints, or visit the Swagger UI at `https://localhost:7071/swagger`

## 📁 Project Structure

```
BookSpot/
├── BookSpot.API/              # Main API project
│   ├── Controllers/           # API controllers
│   ├── Models/               # Data models
│   ├── Services/             # Business logic
│   └── Program.cs            # Application entry point
├── scripts/                  # Development scripts
│   ├── start-localstack.ps1  # Start LocalStack (Windows)
│   ├── stop-localstack.ps1   # Stop LocalStack (Windows)
│   ├── start-localstack.sh   # Start LocalStack (Linux/Mac)
│   └── stop-localstack.sh    # Stop LocalStack (Linux/Mac)
├── localstack-init/          # LocalStack initialization
│   └── 01-create-dynamodb-tables.sh
├── docker-compose.yml        # LocalStack configuration
├── BookSpot.http            # API test collection
└── LocalStack-Setup.md      # Detailed setup guide
```

## 🗄️ Database Schema

### Tables
- **profiles** - User profiles (clients and providers)
- **businesses** - Business information and settings
- **services** - Services offered by businesses
- **business_hours** - Operating hours configuration
- **bookings** - Appointment bookings
- **reviews** - Customer reviews and ratings

## 🔧 API Endpoints

### Profiles
- `GET /api/profiles/{id}` - Get user profile
- `POST /api/profiles` - Create user profile
- `PUT /api/profiles/{id}` - Update user profile

### Businesses
- `GET /api/businesses/{id}` - Get business details
- `POST /api/businesses` - Create business
- `PUT /api/businesses/{id}` - Update business

### Services
- `GET /api/businesses/{businessId}/services` - List services
- `POST /api/businesses/{businessId}/services` - Add service
- `PUT /api/services/{id}` - Update service

### Bookings
- `GET /api/bookings/client/{clientId}` - Get client bookings
- `GET /api/bookings/business/{businessId}` - Get business bookings
- `POST /api/bookings` - Create booking
- `PUT /api/bookings/{id}` - Update booking

### Reviews
- `GET /api/businesses/{businessId}/reviews` - Get business reviews
- `POST /api/reviews` - Create review

## 🧪 Testing

### Using HTTP File
The project includes a comprehensive `BookSpot.http` file with test requests for all endpoints. Use it with:
- Visual Studio Code (REST Client extension)
- Visual Studio
- JetBrains Rider
- Any HTTP client that supports `.http` files

### Manual Testing
1. Start LocalStack
2. Run the API
3. Use the HTTP file or Swagger UI to test endpoints
4. Verify data persistence by checking DynamoDB tables

## 🌍 Environment Configuration

### Development (LocalStack)
- **DynamoDB Endpoint**: `http://localhost:4566`
- **AWS Credentials**: `test` / `test`
- **Region**: `us-east-1`

### Production (AWS)
- Uses standard AWS Lambda and DynamoDB
- Configure through AWS credentials and environment variables

## 🚀 Deployment

### AWS Lambda Deployment
```bash
# Install Lambda tools
dotnet tool install -g Amazon.Lambda.Tools

# Deploy to AWS
cd BookSpot.API
dotnet lambda deploy-function
```

### Docker Deployment
```bash
# Build image
docker build -t bookspot-api .

# Run container
docker run -p 8080:8080 bookspot-api
```

## 🛠️ Development Workflow

1. **Start LocalStack**: `.\scripts\start-localstack.ps1`
2. **Run API**: `dotnet run` in BookSpot.API folder
3. **Test Endpoints**: Use `BookSpot.http` file
4. **Verify Data**: Check DynamoDB tables via AWS CLI
5. **Stop LocalStack**: `.\scripts\stop-localstack.ps1`

## 📚 Documentation

- [LocalStack Setup Guide](LocalStack-Setup.md) - Detailed local development setup
- [API Documentation](BookSpot.http) - Complete API test collection
- [AWS Lambda Documentation](https://docs.aws.amazon.com/lambda/)
- [DynamoDB Documentation](https://docs.aws.amazon.com/dynamodb/)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- Check the [LocalStack Setup Guide](LocalStack-Setup.md) for common issues
- Review the [API test collection](BookSpot.http) for usage examples
- Open an issue for bugs or feature requests

## 🏷️ Version

Current version: 1.0.0

---

Built with ❤️ using .NET 8 and AWS Lambda
