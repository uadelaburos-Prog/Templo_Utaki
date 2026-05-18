# CLAUDE.md — Templo Utaki
**GDD v4.5 (`Docs/GDD_TemploUtaki_v4.5.md`) = fuente autoritativa.** Discrepancias → señalar antes de actuar.
**Estado del proyecto: Post-Alpha (Mayo 2026)**
**Estado actual y tareas de sesión → ver `SESION.md`**

## Stack
Unity `6000.0.30f1` · URP 2D `17.0.3` · C# MonoBehaviour · Input legacy · `DistanceJoint2D` + `Rigidbody2D` · LineRenderer Bezier · 1920×1080 · 60fps · Todo en español
Repo: `github.com/uadelaburos-Prog/Templo_Utaki` · Branch: `"Camara&Fisicas"` (comillas en shell)

## Juego
Plataformas 2D pixel art 8×8 px. Gancho péndulo como mecánica core. 6 niveles + jefe. 20–30 min.
❌ Sin vidas — contacto enemigo/trampa = reinicio · ❌ Jugador NO ataca · ❌ Sin checkpoints/texto
✅ Fade muerte <0.5s, reaparece <2s · ✅ Legibilidad: grappleable/peligroso visible a primera vista

## Controles
Mover: A/D · Saltar: Space · Apuntar: Mouse · Lanzar: Click izq · Soltar: Soltar click · Pausa: ESC/P · Reiniciar: R

## Jugador (`PlayerMovement.cs`)
`moveSpeed=8–10` · `jumpForce=12` · `fallGravityScale=3.5` · `maxFallSpeed=-20` · `riseGravityScale=2.0` · `swingGravityScale=2.5` · `coyoteTime=0.12s` · `jumpCutMult=0.5` · `airAccel=35` · `airDecel=20` · CapsuleCollider 0.5×1u

## Gancho (`GrappleScript.cs`)
Ciclo: `idle → charging → launching → attached → retracting → idle` (charging: click sostenido = carga distancia)
`maxGrappleDistance=10u` · `launchSpeed=20` · `snapRadius=0.4u` · `hookGravity=18` · `swingDamping=0.02` · `failCooldown=0.3s` (solo en fallo; soltar voluntario sin cooldown) · `minGrappleDistance=3u`
Cuerda visual: vuelo=recta · enganchada=Bezier · retrayendo con gravedad en aire, línea recta al tocar piso
Grappleable (layer `Grappleable`): metal, roca, cadenas, piedra. NO: madera, vidrio, vegetación, tierra.
Anclajes: Fijo · Reactivo (ReactiveWall) · Móvil (post-MVP) · Destructible (post-MVP)

## Plataformas
Estática: fricción 0.6 · Móvil: `speed=2` u/s, Lerp lineal, hereda velocidad (posición directa) · Frágil: `breakDelay=1.5s`, 3 fases visuales, `regenDelay=5s` · One-Way: atravesable abajo (S/↓) · Reactiva (ReactiveWall): movible con gancho, cae al superar `distanciaDerribo` · Tracción (post-MVP)

## Obstáculos y trampas
Trampas (reinicio): pinchos estáticos · fuego · foso · pinchos retráctiles
Activadores: placa presión → roca cayente · Gancho: pared reactiva, tracción, viga destructible · Proximidad: puerta, palanca

## Enemigos
**Patrullero** (`PatrollerAI.cs`): `patrolSpeed=2` · `chaseSpeed=4` · `detectionRadius=5u` · `radioAbandonar=7u` · estados: idle→patrulla→persecución→regreso (alerta colapsado en persecución: icono "!" rojo visible mientras persigue) · contacto = reinicio
**Lanzador** (`LauncherAI.cs`): fijo, dirección fija, `fireRate=2–3s` · `projectileSpeed=8` · vida proyectil=10s · contacto = reinicio
**Golem** (`GolemBoss.cs`): 2 fases — F1: proyectiles lentos predecibles · F2: cadencia mayor + ráfagas + colapso plataformas · victoria: alcanzar punto débil entre ataques

## Niveles
| # | Nombre | Mecánicas nuevas | Enemigos |
|---|---|---|---|
| 1 | Tutorial Jungla | Movimiento, salto, gancho, pinchos retráctiles, pared reactiva, tracción, palanca | Patrullero×1 |
| 2 | Entrada Templo | Gancho obligatorio + plataformas complejas | — |
| 3 | Cámaras de Piedra | Gancho + plataformas móviles | — |
| 4 | Plataformas Peligrosas | Frágiles | Patrullero |
| 5 | Las Profundidades | Todo + trampas | Ambos |
| 6 | Cámara del Tesoro | Maestría + Arena Golem | Golem |

N6: recorrido difícil → arena Golem → fade dorado → créditos (puntuación + contador muertes)
Arte: pixel art 8×8, paleta jungla cálida→oscuro místico→dorado. HUD: contador cristales (X/Total) + contador muertes.
Audio: SFX/música informativa. Silencio en pausa. SFX implementados: gancho · enganche · aterrizaje · salto · alerta · cristal · muerte · frágil (crujido+rotura). Post-MVP: proyectil · placa · roca

