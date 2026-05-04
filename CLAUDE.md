# CLAUDE.md — Templo Utaki
**GDD v3.0 (Abril 2026) = fuente autoritativa.** Código/tareas que contradigan el GDD → señalar antes de actuar.

---

## Stack
Unity `6000.0.30f1` · URP 2D `17.0.3` · C# MonoBehaviour · Input legacy (`Input.GetKey`) · `DistanceJoint2D` + `Rigidbody2D` · LineRenderer Bezier cuadrática 20 seg · Git/GitHub · 1920×1080 · 60fps · Todo en español

Repo: `github.com/uadelaburos-Prog/Templo_Utaki` · Branch activo: `"Camara&Fisicas"` (comillas en shell)

---

## Juego
Plataformas 2D pixel art 8×8 px. Gancho péndulo como mecánica core. 6 niveles + jefe. 20–30 min. Sin diálogos, sin texto, aprendizaje implícito.

**Reglas duras:**
- ❌ Sin vidas/salud — contacto con enemigo/trampa = reinicio instantáneo
- ❌ El jugador NO ataca
- ❌ Sin rutas alternativas, secretos, checkpoints, texto en pantalla
- ✅ Fade muerte < 0.5s, reaparece < 2s
- ✅ Legibilidad obligatoria: el jugador sabe si algo es grappleable/peligroso a primera vista

---

## Controles
| Acción | Teclado | Gamepad |
|---|---|---|
| Mover | A/D / flechas | Stick izq |
| Saltar | Space | A |
| Apuntar gancho | Mouse | Stick der |
| Lanzar | Click izq | RT |
| Soltar | Soltar click | Soltar |
| Pausa | ESC / P | Start |
| Reiniciar | R | X |

---

## Parámetros del jugador (`PlayerMovement.cs` — todo SerializeField)
`moveSpeed=8–10` · `jumpForce=12` · `fallGravity=5.5` · `maxFallSpeed=-20` · `normalGravity=1` · `hangGravity=2` · `hangTimeThreshold=0.1s` · `coyoteTime=0.12s` · `jumpBufferTime=0.15s` · `jumpCutMult=0.5` · Fricción suelo=0.6 · CapsuleCollider 0.5×1u · Input lag <50ms

---

## Gancho (`GrappleScript.cs`)
**Ciclo:** `idle → charging → launching → attached → retracting → idle`
(charging: click sostenido → carga distancia de `minGrappleDistance` a `maxGrappleDistance`)

| Param | Valor | Param | Valor |
|---|---|---|---|
| `maxGrappleDistance` | 15u | `hookGravity` | 18 |
| `launchSpeed` | 15–20 u/s | `maxSwingVelocity` | 15 u/s |
| `snapRadius` | 1.5u | Cooldown fallo | 0.3s |
| `swingDamping` | 0.02 (98%/frame) | Longitud cuerda | 2–6u variable |

**Cuerda visual:** vuelo=recta; enganchada=Bezier cuelga; retrayendo=~0.2s.
**Grappleable (solo layer `Grappleable`):** vigas metal, roca, cadenas, techo piedra. NO: madera podrida, vidrio, vegetación, tierra.

**Tipos de anclaje:** Fijo · Móvil (player arrastrado) · Destructible (1–2 usos) · Reactivo (activa efecto)

---

## Plataformas
| Tipo | Comportamiento |
|---|---|
| Estática | Inmóvil, fricción 0.6 |
| Móvil | 0.5–3 u/s, player hereda velocidad |
| Frágil | Timer 1–2s desde pisada, 3 fases visuales, regenera 5s |
| One-Way | Atravesable desde abajo, soporta desde arriba |
| Tracción | Se desplaza hacia el jugador con gancho tensado |

---

## Obstáculos, trampas e interactuables
**Trampas (contacto = reinicio):** pinchos estáticos · fuego · foso vacío · pinchos retráctiles (ciclo configurable)
**Activadores:** placa de presión → roca cayente (trayectoria vertical fija → horizontal al impactar)
**Interactuables por gancho:** pared reactiva · plataforma tracción · viga destructible
**Interactuables por proximidad:** puerta inicio/salida · palanca/switch

