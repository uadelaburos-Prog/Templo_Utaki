# Sesión 7 — Sistema de Muerte Dramática y Animaciones de Jugador

**Fecha:** 15 de mayo de 2026
**Estado:** Implementado — pendiente configuración del Animator en Unity Editor

---

## Resumen

Se implementaron dos nuevos estados de animación para el jugador (muerte dramática y escalada de cuerda), se rediseñó el sistema de muerte para separar la muerte por vacío de la muerte por hazard, y se corrigieron tres bugs críticos relacionados con la detección de suelo y el manejo de eventos de muerte simultáneos.

---

## 1. Sistema de Muerte Dramática

### Comportamiento

Cuando el jugador muere por un hazard (pinchos, fuego, etc.) se activa la secuencia dramática:

1. El jugador pierde el control instantáneamente (`isDead = true`)
2. La pantalla se oscurece casi de inmediato con el SpotlightOverlay (fade-in rápido)
3. El juego completo se pausa (`Time.timeScale = 0`)
4. La animación de muerte del jugador se reproduce en tiempo real (UnscaledTime)
5. El jugador cae si estaba en el aire, mediante simulación de gravedad manual con `Time.unscaledDeltaTime`
6. El spotlight se mantiene visible según `Tiempo Spotlight`:
   - `0` → dura hasta que la escena recarga
   - `> 0` → hace fade-out después de esos segundos y luego recarga
7. La escena se recarga; `OnSceneLoaded` restaura `timeScale = 1` y desactiva el spotlight

La muerte por **vacío** (`VoidScript`) sigue siendo instantánea: sin animación, sin pausa, reinicio directo.

### Separación por tipo de muerte

```
PlayerDied(fromVoid: false)  →  RutinaMuerteDramatica()  →  animación + spotlight + pausa
PlayerDied(fromVoid: true)   →  RutinaReinicio()          →  fade simple, sin dramatismo
```

---

## 2. SpotlightOverlay — Efecto de Reflector

### Descripción

Overlay de pantalla negra con un círculo despejado centrado en el jugador. Genera el efecto de "reflector de teatro" durante la muerte dramática.

### Archivos

| Archivo | Tipo |
|---|---|
| `Assets/Shaders/SpotlightOverlay.shader` | Shader CG personalizado |
| `Assets/Scripts/SpotlightOverlay.cs` | MonoBehaviour controlador |

### Shader (`Custom/SpotlightOverlay`)

- Renderiza sobre una `RawImage` que cubre el Canvas completo
- Usa `_ScreenParams.xy` (built-in de Unity) para el aspect ratio — garantiza un círculo perfecto en cualquier resolución sin depender de cálculos en C#
- El fragmento computa: `dist = length(float2(diff.x * aspect, diff.y))` y aplica `smoothstep` para el borde suave
- Cola Transparent, blend SrcAlpha / OneMinusSrcAlpha

### Parámetros del shader (editables desde el Inspector del componente)

| Parámetro | Default | Descripción |
|---|---|---|
| `Radius` | `0.18` | Radio del círculo como fracción de la altura de pantalla (0.18 ≈ 195 px en 1080p) |
| `Softness` | `0.10` | Suavidad del borde del círculo |
| `Max Alpha` | `0.92` | Oscuridad máxima de la zona negra (1 = negro total) |

### Setup en Unity (jerarquía obligatoria)

```
GameLoopManager (DontDestroyOnLoad)
  └── SpotlightCanvas        [Canvas — Screen Space Overlay — Sort Order 50]
        └── SpotlightImage   [RawImage — ancla full-stretch]
                              [SpotlightOverlay — el script va aquí]
                              [Material con shader Custom/SpotlightOverlay]
```

El `SpotlightCanvas` debe ser hijo directo del `GameLoopManager` para persistir entre escenas junto con él.

---

## 3. Estado de Animación: Escalada de Cuerda (IsClimbing)

### Comportamiento

Cuando el jugador está colgado del gancho y presiona W o S para subir/bajar la cuerda, el Animator activa el parámetro `IsClimbing = true`.

### Implementación

- `GrappleScript.IsClimbing` (propiedad pública): se activa en `GrappleSwing()` cuando `climbInput != 0f`
- Se resetea a `false` en `GrappleRetract()` y `ForceIdle()`
- `PlayerMovement.Update()` escribe `anim.SetBool("IsClimbing", hangingNow && grapple.IsClimbing)`

---

## 4. Parámetros Nuevos en el Inspector

### `PlayerMovement`

| Campo | Tipo | Default | Descripción |
|---|---|---|---|
| `Death Anim Duration` | float | `1.5` | Duración del clip de animación Death en segundos. Debe coincidir con el clip real del Animator. |

### `GameLoopManager` — Header "Muerte Dramática"

