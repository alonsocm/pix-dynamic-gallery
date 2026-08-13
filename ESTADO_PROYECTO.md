# Estado del proyecto — Pix Dynamic Gallery

> Documento vivo para retomar el trabajo en otra sesión. Última actualización: 12 de agosto de 2026 (sesión Paso 3).

## 🎯 Para retomar rápido

**Paso 2 y Paso 3 (código/config) completos** ✅. Frontend en Cloudflare Pages, deployado y funcionando (`somospix.com`, vía `npx wrangler deploy` — el proyecto usa el flujo nuevo de Cloudflare, Workers unificado con Pages, no el clásico; ver `frontend/wrangler.jsonc`). R2 con Public Development URL activada. `tools/booth/env.production.example` ya tiene todos los valores no-secretos precargados (bucket, endpoint, URL pública, CORS) — solo faltan las 2 credenciales de R2, el connection string de Neon y el token del Tunnel (secretos, el usuario los tiene guardados aparte).

Quedan dos cosas para terminar el proyecto:
1. **Instalar/arrancar todo en la PC de la cabina** con `tools/booth/` (requiere estar físicamente ahí — ver `tools/booth/README.md` para el paso a paso completo).
2. **Paso 4** — prueba real: crear un evento y probarlo desde un celular con datos móviles (no WiFi local).

Repo: **https://github.com/alonsocm/pix-dynamic-gallery** (público, rama `main`)

---

## ✅ Ya hecho

### Backend (.NET 9, Clean Architecture) — `src/`

- 4 proyectos: `Domain`, `Application`, `Infrastructure`, `Api`.
- EF Core + PostgreSQL, con migración inicial aplicada.
- **`SparkboothWatcherService`** ([Infrastructure/Watcher/](src/PixDynamicGallery.Infrastructure/Watcher/SparkboothWatcherService.cs)): `FileSystemWatcher` por evento activo + **fallback de polling** cada `Watcher:RefreshIntervalSeconds` (default 15s) — necesario porque Docker Desktop/WSL2 no propaga eventos `inotify` para bind-mounts de carpetas de Windows.
- `IStorageService` con implementaciones S3 (MinIO en local, compatible con R2/AWS real) y Azure Blob — intercambiable por config, sin tocar código.
- `EventHub` (SignalR): grupos por `eventId`, eventos `OnPhotoUploaded`/`OnPhotoFailed`.
- REST: `EventsController` (crear evento, resolver por slug), `PhotosController` (listado paginado, detalle, upload manual — mismo pipeline que el watcher).
- Middleware de errores → RFC 7807 `ProblemDetails`.
- Swagger en Development (`http://localhost:8080/swagger`).

### Frontend (Angular 22 — standalone, signals, zoneless) — `frontend/`

- Tailwind CSS v4.
- Rutas: `/` (home/buscador), `/kiosk/:eventId`, `/e/:eventId/p/:photoId`, `/e/:eventId/wall`, `/admin/events/new`, `/not-found`.
- `core/`: modelos TS, `ApiClient`, `AppConfigService` (URLs vía `env.js` en runtime), `EventService` + resolvers, `SignalRService` (con reconexión + re-join automático al grupo).
- `features/kiosk`: última foto en vivo (con fallback REST al cargar/refrescar) + QR generado 100% client-side (`qrcode` npm).
- `features/guest`: foto HD, descarga, Web Share API con fallback "copiar link".
- `features/wall`: muro tipo Pinterest (CSS columns), paginado + merge en tiempo real, `@defer` por foto.
- `features/admin/create-event`: formulario de creación de eventos con toggle Docker/Nativa que precompleta la ruta correcta.
- PWA instalable (manifest + service worker).
- Docker: `Dockerfile` multi-stage + Nginx, config runtime vía `env.js` regenerado por `envsubst` al arrancar el contenedor.

### Infraestructura local — `docker-compose.yml`

4 servicios: `postgres`, `minio` (+ `minio-init`), `api`, `frontend`. La API monta `C:\SparkboothPhotos` → `/data/sparkbooth`.

**Cómo levantar/bajar todo:**
```powershell
cd D:\src\pix-dynamic-gallery
docker compose up -d              # levantar
docker compose up --build -d      # levantar reconstruyendo (tras cambios de código)
docker compose ps                 # ver estado
docker compose logs -f api        # logs en vivo
docker compose stop               # detener (conserva datos)
docker compose down               # apagar y borrar contenedores (datos sobreviven en volúmenes)
```