---

## Enemigos

**Patrullero** (`PatrollerAI.cs`): `patrolSpeed=2` · `chaseSpeed=4` · `detectionRadius=5u`
Estados: `idle → patrulla → alerta → persecución → regreso`. Área rectangular predefinida. Contacto = reinicio.

**Lanzador** (`LauncherAI.cs` + `Projectile.cs`): fijo, dirección fija (NO apunta al jugador), `fireRate=2–3s`, `projectileSpeed=8 u/s`, vida proyectil=10s. Contacto = reinicio.

**Golem** (`GolemBoss.cs`): estático, 2 fases.
- Fase 1: proyectiles lentos en patrones predecibles.
- Fase 2: cadencia mayor + ráfagas + plataformas colapsan.
- Victoria: alcanzar punto débil durante ventanas entre ataques.

---

## Niveles
| # | Nombre | Mecánicas nuevas | Enemigos | Min |
|---|---|---|---|---|
| 1 | Tutorial Jungla | Movimiento, salto, gancho, pinchos retráctiles, pared reactiva, tracción, palanca | Patrullero×1 | 2–3 |
| 2 | Entrada Templo | Gancho obligatorio + plataformas estáticas complejas | — | 4–5 |
| 3 | Cámaras de Piedra | Gancho + plataformas móviles | — | 4–5 |
| 4 | Plataformas Peligrosas | Frágiles + Patrullero | Patrullero | 5–6 |
| 5 | Las Profundidades | Todo + Lanzador + trampas | Ambos | 5–6 |
| 6 | Cámara del Tesoro | Maestría + Arena Golem | Golem | 3–4+boss |

Nivel 6: Parte 1 (recorrido dificultad alta sin enemigos) + Parte 2 (arena Golem). Victoria → fade dorado → créditos con puntuación y contador de muertes.

---

## Arte
Pixel art 8×8 px/tile. Paleta evoluciona de jungla cálida (N1) → oscuro místico (N4-5) → dorado (N6).
HUD mínimo: contador cristales. Sin barras de vida, sin mapa, sin texto.

---

## Audio
Solo SFX/música que comunican información. Música silenciada en pausa.
SFX clave: lanzar gancho · enganche · aterrizaje · salto · alerta enemigo · proyectil · cristal · muerte · plataforma frágil · placa presión · roca cayente

---

## Arquitectura de scripts

| Script | Estado | Responsabilidad |
|---|---|---|
| `PlayerMovement.cs` | ✅ | Movimiento, salto, gravedad variable, coyote, buffer, momentum jump, swing jump boost |
| `GrappleScript.cs` | ✅ | Estados gancho, Linecast, snap 1.5u, Bezier, carga, climb W/S, obstacleMask |
| `CamaraScript.cs` | ✅ | Seguimiento Lerp, look-behind horizontal, look-down por caída |
| `MovingPlatform.cs` | 🟡 | Senoidal — falta Kinematic + herencia velocidad player |
| `VoidScript.cs` | ✅ | Trigger vacío → recarga escena |
| `LevelManager.cs` | ⚪ | Singleton: muerte, fade, reinicio, cristales, fin nivel, contador muertes |
| `CrystalPickup.cs` | ⚪ | Trigger con Player → LevelManager.CollectCrystal() → desactivar |
| `PatrollerAI.cs` | ⚪ | Máquina estados patrullero, gizmos A↔B + radio detección |
| `LauncherAI.cs` | ⚪ | Timer disparo, instancia proyectil, dirección fija |
| `Projectile.cs` | ⚪ | Movimiento, impacto, destrucción por tiempo/borde |
| `GolemBoss.cs` | ⚪ | Estados jefe, fases, patrones, condición victoria |
| `FragilePlatform.cs` | ⚪ | Timer colapso, 3 fases color, regenera 5s |

**Post-MVP (Alpha):** `LauncherAI` · `Projectile` · `GolemBoss` · `OneWayPlatform` · `TractionPlatform` · `PressurePlate` · `FallingRock` · `Lever` · `Door` · `ReactiveWall` · `DestructibleBeam` · `RetractableSpikes`

