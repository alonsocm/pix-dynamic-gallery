# Estado del proyecto — Pix Dynamic Gallery

> Documento vivo para retomar el trabajo en otra sesión. Última actualización: 6 de agosto de 2026.

## 🎯 Para retomar rápido

Quedamos en: **el usuario está creando las cuentas de Cloudflare y Neon** (Paso 1 de la sección "Pendiente" más abajo). Cuando estén listas, seguimos con el **Paso 2** (provisionar Neon/R2/Tunnel/Pages).

Repo: **https://github.com/alonsocm/pix-dynamic-gallery** (público, rama `main`, 2 commits)

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

### Paso 1 — Cuentas (usuario) — **EN PROGRESO**

- [ ] Cuenta Cloudflare (gratis)
- [ ] Cuenta Neon (gratis)
- [ ] Decidir dominio: ¿uno propio/existente, o arrancar con URLs gratuitas (`*.pages.dev`, `*.trycloudflare.com`) y comprar uno después?

### Paso 2 — Provisionar recursos (usuario, con guía de Claude)

- [ ] Neon: crear proyecto → copiar connection string (`postgresql://...?sslmode=require`)
- [ ] Cloudflare R2: crear bucket → generar API token (Access Key ID + Secret) → anotar endpoint S3 de la cuenta
- [ ] Cloudflare Tunnel: crear túnel (Zero Trust → Networks → Tunnels) → obtener token para `cloudflared`
- [ ] Cloudflare Pages: conectar al repo `alonsocm/pix-dynamic-gallery` en GitHub

### Paso 3 — Código para producción (Claude)

- [ ] Config `Storage:Provider=AwsS3` apuntando a R2 (mismo patrón que MinIO, solo config)
- [ ] Resolver URL pública de fotos en R2 (bucket público vs dominio custom)
- [ ] `Cors:AllowedOrigins` → dominio real de producción
- [ ] Adaptar el mecanismo de `env.js` (hoy pensado para Docker/Nginx) al build de Cloudflare Pages
- [ ] Script de arranque para la PC de la cabina (API nativa + `cloudflared` juntos)
- [ ] Instrucciones de instalación en la PC de la cabina: **.NET Runtime** (no el SDK completo), `cloudflared`, copiar el build publicado (sin clonar el repo completo)
- [ ] Configurar navegador en modo kiosk (pantalla completa) apuntando a la URL de Pages

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