**URLs locales:**
| Qué | URL |
|---|---|
| Frontend | http://localhost:4200 |
| API / Swagger | http://localhost:8080/swagger |
| Consola MinIO | http://localhost:9001 (`minioadmin`/`minioadmin`) |

### Herramientas de prueba — `tools/`

- `smoke-test.ps1` — crea evento, sube foto, verifica que aparece.
- `signalr-test-client.html` — cliente mínimo para ver eventos SignalR en vivo.
- `sample.jpg` — imagen válida mínima para pruebas.

### Git / GitHub

- Repo público creado y pusheado: https://github.com/alonsocm/pix-dynamic-gallery
- `.gitignore` cubre `bin/`, `obj/`, `node_modules/`, `dist/`, `.angular/`, `.claude/` — sin secretos commiteados.

### Bugs reales encontrados y corregidos durante el desarrollo

1. CORS de Development demasiado estricto → permisivo (`SetIsOriginAllowed`) solo en Dev.
2. EF Core generaba `UPDATE` en vez de `INSERT` para fotos nuevas (faltaba `context.Photos.Add()` explícito — el tracking por fixup de colección no alcanzaba porque el Id ya venía seteado client-side).
3. `EventDto` no exponía `guestBaseUrl` — necesario para que el kiosk arme la URL del QR igual que el backend.
4. Dockerfile del frontend: intentaba crear un usuario `app` que ya existía en la imagen base de Nginx.
5. Nginx no-root no podía escribir `env.js` en runtime — faltaba `--chown=nginx:nginx` en el `COPY`.
6. **Docker Desktop + WSL2 no propaga `inotify`** para bind-mounts de Windows → se agregó el fallback de polling en el watcher (con dedupe atómico vía `ConcurrentDictionary.TryAdd`).
7. Dos eventos de prueba (`demo`, `xv-angie`) quedaron con ruta de Windows en vez de ruta de contenedor — mismo error dos veces, confirma que es un patrón real de confusión (por eso el formulario de admin tiene el toggle Docker/Nativa).
8. El kiosk no mostraba nada al abrir/refrescar si no llegaba una foto en vivo justo en ese momento — se agregó un fetch REST inicial (`GetEventPhotos` página 1) como estado semilla.
9. `ExceptionHandlingMiddleware` perdía el campo `errors` en **todas** las respuestas 400 de validación — un `switch` de C# infería el tipo común `ProblemDetails` (no `ValidationProblemDetails`), y `WriteAsJsonAsync<T>` serializaba según ese tipo estático, no el de runtime. Se arregló pasando `problemDetails.GetType()` explícitamente.
10. `S3StorageService.BuildPublicUrl` armaba siempre `{host}/{bucket}/{key}` (path-style, correcto para MinIO) — pero Cloudflare R2 sirve las fotos sin el nombre del bucket en la ruta (tanto vía `r2.dev` como dominio custom), así que las URLs de fotos hubieran salido rotas en producción. Se agregó `Storage:AwsS3:PublicUrlBase` como override sin segmento de bucket.
11. Cloudflare Pages (hosting 100% estático) no replica el `try_files ... /index.html` de `nginx.conf` → sin un `_redirects` propio, un refresh en `/kiosk/:eventId` o abrir un link de invitado directo hubiera dado 404 en producción.
12. El proyecto de Cloudflare resultó ser del flujo nuevo (Workers unificado con Pages, deploy vía `npx wrangler deploy`), no el clásico de Pages — hacía falta `frontend/wrangler.jsonc` con `assets.directory` + `not_found_handling`, que no estaba contemplado originalmente. Al agregarlo junto con el `_redirects` del punto 11, **el deploy real falló** con "Infinite loop detected" — los dos mecanismos de SPA fallback (el nativo de Wrangler y el clásico de `_redirects`) competían entre sí. Se quitó `_redirects`, dejando solo `not_found_handling` de `wrangler.jsonc`. También el `name` de `wrangler.jsonc` no coincidía con el nombre real del proyecto (`pix-gallery`) — Wrangler lo advertía y ofrecía abrir un PR automático para "corregirlo"; se ajustó a mano.

---

## 🔲 Pendiente

### Decisión de arquitectura para producción (ya tomada, falta ejecutar)

**Costo objetivo: $0/mes** (+ dominio opcional ~$10/año).

