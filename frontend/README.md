# Angular Frontend

This project is the frontend part of the application, built with [Angular](https://angular.io/).

## Available Scripts

### Install dependencies

```bash
npm install
```

### Run the app in development mode

```bash
ng serve
```

The app will be available at `http://localhost:4200/`.

## Build for production

```bash
ng build
```

The production files will be in the `dist/` directory.

## Environment configuration

You can set environment-specific variables in:

- `src/environments/environment.ts`
- `src/environments/environment.prod.ts`

Make sure to exclude sensitive data from version control.

## Notes

- Consider using a proxy (`proxy.conf.json`) to redirect API calls to the backend during development.
