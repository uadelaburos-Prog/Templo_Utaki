# Session State — 2026-06-07

> **Propósito**: Pegar esto en una nueva conversación de Claude Code para restaurar contexto sin recargar la conversación completa.

---

## Proyecto

- **Juego**: Templo Utaki — 2D Platformer, Unity 6000.0.30f1, URP, UGUI
- **ProjectConfig**: `docs/ProjectConfig.yaml` — key settings: Unity 6, URP, UGUI, ai_mode: guided, async: Coroutines
- **Fase actual**: Post-Alpha / MVP Parcial 1
- **SESION.md**: Sesión 12 — 2026-06-07

---

## Sprint State

**Goal del sprint actual**: Completar infraestructura de enemigos y hazards para poder testear niveles completos.

| # | Feature / Task | Status | Notas |
|---|---------------|--------|-------|
| 1 | MummyAI — salto + patrol bounce | ✅ Done | Velocity-based ground, contact-based wall, configurable wallCheckOriginY |
| 2 | HazardFire (Llamarada Retráctil) | ✅ Código done | Falta asignar sprites en prefab |
| 3 | LauncherAI + Projectile | ✅ Código done | Falta crear prefabs en Unity Editor |
| 4 | Bug: Play button muerto al volver del menú | ✅ Fixed | Root cause + 5 fixes aplicados |
| 5 | SpectralPatrollerAI rediseño (órbita+dash) | ⬜ Pendiente | GDD lo cambió, script antiguo funciona pero incorrecto |

---

## Completado en las últimas sesiones

### MummyAI (Jun 6)
- `MummyAI.cs`: jump sobre obstáculos (detecta pared por `ContactPoint2D`, clearance configurable), velocity-based ground check, bounce patrol (sin waypoints), `VelocityY` al Animator

### HazardFire (Jun 7)
- `HazardFire.cs`: Enum `ModoActivacion` (Ciclico/Activador), animación por `Sprite[]` sin Animator, 4 fases temporalizadas, hitbox crece desde base del sprite (cálculo por pivot: `baseHitbox = -(pivot.y / pixelsPerUnit)`), peak alterna últimos 2 frames
- `FireHazard.prefab`: Layer "Hazard", isTrigger, HazardFire MonoBehaviour

### LauncherAI + Projectile (Jun 7)
- `LauncherAI.cs`: estacionario, cadencia configurable, `puntoDisparo` Transform hijo, frames cargando/disparando, friendly fire sobre Momia, Gizmos
- `Projectile.cs`: Kinematic Rigidbody2D, `Inicializar(Vector2 dir)`, rota sprite, lifetime, mata Player/Momia, se destruye en geometría sólida, atraviesa SpectralEnemy (trigger)

### Bug Fix crítico (Jun 7)
**Causa raíz**: `AudioManager` y `MenuManager` comparten el mismo GameObject `Managers` en `Menu.unity`. `AudioManager.Awake()` llamaba `Destroy(gameObject)` al detectar instancia DDOL duplicada → destruía `MenuManager` → el botón Play tenía referencia muerta → silencio.

Fixes:
- `AudioManager.cs`: `Destroy(this)` en lugar de `Destroy(gameObject)` + stop/destroy de AudioSources hijos duplicados
- `AudioManager.cs`: `SwapingVolume` usa `Time.unscaledDeltaTime`; guard `maxVolumen = vol > 0 ? vol : 1f`
- `GameLoopManager.cs`: `if (nivelActual == 0) return` en `Update()`
- `GameLoopManager.cs`: `IrAlMenuPrincipal()` oculta panelFinNivel/panelVictoria + `StopAllCoroutines()` + `isDying = false`
- `GameLoopManager.cs`: `OnSceneLoaded` early return para escena 0, oculta fadePanel DDOL

---

## En progreso / siguiente paso inmediato

**Prefabs en Unity Editor** (acciones manuales bloqueantes):
- `LauncherAI.prefab`: Layer Enemy, BoxCollider2D (isTrigger), hijo `PuntoDisparo` (Transform vacío), asignar `prefabProyectil` + `direccionDisparo`
- `Projectile.prefab`: Rigidbody2D (Kinematic, gravityScale=0), BoxCollider2D (isTrigger), asignar frames cuando haya arte
- `FireHazard.prefab`: asignar `frames[]` con sprites de fuego, ajustar `alturaMaxima` y `anchoBase`
- **MenuManager Inspector**: asignar `CanvasGroup` del fade (campo `Fade Panel` está null → sin fade visual al click Play)

---

## Decisiones pendientes

- [ ] **SpectralPatrollerAI**: ¿implementar rediseño GDD (órbita + dash) ahora o dejarlo para después del MVP?
- [ ] **GOs separados AudioManager/MenuManager**: el fix actual (`Destroy(this)`) es parche; arquitectura correcta sería GOs independientes. ¿Refactorizar la escena en Editor?

---

## Cambios de configuración recientes

- Ninguno a `ProjectConfig.yaml`

---

## TODOs activos en código

```
// Ningún TODO inline pendiente relevante.
// El SpectralPatrollerAI.cs usa el diseño antiguo (persecución recta),
// no el rediseñado en GDD (órbita + dash). Funciona para testing.
```

---

## Notas para la próxima sesión

- **El bug del botón Play YA ESTÁ CORREGIDO** — no intentar más fixes en esa dirección
- `MenuManager.fadePanel = null` en la escena es una ACCIÓN DE EDITOR, no un bug de script
- `GameLoopManager.Update()` ya tiene guard `if (nivelActual == 0) return` — el menú no interfiere
- `IrAlMenuPrincipal()` ya llama `StopAllCoroutines()` — sin más race conditions de RutinaReinicio
- Para el `Lanzador de Proyectiles`: en el Inspector, `direccionDisparo` acepta cualquier Vector2 (se normaliza en código); no olvidar desplazar `PuntoDisparo` lo suficiente para evitar autocolisión del proyectil
- El `HazardFire` en modo `Activador` espera que un activador externo llame `hazard.Activar()` — ese activador aún no existe; mientras tanto usar modo `Ciclico`
