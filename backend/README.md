# .NET 8 Backend API

This project is the backend API built with [ASP.NET Core 8](https://learn.microsoft.com/en-us/aspnet/core/).

## Getting Started

### Restore dependencies

```bash
dotnet restore
```

### Build the project

```bash
dotnet build
```

### Run the API

```bash
dotnet run
```

By default, the API will be available at:
- `https://localhost:5001/`
- `http://localhost:5000/`

## Configuration

App settings can be configured in:

- `appsettings.json`
- `appsettings.Development.json` (excluded from version control)

## Notes

- Make sure to keep secrets and credentials out of version control.
- You can test the API with tools like Postman or Swagger.