| Pieza | Dónde | Por qué (resumen) |
|---|---|---|
| Frontend | **Cloudflare Pages** | CDN gratis sin límite de banda, auto-deploy desde GitHub |
| API + Watcher | **Nativo en la PC de la cabina** | El watcher necesita el filesystem local — no puede estar en la nube separado sin agregar un agente nuevo |
| Exponer la API a internet | **Cloudflare Tunnel** | Gratis, sin port-forwarding, funciona detrás de cualquier NAT/WiFi de venue, soporta WebSockets (SignalR) |
| Base de datos | **Neon** (Postgres serverless) | Free tier generoso, se duerme sin actividad (irrelevante para uso por evento) |
| Storage de fotos | **Cloudflare R2** | S3-compatible (mismo código que MinIO), free tier, y **sin costo de egress** — clave porque los invitados descargan/ven fotos repetidamente |

Ver la conversación completa para el razonamiento detallado de cada elección (alternativas consideradas: Vercel/Netlify, ngrok, AWS S3 real).

### Paso 1 — Cuentas (usuario) — ✅ **COMPLETO**

- [x] Cuenta Cloudflare (gratis)
- [x] Cuenta Neon (gratis)
- [x] Dominio: se decidió arrancar con URLs gratuitas (`*.pages.dev`, `*.trycloudflare.com`); dominio propio queda como mejora futura opcional, sin cambios de código.

### Dominio

- **`somospix.com`** — comprado en Cloudflare Registrar (~$10/año). Necesario porque los Tunnels nombrados de Cloudflare no ofrecen URL gratuita estable (a diferencia de Pages con `*.pages.dev`); la alternativa sin dominio (Quick Tunnel `*.trycloudflare.com`) cambia de URL en cada reinicio de `cloudflared`, inviable para una cabina que se prende/apaga por evento.
- Uso planeado de subdominios: `api.somospix.com` → API (Tunnel), `somospix.com` (raíz) → frontend (Pages).

### Paso 2 — Provisionar recursos (usuario, con guía de Claude) — **EN PROGRESO**

- [x] Neon: proyecto `pix-dynamic-gallery` creado → connection string guardado por el usuario (no compartido en el chat)
- [x] Cloudflare R2: bucket **`pix`** creado (cuenta `0c45f3c5ad49b738624be15c33e99444`) → API token (Account API Token, Object Read & Write, restringido al bucket) generado → Access Key ID + Secret Access Key guardados por el usuario (no compartidos en el chat) → **Public Development URL activada**: `https://pub-a265008846d646b3b90185cf6a5efe9e.r2.dev` (ya precargada en `tools/booth/env.production.example`)
- [x] Cloudflare Tunnel: túnel `pix` creado en Zero Trust → Networks → Tunnels. Public Hostname configurado vía la pestaña **"Published application routes"** (el dashboard renombró "Public Hostname" → eso; "Hostname routes" es para redes privadas/WARP, no sirve): `api.somospix.com` → `HTTP` → `localhost:8080`. Token de `cloudflared` pendiente de usar en la PC de la cabina (Paso 3). Status "Inactive" es normal hasta que `cloudflared` corra ahí.
- [x] Cloudflare Pages: proyecto conectado al repo `alonsocm/pix-dynamic-gallery` en GitHub, dominio custom `somospix.com` agregado. Build configurado: **Root directory** `frontend`, **Build command** `npm run build -- --configuration=production`. Env vars `API_BASE_URL`/`HUB_BASE_URL` = `https://api.somospix.com`. Deploy real **funcionando** (ver Paso 3 para el detalle de por qué costó dos intentos).

**Paso 2 → ✅ COMPLETO.**

### Paso 3 — Código para producción — ✅ **COMPLETO** (código en `main`, deploy de Pages confirmado funcionando)

- [x] `Storage:AwsS3` ya soportaba apuntar a cualquier endpoint S3-compatible (mismo patrón que MinIO) — **bug real encontrado y arreglado**: `BuildPublicUrl` siempre armaba `{host}/{bucket}/{key}` (path-style), pero R2 (tanto la Public Development URL como un dominio custom) sirve las fotos como `{host}/{key}`, **sin** el nombre del bucket en la ruta. Se agregó `Storage:AwsS3:PublicUrlBase` ([StorageOptions.cs](src/PixDynamicGallery.Infrastructure/Storage/StorageOptions.cs), [S3StorageService.cs](src/PixDynamicGallery.Infrastructure/Storage/S3StorageService.cs)) para ese caso; con él seteado, tiene prioridad sobre `PublicServiceUrl`.
  - Nota: en R2 el ACL por-objeto (`Storage:PublicRead`) se acepta pero no hace nada — el acceso público es un toggle a nivel bucket en el dashboard de R2.
  - `Storage:AwsS3:Region` debe ser el string literal `"auto"` para R2 (no es una región real, pero el SDK de AWS necesita algo ahí).
