# Thesis Repository System - C# Backend

ASP.NET Core Web API backend for the Thesis Archiving and Research Publication Repository System.

## Prerequisites

- .NET 8.0 SDK or later
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code (optional)

## Setup Instructions

### 1. Install .NET SDK

Download and install from: https://dotnet.microsoft.com/download

Verify installation:
```bash
dotnet --version
```

### 2. Restore Dependencies

Navigate to the backend folder and run:
```bash
cd backend
dotnet restore
```

### 3. Database Configuration

The application uses SQL Server LocalDB by default. The connection string is in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ThesisRepositoryDb;Trusted_Connection=true;MultipleActiveResultSets=true"
}
```

**To use a different SQL Server instance:**

1. Open `appsettings.json`
2. Update the connection string, for example:
   ```json
   "DefaultConnection": "Server=YOUR_SERVER;Database=ThesisRepositoryDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=true"
   ```

### 4. Create Database

The database will be automatically created when you run the application. The seed data will include:

- 5 demo user accounts
- 5 sample theses

### 5. Run the Application

```bash
dotnet run
```

Or with hot reload:
```bash
dotnet watch run
```

The API will be available at:
- **HTTP:** http://localhost:5000
- **Swagger UI:** http://localhost:5000/swagger

### 6. Update Frontend Configuration

Make sure the frontend `.env` file points to the correct API URL:
```
VITE_API_URL=http://localhost:5000/api
```

## API Endpoints

### Authentication
- `POST /api/Auth/signin` - User login
- `POST /api/Auth/signup` - User registration
- `POST /api/Auth/signout` - User logout

### Users
- `GET /api/Users` - Get all users
- `GET /api/Users/{id}` - Get user by ID
- `PATCH /api/Users/{id}/status` - Update user status (approve/deactivate)

### Theses
- `GET /api/Theses` - Get all theses
- `GET /api/Theses/{id}` - Get thesis by ID
- `POST /api/Theses` - Create new thesis
- `PATCH /api/Theses/{id}` - Update thesis
- `DELETE /api/Theses/{id}` - Delete thesis
- `POST /api/Theses/upload-pdf` - Upload PDF file
- `GET /api/Theses/pdf/{fileId}` - Get PDF data

### Password Reset
- `POST /api/PasswordReset` - Create password reset request
- `GET /api/PasswordReset` - Get all reset requests
- `PATCH /api/PasswordReset/{id}` - Update reset request
- `DELETE /api/PasswordReset/{id}` - Delete reset request

## Default User Accounts

After database initialization, the following accounts are available:

| Email | Password | Role |
|-------|----------|------|
| admin@thesis.com | AdminPass123! | Administrator |
| faculty@thesis.com | password123 | Faculty |
| student@thesis.com | password123 | Student |
| uploader@thesis.com | password123 | Uploader |
| approver@thesis.com | password123 | Approver |

## Project Structure

```
backend/
├── Controllers/          # API Controllers
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── ThesesController.cs
│   └── PasswordResetController.cs
├── Data/                 # Database Context and Seed
│   ├── ApplicationDbContext.cs
│   └── DbInitializer.cs
├── DTOs/                 # Data Transfer Objects
│   ├── AuthDTOs.cs
│   └── ThesisDTOs.cs
├── Models/               # Database Models
│   ├── User.cs
│   ├── Thesis.cs
│   └── PasswordResetRequest.cs
├── Services/             # Business Logic
│   ├── AuthService.cs
│   ├── UserService.cs
│   ├── ThesisService.cs
│   └── PasswordResetService.cs
├── Program.cs            # Application entry point
├── appsettings.json      # Configuration
└── ThesisRepository.csproj
```

## Configuration

### JWT Settings

In `appsettings.json`:
```json
"JwtSettings": {
  "Secret": "YourSuperSecretKeyForThesisRepositorySystem2024!@#$%",
  "ExpiryInDays": 7
}
```

### CORS Settings

The API is configured to allow requests from:
- http://localhost:5173 (Vite default)
- http://localhost:3000 (React default)
- http://localhost:5174 (Vite alternative)

To add more origins, update `Program.cs`:
```csharp
policy.WithOrigins("http://localhost:5173", "YOUR_ADDITIONAL_ORIGIN")
```

## Troubleshooting

### Database Connection Issues

1. **LocalDB not installed:**
   - Install SQL Server Express with LocalDB
   - Or update connection string to use full SQL Server

2. **Connection string error:**
   - Verify server name matches your SQL Server instance
   - Check credentials if using SQL Server authentication

### Port Already in Use

If port 5000 is already in use, update in `Properties/launchSettings.json` or run:
```bash
dotnet run --urls "http://localhost:YOUR_PORT"
```

### CORS Errors

Ensure the frontend origin is added to CORS policy in `Program.cs`.

## Production Deployment

For production deployment:

1. Update `appsettings.Production.json` with production settings
2. Change JWT secret to a secure random string
3. Use a production SQL Server instance
4. Enable HTTPS
5. Update CORS to only allow your production domain
6. Publish the application:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

### Deploying to Render

1. Create a new Web Service on Render and connect it to this GitHub repository.
2. Set the build command to:
   ```bash
   dotnet publish -c Release -o publish
   ```
3. Set the start command to:
   ```bash
   dotnet ./publish/ThesisRepository.dll
   ```
4. Add required environment variables in the Render dashboard:
   - ConnectionStrings__DefaultConnection: your production SQL Server connection string
   - JwtSettings__Secret: a secure random JWT signing key
5. Render provides a PORT environment variable automatically; the app will bind to it.

Note: The uploads/ directory is ignored by .gitignore. For persistent file storage consider using an external object store (S3, DigitalOcean Spaces, etc.) or Render Volumes.

## License

Copyright © 2024 Thesis Repository System. All rights reserved.