---

## Layers de Unity

| Layer | Uso |
|---|---|
| `Floor` (3) | Suelo y plataformas, detección isGrounded |
| `Grappleable` (6) | Raycast del gancho — anclajes válidos |
| `Obstacle` (8) | Bloquea raycast sin engancharse |
| `Enemy` (9) | Contacto con Player → muerte |
| `Hazard` (10) | Trigger con Player → muerte |
| `Collectible` (11) | Trigger con Player → recolección |
| `Player` (7) | Player |

**Pendiente en Editor:** renombrar layer 6 `Grapple→Grappleable` · crear Obstacle(8)/Enemy(9)/Hazard(10)/Collectible(11)
**Tags a crear:** `Crystal` · `Exit` · `Enemy` · `Hazard` · `SpawnPoint`

---

## Estado actual (snapshot 2026-04-19)

### Bugs resueltos ✅
GrappleScript dedupe de fields · CamaraScript rb/duplicado/orthographicSize → Start · isGrounded→FixedUpdate · groundCheck SerializeField (0.45×0.05, gizmo rojo/verde) · jumpCooldown removido · feature Rebote removida · obstacleMask implementado · snapRadius 1.5u efectivo

### Pendientes en Inspector 🟡
- `moveSpeed` → 8–10 (default script: 12)
- `jumpForce` → 12 · `fallGravity` → 5.5
- `CapsuleCollider2D.size` → (0.5, 1)
- Asignar `obstacleMask` (Floor mínimo)
- Eliminar `ropeProgressionSpeed` huérfano
- Eliminar `PlayerMovement` duplicado del Player

### Lo que funciona bien ✅ (no tocar sin razón)
Estados gancho completo · coyote/buffer/jump cut/gravedad variable · momentum jump (`jumpHBoost=4`) · swing jump boost (`+30%` altura post-gancho) · carga del gancho (click sostenido) · climb W/S (`climbSpeed=6`) · hookGravity 18 parabólico · Linecast obstacleMask · highlight al cargar · Bezier + onda AnimationCurve · gizmos debug · look-behind/look-down cámara

### Features pendientes ⚪
LevelManager · CrystalPickup · fade muerte · HUD · pantalla fin nivel · Menú principal · Pausa (ESC) · Victoria/créditos · Contador muertes · PatrollerAI · LauncherAI · Golem · FragilePlatform · One-Way · Tracción · Todas las trampas e interactuables · 5 niveles (solo existe SampleScene) · Arte definitivo · Audio

---

## Plan MVP — Parcial 1

**DoD MVP:** compila sin errores · 1920×1080 · gancho/salto/movimiento fluidos · muerte→reaparece <2s · PatrollerAI mínimo (patrol A↔B) · FragilePlatform · ≥3 cristales con HUD · pantalla fin nivel (cristales/tiempo/botón Continuar) · ESC pausa (`timeScale=0`) · R reinicia · sin crashes en 5 playthroughs · build .exe standalone

### Sesión 4 — Systems y muerte
1. `LevelManager.cs` — singleton, fade <0.5s, reinicio <2s, cristales, contador muertes, ShowEndOfLevel
2. `CrystalPickup.cs` — trigger → LevelManager.CollectCrystal() → desactivar
3. Canvas `HUD`: `TMP_Text "Cristales: X/Y"` + `Image` negro fullscreen alpha=0 (fade)
4. Refactor `VoidScript` → `LevelManager.Instance.PlayerDied()`
5. Prefab `Crystal` (CircleCollider isTrigger + SpriteRenderer placeholder + CrystalPickup)
6. Build de prueba temprano para detectar problemas URP