| Campo | Tipo | Default | Descripción |
|---|---|---|---|
| `Spotlight Overlay` | SpotlightOverlay | — | Referencia al componente en SpotlightImage |
| `Spotlight Fade In` | float | `0.10` | Segundos del fade-in al activarse (recomendado: 0.05–0.15 para efecto inmediato) |
| `Spotlight Fade Out` | float | `0.20` | Segundos del fade-out al cerrarse |
| `Tiempo Spotlight` | float | `0` | Duración visible del spotlight. `0` = dura hasta el reinicio de escena |

---

## 5. Configuración del Animator (Editor — obligatorio)

### Parámetros a agregar

| Nombre | Tipo | Notas |
|---|---|---|
| `Death` | Trigger | Usado internamente; el código usa `anim.Play("Death", 0, 0f)` para forzar el estado sin pasar por el grafo |
| `IsDead` | Bool | Se pone en `true` en `TriggerDeath()`. **Debe agregarse como condición `IsDead == false` en todas las transiciones "Any State → X" excepto Death**, para evitar que Fall/Jump/Idle sobreescriban la animación de muerte |
| `IsClimbing` | Bool | Activo cuando el jugador sube o baja la cuerda con W/S |

### Transición Any State → Death

| Ajuste | Valor |
|---|---|
| Has Exit Time | ❌ false |
| Transition Duration | `0` |
| Interruption Source | Any State |
| Ordered Interruption | ❌ false |

### Estado Death

- Asignar el clip de animación de muerte
- Sin transiciones de salida (la escena se recarga al terminar la secuencia)
- El clip debe durar exactamente lo configurado en `Death Anim Duration` del Inspector

### Estado Climbing

- Activar desde el estado Hanging con condición `IsClimbing == true`
- Transición de vuelta a Hanging cuando `IsClimbing == false`

---

## 6. Bugs Corregidos

### Bug 1 — Salto infinito en paredes verticales (`PlayerMovement`)

**Causa:** El ground check usaba `Physics2D.OverlapBox`, que detectaba paredes verticales como suelo.

**Fix:** Reemplazado por `rb.GetContacts(ContactFilter2D, ContactPoint2D[])` con filtro de normal Y ≥ `groundNormalThreshold` (0.7 por default ≈ 45°). Cero allocations por frame, inmune a colisiones laterales.

**Campo nuevo:** `Ground Normal Threshold` (float, Range 0.5–1.0, default `0.7`)

---

### Bug 2 — Pinchos estáticos nunca mataban (`SpikeHazard`)

**Causa:** `OnTriggerEnter2D` verificaba `_estado != Estado.Desplegado` para todos los modos. En `SpikeMode.Estatico`, `_estado` siempre vale `Retraido` (default del enum = 0), así que la condición nunca se cumplía y `PlayerDied()` nunca se llamaba.

**Fix:** La guarda de estado solo aplica al modo `Retractil`:
```csharp
if (modo == SpikeMode.Retractil && _estado != Estado.Desplegado) return;
```

---

### Bug 3 — Múltiples corrutinas de muerte simultáneas (`GameLoopManager`)

**Causa:** `PlayerDied()` no tenía guard. Si el jugador tocaba un pincho en el borde de un vacío, `SpikeHazard.OnTriggerEnter2D` y `VoidScript.OnTriggerEnter2D` llamaban a `PlayerDied()` en el mismo frame. `RutinaMuerteDramatica()` y `RutinaReinicio()` corrían en paralelo: la segunda hacía FadeOut a negro inmediatamente, ocultando la animación de muerte.

**Fix:** Flag `isDying` en `GameLoopManager`. Primer llamado entra, los siguientes retornan inmediatamente. Se resetea en `OnSceneLoaded`.

---

## 7. Archivos Modificados

| Archivo | Cambios |
|---|---|
| `Scripts/PlayerMovement.cs` | Ground detection por normales; `TriggerDeath()`; `isDead` + simulación de gravedad manual; `IsClimbing` en Animator |
| `Scripts/GameLoopManager.cs` | `PlayerDied(fromVoid)`; `RutinaMuerteDramatica()`; `isDying` guard; pausa/restauración de `timeScale`; `OnSceneLoaded` restaura estado |
| `Scripts/VoidScript.cs` | Llama `PlayerDied(fromVoid: true)` |
| `Scripts/SpikeHazard.cs` | Fix modo Estatico en `OnTriggerEnter2D` |
| `Scripts/GrappleScript.cs` | Propiedad `IsClimbing`; actualización en `GrappleSwing`, `GrappleRetract`, `ForceIdle` |
| `Scripts/SpotlightOverlay.cs` | **NUEVO** — controlador del efecto de reflector |
| `Shaders/SpotlightOverlay.shader` | **NUEVO** — shader CG del reflector |
