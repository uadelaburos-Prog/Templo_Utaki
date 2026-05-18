Realiza una auditoría completa de la implementación actual contra el GDD v3.0 de Templo Utaki.

## Flujo

**Paso 1 — Cargar referencias**
Lee `CLAUDE1.md` completo (GDD autoritativo) y luego lee todos los scripts en `Templo_Utaki_Unity/Assets/Scripts/` y sus subdirectorios.

**Paso 2 — Auditoría por sistema**
Para cada sistema listado en el GDD, genera una tabla:

| Sistema | Script | Estado | Detalle |
|---------|--------|--------|---------|
| PlayerMovement | PlayerMovement.cs | ✅ / ⚠️ / ❌ | Descripción breve |

Estados:
- ✅ **Implementado** — cumple la spec del GDD
- ⚠️ **Parcial** — implementado pero faltan detalles o hay desvíos menores
- ❌ **Faltante** — no existe o está roto
- 🚫 **Post-MVP** — marcado explícitamente como post-MVP en el GDD

**Sistemas a auditar:**
- Jugador (movimiento, física Celeste-style, valores exactos)
- Gancho (estados, distancia, visual Bezier, snap)
- Plataformas (estática, móvil, frágil, one-way)
- Obstáculos (pinchos, fuego, foso)
- Enemigos (PatrollerAI, LauncherAI)
- LevelManager (fade, muerte, cristales, fin de nivel)
- Audio (AudioManager, SFX asignados)
- Cámara (lerp, look-behind, look-down)

**Paso 3 — Resumen ejecutivo**
Lista priorizada de gaps, separando:
- 🔴 **Bloqueante para entrega** — falta algo crítico para que el juego funcione
- 🟡 **Polish pendiente** — funciona pero no cumple spec exacta
- 🟢 **Post-MVP** — fuera de scope actual

**Paso 4 — Recomendación**
Sugiere las 3 acciones más importantes a tomar antes de la próxima entrega, en orden de prioridad. No implementes nada — este comando es solo diagnóstico.
