# HookRepair

### Configuración local

No guardar credenciales en `appsettings.json`. Configurar user-secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=hook_db;Username=<USUARIO>;Password=<CONTRASEÑA>;SSL Mode=Prefer" --project ./HookRepair.Api
dotnet user-secrets set "Auth:SigningKey" "<CLAVE-DE-32-O-MAS-CARACTERES>" --project ./HookRepair.Api
dotnet user-secrets set "Auth:AllowedOrigins:0" "https://localhost:4200" --project ./HookRepair.Api
```

El frontend debe conservar access JWT en memoria. El refresh token llega únicamente como cookie `HttpOnly`; nunca usar `localStorage` para tokens.
