# Monorepo: Angular Frontend + .NET 8 API

This repository contains two main projects:

- `frontend/`: The frontend application built with Angular.
- `backend/`: The backend API built with .NET 8 (ASP.NET Core).

## Project Structure

```
/
├── frontend/   # Angular application
├── backend/    # .NET 8 Web API
├── .gitignore
└── README.md
```

## Requirements

- Node.js 18 or newer
- Angular CLI
- .NET 8 SDK

## Getting Started

### Frontend (Angular)

```bash
cd frontend
npm install
ng serve
```

The app will be available at `http://localhost:4200/`.

### Backend (.NET 8 API)

```bash
cd backend
dotnet restore
dotnet build
dotnet run
```

The API will be available at `https://localhost:5001/` or `http://localhost:5000/` by default.

## Environment Configuration

You can add environment-specific configuration files such as:

- `frontend/src/environments/environment.ts`
- `backend/appsettings.Development.json`

Make sure to keep sensitive data out of version control.

## Notes

- Keep Angular and .NET projects decoupled and modular.
- Use `proxy.conf.json` in Angular to redirect API requests to the .NET backend during development if needed.
