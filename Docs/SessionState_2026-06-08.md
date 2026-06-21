# Session State — 2026-06-08 02:49

> Pegar esto en una conversación nueva para restaurar contexto.

---

## Project

- **Game**: Templo Utaki — plataformero 2D con gancho, enemigos y hazards
- **ProjectConfig**: `docs/ProjectConfig.yaml`
- **Current Phase**: MVP Parcial — Semana 4/5
- **Branch**: `main`

---

## Sprint State

| # | Feature / Task | Status | Notas |
|---|---------------|--------|-------|
| 1 | HazardFire modo Activador | ✅ Done | Fix TickCiclo + Desactivar() pública |
| 2 | HazardFire desfase ciclo completo | ✅ Done | AplicarDesfase() nuevo método |
| 3 | GameLoopManager limpieza paneles | ✅ Done | panelVictoria eliminado, txtResumenPanel directo |
| 4 | Bug pausa — input bloqueado | ✅ Done | IsPaused static + guards en Player y Grapple |
| 5 | Bug fin de nivel — input bloqueado | ✅ Done | NivelCompleto() setea IsPaused + AudioListener |
| 6 | CameraZone transform-based | ✅ Done | Migración manual pendiente en escenas |
| 7 | KeyCarrier — fantasma porta llave | ✅ Done | FixedUpdate propio, sprite voltea inversamente |
| 8 | AfterimageTrail — sprites huérfanos | ✅ Done | AfterimageImage clase en mismo archivo |
| 9 | WorldTextNotification — sorting | ✅ Done | sortingLayerName + sortingOrder en Inspector |
| 10 | Migración CameraZones en escena | ⬜ Pending | Requiere setup manual en Editor |

---

## Completed This Session

- `HazardFire.cs` — fix modo Activador completa ciclo; `Desactivar()` nueva; `AplicarDesfase()` soporta ciclo completo
- `GameLoopManager.cs` — eliminado `panelVictoria`; `IsPaused` static; guards de pausa; `NivelCompleto()` bloquea input
- `CameraZone.cs` — reescrito: posición del GO = centro de zona; campo `tamaño` Vector2
- `KeyCarrier.cs` — rework completo: FixedUpdate propio, desvinculado de jerarquía, sprite flip invertido
- `AfterimageTrail.cs` — clase `AfterimageImage` autocontenida en mismo archivo
- `WorldTextNotification.cs` — sortingLayerName + sortingOrder configurables
- `PlayerMovement.cs` — guard `GameLoopManager.IsPaused` en Update
- `GrappleScript.cs` — guard `GameLoopManager.IsPaused` en Update

---

## Pending Setup en Unity Editor

- **CameraZones**: para cada zona existente → mover GO al centro del área → setear `Tamaño X/Y`
- **GameLoopManager prefab**: asignar `txtResumenPanel` (TMP_Text hijo de panelFinNivel)
- **Botón "Continuar"**: `OnClick → GameLoopManager.ContinuarSiguienteNivel()`
- **KeyCarrier**: ajustar `offsetPortada` en Inspector del fantasma
- **WorldTextNotification**: setear `sortingLayerName` correcto en cada checkpoint
- **HazardFire con palanca**: conectar `Desactivar()` a `Lever.alDesactivar` donde corresponda

---

## Decisiones técnicas clave

- `panelFinNivel` es el único panel de resultados — en el último nivel "Continuar" carga escena 0
- `GameLoopManager.IsPaused` es static — cualquier script lo consulta sin null-check
- `CameraZone` sin dependencia de física — bounds desde `transform.position + tamaño/2`
- `KeyCarrier` desvincula la llave de la jerarquía en `Start()` y la mueve por `MovePosition`
- `AfterimageImage` (mismo .cs que `AfterimageTrail`) corre fade sobre sí misma, independiente del fantasma

---

## Deuda técnica conocida

- Legacy Input System en todo el proyecto (`Input.GetKey`) — no bloquea MVP
- `Lever.Update()` sin guard de pausa — menor, solo activable con gancho
- CameraZones en escenas requieren migración manual (breaking change esta sesión)
