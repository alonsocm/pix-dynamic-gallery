# Plan de migración: standby de Azure → Cloudflare Workers (repo nuevo)

> **Estado (2026-09-14): ✅ Migración completa.** Fases 1 y 2 desplegadas — repo `pix-app`
> (`D:\src\pix-app`), Worker en `https://pix-app.alonso-casmax.workers.dev` (subdominio custom aún
> sin definir, no bloqueante). `API_BASE_URL` cortado en Cloudflare Pages y confirmado en vivo.
> Container App y resource group de Azure borrados. Como paso adicional, `pix-dynamic-gallery` se
> redujo a solo lo que la cabina necesita (ver Paso 7 en `ESTADO_PROYECTO.md`) — la decisión de
> alcance original de más abajo ("este repo no se toca") quedó parcialmente superada por eso: el
> dominio (Domain/Application/Infrastructure del watcher) sigue intacto, pero Agenda/Finance/
> Inventory/EventTransactions y las acciones admin de Events/Photos se borraron de aquí.

> **Contexto:** el standby actual (`pix-gallery` en Azure Container Apps) quedó inutilizable por
> una suspensión de facturación de la suscripción Free Trial (ver git log / conversación de
> soporte del 2026-09-13). Se decidió no reintentar en Azure sino migrar a un modelo verdaderamente
> gratis-para-siempre (facturación por request, no por tiempo prendido) en **Cloudflare Workers**,
> el mismo proveedor que ya hospeda el frontend sin costo.
>
> **Decisión de alcance:** este repo (`pix-dynamic-gallery`) **no se toca** — sigue siendo la
> fuente de verdad del dominio (C#, EF Core, watcher, SignalR) y sigue corriendo tal cual en la
> cabina. El standby se reconstruye desde cero en un **repo nuevo**, en TypeScript, como espejo de
> solo la parte que no depende del filesystem local.

## Por qué el alcance es más grande de lo que parecía

`apiBaseUrl` (la URL que hoy apunta a Azure) no solo sirve la galería — también sirve **todo el
panel de administración** (agenda, finanzas, inventario), porque el frontend usa la misma base URL
para ambos. El standby real cubre:

| Área | Controller (fuente de verdad) | Tablas | Complejidad |
|---|---|---|---|
| Events | `EventsController.cs` | `Event` | CRUD |
| Photos | `PhotosController.cs` | `Photo` | CRUD + upload a R2 |
| Agenda | `AgendaController.cs` | `AgendaEntry`, `AgendaDeposit` | CRUD |
| Finance | `FinanceController.cs` | `FinanceSettings`, `GlobalExpense` | **Agregación real** (`GetFinanceDashboardQuery`, 115 líneas) |
| Inventory | `InventoryController.cs` | `PaperPurchase`, `UsbPurchase` | **Cálculo real** (`PaperStockCalculator`, `UsbStockCalculator`) |
| EventTransactions | `EventTransactionsController.cs` | `EventTransaction` | CRUD |

Total: 9 tablas, ~30 endpoints, 2 piezas de lógica no trivial.

## Decisiones a tomar antes de empezar

- [ ] **Nombre del repo nuevo** (propuesta: `pix-gallery-edge-api`).
- [ ] **Subdominio del Worker** — `api.somospix.com` ya es el túnel de la cabina, así que el Worker
      necesita otro nombre (propuesta: `gallery-api.somospix.com` o `standby.somospix.com`).
- [ ] Neon y R2 **no cambian** — el Worker apunta a la misma base de datos y el mismo bucket que ya
      usa la cabina. Solo se mueve la capa de cómputo, no los datos.
- [ ] Al terminar la Fase 2, borrar el Container App y el Resource Group `pix` en Azure para cerrar
      el riesgo de facturación por completo (no dejarlo "pausado").

## Fase 1 — Events + Photos (lo que ven los invitados)

**Objetivo:** sacar la galería del riesgo de Azure primero, porque es la parte crítica para un
evento en curso. Es la fase más simple: sin agregaciones, solo CRUD + upload.

### Endpoints a portar
- `POST /api/events` (admin)
- `GET /api/events/{slug}`
- `GET /api/events` (admin, listado)
- `PATCH /api/events/{id}/active` (admin)
- `GET /api/events/{eventId}/photos` (paginado — portar `PaginatedList`)
- `GET /api/events/{eventId}/photos/{photoId}`
- `POST /api/events/{eventId}/photos` (multipart, equivalente a `UploadCapturedPhotoCommand` sin el
  watcher)
- `POST /api/events/{eventId}/photos/delete` (admin)

### Tareas
1. Scaffold del repo: `npm create cloudflare@latest` con plantilla Hono + TypeScript.
2. Bindings en `wrangler.jsonc`:
   - `r2_buckets` → el mismo bucket de producción (nativo, sin SDK de S3).
   - Secretos: `DATABASE_URL` (Neon), `ADMIN_PASSWORD`.
3. Capa de datos: esquema Drizzle **solo** para `events` y `photos` (inspeccionar columnas reales
   en Neon antes de escribirlo — no inventar el esquema).
4. Middleware de auth: comparar header `X-Admin-Password` contra el secreto (igual que
   `AdminAuthAttribute.cs`, ~5 líneas en TS).
5. Middleware de CORS restringido a `https://app.somospix.com`.
6. Portar cada handler 1:1 usando el C# actual como spec (incluye las validaciones de
   `CreateEventCommandValidator` / `UploadCapturedPhotoCommandValidator` con Zod).
