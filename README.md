# RefugioSafe – Prototipo funcional (Entregable #2, punto 4)

Solución de Visual Studio con la arquitectura del C4 (nivel 2):

| Contenedor del C4 | Proyecto | Tecnología |
|---|---|---|
| Web Application | `RefugioSafe.Web` | ASP.NET Core MVC (puerto 5200 / 7200) |
| API Application | `RefugioSafe.Api` | .NET Web API + JWT (puerto 5100) |
| Database | SQL Server LocalDB `RefugioSafeDb` | Entity Framework Core |
| Background Service | `RecordatoriosHostedService` (dentro de la API) | .NET HostedService |
| Sistema de correo / SMTP | `EmailNotificador` | System.Net.Mail |
| (compartido) | `RefugioSafe.Contracts` | DTOs entre Web y API |

## Cómo ejecutarlo
1. Requisitos: Visual Studio 2022 (carga de trabajo *ASP.NET y desarrollo web*), SDK de .NET 8 y SQL Server LocalDB (viene con Visual Studio).
2. Abrir `RefugioSafe.sln`.
3. Clic derecho en la solución → **Configurar proyectos de inicio** → *Varios proyectos de inicio* → `RefugioSafe.Api` = Iniciar y `RefugioSafe.Web` = Iniciar (la API debe ir primero). Luego F5.
4. La primera vez la API crea la base de datos y los datos de demostración sola (`EnsureCreated`, sin migraciones).

Usuarios de prueba: `admin` / `Admin123*` (Administrador) y `personal` / `Personal123*` (Personal del refugio).

## Historias de usuario → pantallas (Figma)
| HU | Dónde |
|---|---|
| HU-01 Registrar animal | Registrar animal (admin) |
| HU-02 Lista y filtro | Animales (filtro por estado) |
| HU-03 Registrar adoptante | Adoptantes → Registrar |
| HU-04 Iniciar adopción | Animales → *Iniciar adopción* / Solicitudes → *Iniciar proceso* |
| HU-05 Aprobar o rechazar | Solicitudes de adopción (Apto / No apto / Aprobar / Rechazar) |
| HU-06 Seguimiento | Seguimiento post-adopción |
| HU-07 Recordatorios | Notificaciones (+ servicio en segundo plano) |
| HU-08 Capacidad | Inicio (dona de capacidad; el admin puede cambiar el total) |

## Dónde está cada patrón
- **State**: `Domain/Estados.cs` (5 estados) y `Adopcion` en `Domain/Entidades.cs` (contexto). Aquí viven RN-03 y RN-04.
- **Observer**: `IAdopcionObserver` en `Application/Contratos.cs`; observadores en `Application/Observadores.cs`; el sujeto es `AdopcionService`. Se registran en `Program.cs`.
- **Strategy**: `INotificador` con `EmailNotificador` y `NotificadorMock` (`Infrastructure/Notificadores.cs`). Se elige en `appsettings.json` → `Notificaciones:Canal` (`Mock` o `Email`).
- **SOLID/DIP**: repositorios e interfaces en `Application/Contratos.cs`, resueltos con la inyección de dependencias de ASP.NET Core en `Program.cs` de la API.

## Demo sugerida (flujo principal)
1. Entrar como `admin` → Inicio muestra la capacidad.
2. Registrar animal → Registrar adoptante → *Iniciar adopción* sobre un animal Disponible.
3. Solicitudes: marcar **Apto** → **Aprobar**. Se crea el primer control (+15 días) y un aviso al adoptante.
4. Notificaciones → *Ejecutar verificación ahora*: genera el recordatorio de la vacuna de Rex (ya sembrada). Con `Canal = Mock` el correo solo se escribe en la consola de la API.

## Notas
- Para enviar correos reales: `Canal = "Email"` y completar `Notificaciones:Smtp` (en Gmail se necesita una contraseña de aplicación).
- Si usa otra versión de .NET, cambie `TargetFramework` y las versiones de los paquetes `Microsoft.EntityFrameworkCore.SqlServer` y `Microsoft.AspNetCore.Authentication.JwtBearer` en `RefugioSafe.Api.csproj` para que coincidan.
- Si la web no conecta con la API, revise `Api:BaseUrl` en `RefugioSafe.Web/appsettings.json`.
