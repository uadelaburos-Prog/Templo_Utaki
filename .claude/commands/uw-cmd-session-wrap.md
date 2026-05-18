Cierra la sesión actual de Templo Utaki y actualiza los documentos de estado.

## Flujo

**Paso 1 — Reconstruir lo hecho**
Revisa la conversación actual y lista todo lo que se hizo: scripts creados/modificados, bugs corregidos, decisiones tomadas, problemas sin resolver.

**Paso 2 — Actualizar SESION.md**
Reescribe `SESION.md` con el estado actual real. Usa este formato:

```
# SESION — Templo Utaki
**Fecha:** YYYY-MM-DD  **Sesión:** N

## Estado actual
[2–3 oraciones: qué está funcionando, qué está en progreso]

## Completado esta sesión
- [lista de lo que se terminó]

## Pendiente (próxima sesión)
- [lista priorizada]

## Decisiones tomadas
- [decisiones de diseño o técnicas que deben persistir]

## Problemas conocidos
- [bugs o deudas técnicas identificadas]
```

**Paso 3 — SessionState (si la sesión fue larga)**
Si se tomaron 3+ decisiones importantes o se implementó una feature completa, genera también un `SessionState` usando el template en `templates/SessionState_Template.md`. Ofrécelo al usuario para que lo copie en una nueva conversación.

**Paso 4 — Resumen para commit**
Sugiere un mensaje de commit en español imperativo corto que resuma los cambios de esta sesión (para que Bonio lo use si hace commit).
