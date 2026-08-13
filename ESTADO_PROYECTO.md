# Estado del proyecto — Pix Dynamic Gallery

> Documento vivo para retomar el trabajo en otra sesión. Última actualización: 13 de agosto de 2026 (sesión Paso 5).

## 🎯 Para retomar rápido

**Pasos 1-4 completos** ✅ — todo el sistema probado de punta a punta en producción real (cabina + Cloudflare Pages + R2 + Neon + Tunnel), incluyendo desde un iPhone con datos móviles: captura → watcher → R2 → SignalR → kiosk → QR → foto de invitado → descargar → compartir. Varios bugs reales encontrados y arreglados en el camino (ver lista de bugs más abajo, #10-17): subida a R2 rota por streaming signature, descarga rota en Safari (dos veces), fotos perdidas si la cabina se queda sin internet, entre otros.

**Ahora en curso: Paso 5** — se descubrió que con la cabina apagada, toda la galería (wall, foto de invitado) deja de cargar (504), aunque las fotos sigan intactas en R2/Neon. Plan aprobado para resolverlo con una segunda instancia de la API (sin el watcher) en Azure Container Apps, **sin tocar código**. Ver la sección "Paso 5" más abajo para el checklist, y `C:\Users\Alonso\.claude\plans\tingly-nibbling-ritchie.md` para el razonamiento completo.

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
13. **Primera subida de foto real contra R2 falló** con `STREAMING-AWS4-HMAC-SHA256-PAYLOAD not implemented`. El SDK de AWS para .NET usa por defecto firma-en-streaming (`aws-chunked`) para `PutObject`; AWS S3 real y MinIO lo soportan, pero **R2 no** — es una limitación documentada de su compatibilidad S3. Fix: `PutObjectRequest.UseChunkEncoding = false` en [S3StorageService.cs](src/PixDynamicGallery.Infrastructure/Storage/S3StorageService.cs) (la propiedad vive en el *request*, no en `AmazonS3Config` — se intentó ahí primero y no compiló). Sin efecto negativo en AWS S3/MinIO, solo cambia a firmar el payload completo de una vez en lugar de en chunks — irrelevante para fotos de pocos MB.
14. **Botón "Descargar" en la página de invitado no descargaba, abría la foto en una pestaña nueva** (confirmado en un celular real, con datos móviles). Causa: el atributo `download` de un `<a href>` de HTML **solo funciona si el recurso es del mismo origen** — las fotos viven en `pub-xxxx.r2.dev`, un origen distinto a `somospix.com`, así que el navegador lo ignoraba y solo navegaba ahí. Fix en [download-share-buttons.component.ts](frontend/src/app/shared/ui/download-share-buttons/download-share-buttons.component.ts): en vez de un link directo, se trae la imagen con `fetch` + `Blob` y se dispara la descarga desde una URL `blob:` (mismo origen, sí respeta `download`). **Requiere que el bucket de R2 tenga una CORS Policy permitiendo `GET` desde `https://somospix.com`**, o el `fetch` falla por CORS (con fallback a abrir en pestaña nueva si eso pasa, para no dejar el botón sin hacer nada).
15. **Con el fix anterior, la descarga seguía fallando específicamente en iPhone/Safari**: mostraba el diálogo nativo "¿Quieres descargar este archivo?", pero al confirmar tiraba error. Causa: el código llamaba `URL.revokeObjectURL()` inmediatamente después de `link.click()`, sin esperar — en la mayoría de navegadores la descarga arranca al instante y no importa, pero Safari en iOS mete su propio diálogo de confirmación en el medio, y para cuando el invitado toca "Download" el blob ya estaba liberado. Fix: adjuntar el link al DOM antes de hacer click (Safari lo requiere para que el click "cuente"), y retrasar el `revokeObjectURL` con `setTimeout` en vez de llamarlo sincrónico.
16. **Descargar la misma foto una segunda vez fallaba en Safari/iOS** (la primera vez sí funcionaba). Causa: el nombre de archivo se derivaba del key en R2, siempre igual para esa foto — Safari no le hace auto-rename a una descarga repetida de `blob:` con el mismo nombre en la misma sesión (a diferencia de otros navegadores, que sí agregan un sufijo). Fix: `suggestedFilename()` ahora agrega un timestamp, generando un nombre distinto en cada intento. De paso, se mejoró el botón "Compartir" para mandar el archivo real (`navigator.share({ files })`) en vez de solo la URL — así en iOS el panel de compartir nativo ofrece "Guardar imagen" (directo a Fotos), algo que compartir solo-URL no habilita.
17. **Si la cabina se quedaba sin internet justo cuando se capturaba una foto, esa foto se perdía para siempre** (encontrado revisando el código a partir de una pregunta del usuario, no en producción real). Causa: `SparkboothWatcherService` marcaba un archivo como "ya visto" (`_knownFiles.TryAdd`) **antes** de saber si la subida a Neon/R2 tenía éxito — si fallaba por falta de conectividad, nunca se reintentaba, ni por el watcher en vivo ni por el polling de respaldo, ni siquiera reiniciando la API (un restart trata cualquier archivo ya presente en la carpeta como "preexistente" y lo ignora a propósito). Fix de dos partes: (1) [SparkboothWatcherService.cs](src/PixDynamicGallery.Infrastructure/Watcher/SparkboothWatcherService.cs) ahora libera el "reclamo" del archivo si el dispatch falla, para que el siguiente ciclo de polling (cada `Watcher:RefreshIntervalSeconds`) lo reintente; (2) [UploadCapturedPhotoCommandHandler.cs](src/PixDynamicGallery.Application/Photos/Commands/UploadCapturedPhoto/UploadCapturedPhotoCommandHandler.cs) se hizo idempotente por `(EventId, LocalFilePath)` — un reintento reutiliza la fila `Photo` que dejó el intento fallido en vez de crear una duplicada.

---

## 🔲 Pendiente

### Decisión de arquitectura para producción (ya tomada, falta ejecutar)

**Costo objetivo: $0/mes** (+ dominio opcional ~$10/año).

| Pieza | Dónde | Por qué (resumen) |
|---|---|---|
| Frontend | **Cloudflare Pages** | CDN gratis sin límite de banda, auto-deploy desde GitHub |
| API + Watcher (escritura, tiempo real) | **Nativo en la PC de la cabina** | El watcher necesita el filesystem local — no puede estar en la nube separado sin agregar un agente nuevo |
| API standby (lectura, siempre disponible) | **Azure Container Apps** (`Watcher:Enabled=false`) | Misma imagen Docker, sin el watcher — sirve la galería (wall, foto de invitado, crear evento) aunque la cabina esté apagada. Ver Paso 5. |
| Exponer la API de la cabina a internet | **Cloudflare Tunnel** | Gratis, sin port-forwarding, funciona detrás de cualquier NAT/WiFi de venue, soporta WebSockets (SignalR) |
| Base de datos | **Neon** (Postgres serverless) | Free tier generoso, se duerme sin actividad (irrelevante para uso por evento), compartida por ambas instancias de la API |
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

### Paso 4 — Prueba real — ✅ **COMPLETO**

- [x] Evento de prueba creado en producción (`xv-angie`)
- [x] Probado **desde un iPhone con datos móviles** (no WiFi local): captura → watcher → R2 → SignalR → kiosk → QR → foto de invitado → descargar → compartir. Todo el circuito confirmado funcionando de punta a punta.

### Paso 5 — Galería disponible con la cabina apagada — ✅ **COMPLETO**

Motivo: se descubrió (probando el Paso 4) que con la cabina apagada, `api.somospix.com` responde 504 — el wall/página de invitado dejan de funcionar por completo aunque las fotos sigan intactas en R2/Neon, porque el frontend solo sabe hablar con la API, nunca directo con la base o el storage. Plan completo y razonamiento en `C:\Users\Alonso\.claude\plans\tingly-nibbling-ritchie.md`. Resumen:

- Segunda instancia de la **misma** API (mismo `Dockerfile`, cero cambios de código) en **Azure Container Apps**, con `Watcher:Enabled=false` — sirve lectura/escritura REST (evento, fotos, crear evento) sin depender de la cabina, porque ninguno de esos endpoints toca el filesystem local.
- El frontend ya separaba `apiBaseUrl` de `hubBaseUrl` en `AppConfigService` — sin tocar código, `API_BASE_URL` (Pages) pasa a apuntar a Azure, `HUB_BASE_URL` sigue apuntando a la cabina (tiempo real solo mientras el evento está en curso).

Checklist:

- [x] Imagen construida localmente (`docker build`) desde `src/PixDynamicGallery.Api/Dockerfile` y subida a **Docker Hub** (`alonsopix/pix-gallery-api:latest`, repo público) — se descartó Azure Container Registry por tener costo mensual (rompía el objetivo de $0/mes).
- [x] Azure Container App **`pix-gallery`** creada (Resource Group `pix`, entorno `managedEnvironment-pix-9ed3`, región West US): 0.25 vCPU / 0.5 GiB, Ingress externo HTTP puerto `8080`, env vars según [tools/azure-standby/env.example](tools/azure-standby/env.example) (mismos valores de Neon/R2 que la cabina, más `Watcher__Enabled=false`). URL pública: `https://pix-gallery.mangograss-89e14f71.westus.azurecontainerapps.io`.
  - **Réplicas mínimas venía en 0 por defecto** (hubiera introducido el mismo cold-start que se quería evitar) → corregido a **1** en Escalado (Scale), aplicado como nueva revisión.
  - Verificado con `curl` (incluyendo headers CORS con `Origin: https://somospix.com`): responde 200 con datos reales de Neon (evento `xv-angie`, sus fotos con URLs de R2).
- [x] Cloudflare Pages: `API_BASE_URL` → `https://pix-gallery.mangograss-89e14f71.westus.azurecontainerapps.io`, `HUB_BASE_URL` sin cambios (`https://api.somospix.com`), redeploy confirmado (`https://somospix.com/env.js` refleja la nueva URL).
- [x] **Verificado end-to-end con la cabina realmente apagada**: `https://somospix.com/e/xv-angie/wall` carga las fotos con normalidad.

### Explícitamente pospuesto (a pedido del usuario)

- Verificación en vivo de la carpeta en el formulario de admin (endpoint `GET /api/events/check-folder` + botón "Comprobar carpeta") — usuario dijo "déjalo así por ahora".

### Mejoras recomendadas, no bloqueantes

- Miniaturas para el muro (rendimiento con muchas fotos — hoy carga full-res en cada tile).
- Autenticación mínima para `/admin/events/new` y `POST /api/events` (hoy están abiertos, cualquiera con la URL puede crear eventos).
- Endpoint `/health`.
- Rate limiting, tests automatizados, panel para editar/desactivar eventos existentes (hoy solo se pueden crear).

### Limpieza opcional

- La base de datos local tiene varios eventos de prueba acumulados (`demo`, `real-flow`, `xv-angie`, `cumpleanos-de-maria-30`, varios `smoke-test-*`). No estorban, pero se pueden borrar cuando se quiera arrancar limpio.