### Sesión 5 — Nivel 1 + Enemigo + Frágil
1. Renombrar `SampleScene` → `Sandbox_Mecanicas` (playground, índice 0 en Build)
2. Crear `Level_01_JunglaInicial.unity` (índice 1)
3. Armar beat-map Nivel 1: entrada → escalera → gap → abismo+gancho → pared reactiva (placeholder) → pinchos → meta
4. `PatrollerAI.cs` + 1 instancia con pointA/pointB
5. `FragilePlatform.cs` + 1 instancia
6. 3 cristales en posiciones que requieran gancho
7. Canvas `EndOfLevel` (inactivo) + trigger de salida → LevelManager.ShowEndOfLevel()

### Sesión 6 — Pulido y Build
1. Highlight grappleable: pulso verde cuando `dist(player, superficie) < 8u`
2. PauseMenu ESC: `Time.timeScale=0`, botones Reanudar/Reintentar/Menú
3. Feedback visual: destello al lanzar · color LineRenderer al enganchar · "!" sobre Patrullero en alerta
4. Build PC 1920×1080 final

---

## Contratos de scripts nuevos

### `LevelManager.cs`
```
[SerializeField] private CanvasGroup fadeCanvas;
[SerializeField] private float fadeDuration = 0.4f;
[SerializeField] private Transform spawnPoint;
[SerializeField] private int totalCrystals;
[SerializeField] private GameObject endOfLevelPanel;
private int crystalsCollected, deathCount;
private float levelStartTime;
// API: PlayerDied() · CollectCrystal() · LoadNext() · RestartLevel() · ShowEndOfLevel()
```

### `PatrollerAI.cs`
```
[SerializeField] private float patrolSpeed = 2f, chaseSpeed = 4f, detectionRadius = 5f;
[SerializeField] private Transform pointA, pointB;
[SerializeField] private LayerMask playerLayer;
enum State { Idle, Patrol, Alert, Chase, Return }
// Contacto con Player → LevelManager.Instance.PlayerDied()
// MVP mínimo: solo Patrol A↔B sin Alert/Chase
```

### `FragilePlatform.cs`
```
[SerializeField] private float breakDelay = 1.5f, regenDelay = 5f;
[SerializeField] private SpriteRenderer sr;
[SerializeField] private Color[] warningColors; // 3 fases: verde/amarillo/rojo
// OnCollisionEnter2D con Player → corrutina Countdown()
// Al breakDelay: desactivar Collider2D + SpriteRenderer
// Al regenDelay: reactivar
```

---

## Convenciones
- Comentarios en **español** · Naming en **inglés** estándar Unity
- `[SerializeField] private` para valores ajustables · `[Header("...")]` para agrupar en Inspector
- Cachear componentes en `Awake`/`Start` — nunca `GetComponent` en `Update`/`FixedUpdate`
- **Física en `FixedUpdate`** · input en `Update` — nunca mezclar
- Movimiento vía `Rigidbody2D` — nunca `Transform.Translate`
- Todo valor de diseño en SerializeField — nunca hardcodear (Santiago hace tuning desde Inspector)
- Commits: español, imperativo corto (`Fix: bug grounding`). Branch por feature. Review Bonio antes de merge.
- Git: nunca editar `.meta` a mano · nunca borrar archivos desde Explorer (usar Unity Editor)

---

## Equipo
| Nombre | Rol |
|---|---|
| **Bono Dipacce (Bonio)** | Game Design / Prog — **aprobación final GD y Prog** |
| Fermin Blanco | Programación |
| Eliel Denmon | Prog / Audio |
| Belen Almed | Arte (sprites, enemigos, Golem, tileset N6) |
| Julieta Cerelli | Arte (tilesets N1-N2) |
| Santiago Calvo | Producción / QA / tuning Inspector |

---

## Reglas para Claude
1. GDD v3.0 manda — señalar discrepancias antes de actuar
2. No inventar features no listadas en el GDD sin confirmación de Bonio
3. Leer el script antes de afirmar comportamientos
4. Repo `Templo_Utaki` (underscore). Branch `"Camara&Fisicas"` (comillas en shell)
5. Responder siempre en español
6. Avisar si algo empuja el scope más allá del GDD
7. Física en FixedUpdate, input en Update — nunca mezclar
8. Todo valor de diseño en SerializeField — nunca hardcodear
9. Antes de marcar tarea GD/Prog como completa: requiere review de Bonio