- [x] R2: bucket **`pix`**, Public Development URL activada → `https://pub-a265008846d646b3b90185cf6a5efe9e.r2.dev`. Ya precargado en `tools/booth/env.production.example` junto con `BucketName`/`ServiceUrl` (ambos no-secretos); solo faltan las 2 credenciales reales al llenar `.env.production` en la cabina.
- [x] `Cors:AllowedOrigins` — no requirió cambio de código (ya viene de config/env vars); el valor real (`https://somospix.com`, `https://www.somospix.com`) se setea en `tools/booth/.env.production`, no está hardcodeado en el repo.
- [x] Mecanismo de `env.js` adaptado al build de Cloudflare Pages, que es hosting 100% estático (sin entrypoint tipo Docker/Nginx para sustituir en runtime): nuevo hook `prebuild` en [package.json](frontend/package.json) corre [frontend/scripts/generate-env.mjs](frontend/scripts/generate-env.mjs), que hornea `API_BASE_URL`/`HUB_BASE_URL` en `public/env.js` **en build time**, leyendo el mismo `docker/env.template.js` que ya usaba el mecanismo Docker (ese sigue intacto, sin cambios, para docker-compose/self-host).
- [x] Script de arranque de la cabina: [tools/booth/start-booth.ps1](tools/booth/start-booth.ps1) — carga secretos desde `.env.production` (gitignored, plantilla en [tools/booth/env.production.example](tools/booth/env.production.example)) y levanta la API nativa (`dotnet PixDynamicGallery.Api.dll`) + `cloudflared tunnel run --token ...` en ventanas separadas.
- [x] Instrucciones de instalación en la PC de la cabina: [tools/booth/README.md](tools/booth/README.md) — instalar **.NET 9 Runtime** (no el SDK), `cloudflared`, `dotnet publish` + copiar salida a `tools/booth/api/` (sin clonar el repo completo), llenar `.env.production`, correr el script. Incluye sección de modo kiosk del navegador y troubleshooting.
- [x] Modo kiosk documentado en el mismo README: `chrome.exe --kiosk https://somospix.com/kiosk/<eventId>`, deshabilitar salvapantallas/suspensión.

**El deploy de Pages resultó más enredado de lo previsto** (ver bugs #11 y #12 más arriba): el proyecto usa el flujo nuevo de Cloudflare (Workers unificado con Pages, `npx wrangler deploy`), no el clásico con un campo simple de "output directory". Hubo que agregar [frontend/wrangler.jsonc](frontend/wrangler.jsonc) y, en el primer intento real, el `_redirects` clásico chocó con el `not_found_handling` nativo de Wrangler ("Infinite loop detected") — se resolvió dejando solo el mecanismo nativo. **Confirmado funcionando en el segundo intento.**

### Paso 4 — Prueba real

- [ ] Crear un evento de prueba en el entorno de producción
- [ ] Probar **desde un celular con datos móviles** (no WiFi local) para confirmar accesibilidad real desde internet

### Explícitamente pospuesto (a pedido del usuario)

- Verificación en vivo de la carpeta en el formulario de admin (endpoint `GET /api/events/check-folder` + botón "Comprobar carpeta") — usuario dijo "déjalo así por ahora".

### Mejoras recomendadas, no bloqueantes

- Miniaturas para el muro (rendimiento con muchas fotos — hoy carga full-res en cada tile).
- Autenticación mínima para `/admin/events/new` y `POST /api/events` (hoy están abiertos, cualquiera con la URL puede crear eventos).
- Endpoint `/health`.
- Rate limiting, tests automatizados, panel para editar/desactivar eventos existentes (hoy solo se pueden crear).

### Limpieza opcional

- La base de datos local tiene varios eventos de prueba acumulados (`demo`, `real-flow`, `xv-angie`, `cumpleanos-de-maria-30`, varios `smoke-test-*`). No estorban, pero se pueden borrar cuando se quiera arrancar limpio.