## Arquitectura de scripts
| Script | Responsabilidad |
|---|---|
| `PlayerMovement.cs` | Movimiento Celeste-style, salto, gravedad, coyote, jumpQueued, swing |
| `GrappleScript.cs` | Estados gancho, Linecast, snap, Bezier, carga, climb W/S, tracción ReactiveWall |
| `CamaraScript.cs` | Lerp LateUpdate, look-behind horizontal, look-down caída |
| `MovingPlatform.cs` | Lerp lineal, Kinematic, herencia velocidad por posición directa |
| `VoidScript.cs` | Trigger foso → PlayerDied() |
| `LevelExit.cs` | Trigger salida → NivelCompleto() |
| `GameLoopManager.cs` | Singleton: muerte, fade, reinicio, cristales, fin nivel, pausa, victoria |
| `CrystalPickup.cs` | OverlapCircle → CollectCrystal() → desactivar |
| `PatrollerAI.cs` | Estados patrullero, gizmos A↔B, icono "!" |
| `SpikeHazard.cs` | Pincho estático o retráctil 3 fases (Retraido/Asomando/Desplegado) |
| `SpikeGroup.cs` | Instancia y desincroniza grupos de SpikeHazard |
| `OneWayPlatform.cs` | Ignora colisión desde abajo, bajar con S/↓ |
| `ReactiveWall.cs` | Pared movible por gancho, cae al superar distancia |
| `FragilePlatform.cs` | Timer colapso, 3 fases color, regenera |
| `AudioManager.cs` | Singleton SFX espacial + música con crossfade + mixer |
| `LauncherAI.cs` | (post-MVP) Timer disparo, instancia proyectil |
| `Projectile.cs` | (post-MVP) Movimiento, destrucción por tiempo/borde |
| `GolemBoss.cs` | (post-MVP) Estados jefe, fases, victoria |

Post-MVP: `LauncherAI` · `GolemBoss` · `TractionPlatform` · `PressurePlate` · `FallingRock` · `Lever` · `Door` · `DestructibleBeam`
Implementado adelantado (funcional, no prioritario): `OneWayPlatform` · `ReactiveWall` · `SpikeHazard` retráctil · `SpikeGroup`

## Layers
`Floor(3)` isGrounded · `Player(7)` · `Grappleable(6)` raycast gancho · `Obstacle(8)` bloquea raycast · `Enemy(9)` muerte · `Hazard(10)` muerte trigger · `Collectible(11)` recolección
Tags: `Crystal` · `Exit` · `Enemy` · `Hazard` · `SpawnPoint`

## Contratos
**`GameLoopManager.cs`:** `CanvasGroup fadePanel` · `float tiempoFadeOut=0.4f` · `int cristalesTotales` · `GameObject panelFinNivel, panelPausa, panelVictoria` · `int cristalesObtenidos, contadorMuertes` · Respawn = recarga de escena (sin spawnPoint) · API: `PlayerDied()` `CollectCrystal()` `NivelCompleto()` `ContinuarSiguienteNivel()` `Reintentar()` `TogglePause()`

**`PatrollerAI.cs`:** `velocidadPatrulla=2f` · `velocidadPersecucion=4f` · `velocidadRegreso=3f` · `radioDeteccion=5f` · `radioAbandonar=7f` · `duracionIdle=0.6f` · `Transform puntoA, puntoB` · enum `{Idle,Patrulla,Persecucion,Regreso}` · icono "!" visible durante persecución

**`FragilePlatform.cs`:** `breakDelay=1.5f` · `regenDelay=5f` · `SpriteRenderer sr` · `Color[] warningColors` (3 fases) · OnCollision Player → corrutina → desactivar collider+sr → reactivar

## Convenciones
- Comentarios español · Naming inglés estándar Unity
- `[SerializeField] private` · `[Header]` para Inspector · cachear en `Awake/Start`
- Física en `FixedUpdate` · input en `Update` — nunca mezclar
- `Rigidbody2D` — nunca `Transform.Translate` · nunca hardcodear valores de diseño
- Commits: español imperativo corto. Review Bonio antes de merge.
- Git: nunca editar `.meta` a mano · nunca borrar desde Explorer

## Equipo
**Bono Dipacce (Bonio)** — GD/Prog, aprobación final · Fermin Blanco — Prog · Eliel Denmon — Prog/Audio · Belen Almed — Arte sprites/Golem/N6 · Julieta Cerelli — Arte N1-N2 · Santiago Calvo — QA/tuning

## Reglas Claude
1. GDD v3.0 manda — señalar discrepancias antes de actuar
2. No inventar features sin confirmación de Bonio
3. Leer script antes de afirmar comportamientos
4. Responder en español · física en FixedUpdate · input en Update
5. Valores de diseño siempre en SerializeField
6. Tareas GD/Prog requieren review de Bonio antes de marcar completas
