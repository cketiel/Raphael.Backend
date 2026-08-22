# Raphael.Backend — .NET 8

Núcleo del ecosistema Raphael (NEMT). Reglas globales: `../CLAUDE.md`.

## Rol
Fuente única de verdad del ecosistema: expone la API que consumen Desktop, Driver, Rider,
el Customer Service Bot y los integradores externos. Si un contrato cambia aquí, algo se rompe allá.

## Proyectos
- `Raphael.Api` — 31 controllers. Entrada: `Raphael.Api/Program.cs`
- `Raphael.Notification` — Business Events, reglas y canales de entrega (push)
- `Raphael.Shared` — Entities, DTOs (52), EF Core, servicios de dominio. **No lo consumen los clientes**

## Seguridad
- **JWT** → Desktop, Driver, Rider.
- **API Key** → integraciones. Atributo: `Raphael.Api/Attributes/IntegrationApiKeyAttribute.cs`
- Endpoint nuevo nace con `[Authorize]`. Anónimo exige justificación escrita.
- `appsettings.Production.json` no se lee ni se edita desde aquí.

## Contratos — regla dura
`Raphael.Shared/DTOs/*.cs` es la fuente de verdad. Desktop duplica 22 DTOs, Driver 6, Rider los
espeja en TypeScript. **Al tocar un DTO, una Entity o la firma de un controller, ejecutar
`/contract-impact <Símbolo>` antes de cerrar la tarea.** Ver `../_meta/CONTRACT_MAP.md`.

Drift abierto hoy: `ScheduleDto` → Desktop va 8 propiedades por detrás.

## Anclas
- Endpoints: `Raphael.Api/Controllers/`
- Esquema de datos: `Raphael.Shared/Entities/` + `Raphael.Shared/Persistence/Configurations/`
- Eventos de negocio: `Raphael.Shared/Catalog/BusinessEvents/`
- Reglas de notificación: `Raphael.Shared/Catalog/NotificationRules/`

## No leer
`Raphael.Shared/Migrations/` — **87 archivos, 2.4 MB**. Para conocer el esquema usa la Entity y su
Configuration, nunca la migración. Tampoco `bin/`, `obj/`, `*.user`.

## Comandos
- Build: `dotnet build Raphael.Backend.sln`
- Run API: `dotnet run --project Raphael.Api`
- Migración: `dotnet ef migrations add <Nombre> --project Raphael.Shared --startup-project Raphael.Api`
- Test: no hay proyecto de tests. Si un cambio toca lógica de estados de viaje o facturación, dilo.
