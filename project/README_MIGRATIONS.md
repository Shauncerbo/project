# Database Migrations Setup

This project uses Entity Framework Core migrations to automatically create and update the database.

## How It Works

1. **Automatic Migration**: When you run the application, it automatically:
   - Creates the database if it doesn't exist
   - Applies all pending migrations
   - Seeds initial data (admin user, roles, membership types)

2. **Database Initializer**: The `DatabaseInitializer` service handles:
   - Running migrations on startup
   - Seeding initial data

## Initial Setup

When you clone this project to a new laptop:

1. **Update Connection String**: Edit `MauiProgram.cs` and update the connection string:
   ```csharp
   const string localConnectionString =
       "Data Source=YOUR_SERVER_NAME\\SQLEXPRESS;Initial Catalog=GymCRM_DB;Integrated Security=True;Trust Server Certificate=True";
   ```

2. **Run the Application**: The database will be created automatically on first run.

## Default Login Credentials

- **Username**: `admin`
- **Password**: `adminpassword`
- **Role**: Admin

⚠️ **Important**: Change the admin password after first login!

## Seeded Data

The following data is automatically created:

- **Roles**: Admin, Staff, Trainer
- **Admin User**: username "admin", password "adminpassword"
- **Membership Types**: Basic (₱1,000), Premium (₱2,000), Gold (₱3,000)

## Creating New Migrations

If you add new models or change existing ones:

1. Install EF Core tools (if not already installed):
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. Create a new migration:
   ```bash
   dotnet ef migrations add MigrationName --project project
   ```

3. The migration will be applied automatically on next app startup.

## Manual Migration (if needed)

If you need to manually apply migrations:

```bash
dotnet ef database update --project project
```