7. Deploy a un Worker Custom Domain (el subdominio decidido arriba).
8. **Prueba de paridad**: correr el Worker en paralelo al Azure standby (mientras siga vivo o con
   Render como puente temporal) y comparar respuesta por respuesta contra un evento de prueba, no
   contra datos reales.
9. Corte: actualizar `API_BASE_URL` en las env vars de Cloudflare Pages.
10. Dejar el Azure standby apagado (no borrado todavía) como red de seguridad unos días.

**Estimado:** 1–2 días — es CRUD conocido, sin lógica de negocio compleja.

## Fase 2 — Agenda + Finance + Inventory + EventTransactions (panel admin)

**Objetivo:** retirar Azure por completo. Es la fase de mayor riesgo porque hay lógica de cálculo
real (dinero e inventario), no solo passthrough a la base de datos.

### Endpoints a portar
Los ~24 restantes de `AgendaController`, `FinanceController`, `InventoryController` y
`EventTransactionsController`.

### Tareas
1. Extender el esquema Drizzle con las 7 tablas restantes.
2. Portar el CRUD simple (Agenda, EventTransactions, compras de inventario) — mismo patrón que
   Fase 1.
3. **Portar con cuidado extra** (esto es lo que puede salir mal si se apura):
   - `PaperStockCalculator` / `UsbStockCalculator` → escribir tests con fixtures conocidos antes de
     confiar en el resultado.
   - `GetFinanceDashboardQuery` → portar la agregación y comparar su salida contra la versión C#
     (mientras Azure siga vivo) para los eventos reales ya cerrados — si los números no cuadran
     centavo a centavo, no se corta.
4. Reusar el mismo Worker/dominio de la Fase 1 (agregar rutas, no un proyecto nuevo).
5. Corte final de `API_BASE_URL` (si Fase 1 ya cortó, aquí solo se agregan rutas nuevas al mismo
   Worker).
6. Borrar el Container App y el Resource Group `pix` en Azure.
7. Actualizar `README.md` / `ESTADO_PROYECTO.md` de este repo: reemplazar la sección de "Azure
   Container Apps" por la referencia al repo nuevo, y marcar `tools/azure-standby/` como histórico.

**Estimado:** 2–4 días — el tiempo extra es por las pruebas de paridad de finanzas/inventario, no
por el volumen de CRUD.

## Qué NO cambia en ningún momento
- El watcher de Sparkbooth, el hub de SignalR y `hubBaseUrl` — siguen 100% en la cabina, en .NET,
  sin tocar.
- Neon (base de datos) y R2 (storage) — mismos recursos, mismos datos, solo cambia quién les pega.
