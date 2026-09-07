---
target: the admin dashboard (/admin/finance)
total_score: 23
max_score: 40
na_heuristics: 
p0_count: 1
p1_count: 3
target_identity: "file:D:\\src\\pix-dynamic-gallery\\frontend\\src\\app\\features\\admin\\finance-dashboard.component.ts"
target_fingerprint: "sha256:4883ead73affce2d5a93a5b509efb89ffa49d9092eeb756ffa23c8bbbab3ea52"
target_path: "D:\\src\\pix-dynamic-gallery\\frontend\\src\\app\\features\\admin\\finance-dashboard.component.ts"
timestamp: 2026-09-07T18-53-50Z
slug: app-features-admin-finance-dashboard-component-ts
closed: true
---
# Crítica UX — /admin/finance (FinanceDashboardComponent)

Method: dual-agent (A: a73ed878d30d247cc · B: a690230466dbc774a)

## Design Health Score

| # | Heurística | Score | Hallazgo clave |
|---|---|---|---|
| 1 | Visibilidad del estado del sistema | 2 | saveCostPerKm() no da ninguna señal de éxito o fallo |
| 2 | Coincidencia con el mundo real | 4 | Vocabulario de dominio genuino, formato MXN correcto |
| 3 | Control y libertad del usuario | 2 | Sin editar un gasto ya cargado; sin Cancelar; solo confirm() nativo |
| 4 | Consistencia y estándares | 3 | Inconsistente con AgendaListComponent (sí muestra error/estado) |
| 5 | Prevención de errores | 2 | Valor inválido en Costo por km no hace nada, sin explicar por qué |
| 6 | Reconocer antes que recordar | 3 | Moneda preformateada y links contextuales buenos; falta ayuda inline |
| 7 | Flexibilidad y eficiencia | 1 | Sin filtro/búsqueda/rango de fechas; sin exportar |
| 8 | Diseño estético y minimalista | 3 | Fiel a tokens de DESIGN.md; Ganancia neta sin peso visual distinto |
| 9 | Ayudar a reconocer/recuperarse de errores | 1 | Guardado/borrado fallido no muestra ningún mensaje |
| 10 | Ayuda y documentación | 2 | Una buena excepción (anticipos de agenda), nada más |

Total: 23/40 — Aceptable

## Veredicto de especificidad de diseño

LLM (A): copy específico del negocio envuelto en un esqueleto de dashboard genérico; no reconoce el uso real (una mano, de noche, cansado, tras un evento).

Escaneo determinístico (B): CLI estático = 0 hallazgos. Detector en navegador sobre DOM renderizado = 4 (2 reglas x 2 elementos): ai-color-palette sobre text-emerald-400 (FALSO POSITIVO — DESIGN.md sanciona Neon Emerald para ingresos) y low-contrast 3.5:1 en botones primarios blanco/#ec4899 (REAL, verificado — falla AA 4.5:1, y es el componente button-primary de todo el sistema, no solo esta pantalla).

## Impresión general

Los KPIs al frente son correctos, pero un bug de zona horaria corrompe silenciosamente la fecha de cada movimiento, y la pantalla trata "guardar/borrar dinero real" con la misma indiferencia visual que un ajuste cosmético.

## Lo que funciona

1. Fila de KPIs al frente, sin scroll.
2. Ciudadanía fiel al sistema de diseño (paneles esmerilados, magenta racionado, emoji como iconografía).
3. Reafirmación bien puesta en anticipos de agenda ("ya cuenta como ingreso arriba").

## Problemas prioritarios

[P0] Bug de zona horaria corrompe silenciosamente la fecha de cada registro (new Date(dateString).toISOString() en gastos, depósitos de agenda, compras de papel/USB). Fix: parsear/construir como fecha local. Comando: /impeccable harden

[P1] Fallo/éxito silencioso en acciones de dinero (saveCostPerKm, submitGlobalExpense, removeGlobalExpense sin manejo de error/éxito, a diferencia de AgendaListComponent). Comando: /impeccable harden

[P1] Sin jerarquía visual para "Ganancia neta" (mismo peso que sus dos insumos). Comando: /impeccable bolder

[P1] Accesibilidad: botones 🗑️ sin aria-label, <select> de Categoría con nombre accesible roto, y contraste real verificado 3.5:1 (necesita 4.5:1) en el botón primario del sistema completo (blanco sobre #ec4899). Comando: /impeccable harden

[P2] Listas sin límite ni agrupación (Gastos globales, Ganancia por evento) — crecerán sin control. Comando: /impeccable distill

## Alertas por persona

Alex: sin buscar/filtrar/exportar; Costo por km no se refleja en otro lado.
Sam: botones sin nombre accesible, select roto, señal solo por color, contraste AA fallido.
Operador solo (persona del proyecto): gasto de esta noche archivado como de ayer (bug de fecha); 🗑️ sin padding junto al monto, confirm() trivial de tocar sin leer; "Ganancia por evento" hasta el final de la página.

## Observaciones menores

- Nav pills sin estado activo (regla intencional de DESIGN.md).
- "Costo por km" sin sufijo $/km.
- Prefijo "+" consistente en Gasto/Compra/Nueva reserva.
- barWidth() con piso de 2% — buen detalle.
- Sin timestamp de última actualización ni pull-to-refresh.

## Preguntas provocadoras

1. ¿Por qué "Ganancia neta" pesa visualmente igual que los números que solo la alimentan?
2. ¿Cuántos meses de contabilidad real ya cargan el error de fecha?
3. ¿Un confirm() nativo pelado es proporcional para borrar un registro financiero real?
