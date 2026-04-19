# CLAUDE.md — Templo Utaki

> Proyecto Unity 2D. Leer este archivo antes de cualquier sesión. **GDD v3.0 (Abril 2026) = fuente autoritativa.** Si código, tareas o docs contradicen al GDD, **gana el GDD**.

---

## 1. Stack técnico

| Componente | Tecnología |
|---|---|
| Motor | Unity `6000.0.30f1` |
| Render | URP 2D `17.0.3` |
| Lenguaje | C# (MonoBehaviour) |
| Input | Unity Input System `1.11.2` |
| Física del gancho | `DistanceJoint2D` + `Rigidbody2D` |
| Cuerda visual | `LineRenderer` — 20 segmentos — **curva Bezier cuadrática** |
| Arte | Aseprite, tiles 8×8 px |
| Audio | A definir por área de sonido |
| VCS | Git — GitHub |
| Repo | `github.com/uadelaburos-Prog/Templo_Utaki` (underscore, NO hyphen) |
| Branch activo | `Camara&Fisicas` (usar comillas en shell) |
| Resolución target | 1920×1080 fija |
| FPS target | 60 FPS estables |
| Idioma de trabajo | Español (commits, docs, comentarios, UI) |

---

## 2. Resumen del juego

Plataformas 2D pixel art (tiles 8×8 px) con **gancho tipo péndulo** como mecánica core. 6 niveles + arena de Jefe Final. 20–30 min de duración total. Un explorador sin nombre entra al Templo Utaki buscando un tesoro legendario custodiado por un Golem de Piedra.

**Narrativa implícita:** sin diálogos, sin cutscenes, sin texto en pantalla. El mundo cuenta la historia. La arquitectura, las trampas y el guardián final comunican que este lugar fue construido para no ser atravesado.

### Pilares de diseño
1. **Movimiento fluido** — el gancho se siente natural desde el primer lanzamiento
2. **Física real** — péndulo con leyes físicas reales, sin fuerzas mágicas asistidas
3. **Accesibilidad** — desafío progresivo, muerte indolora (reinicio, no punición)
4. **Concisión** — 20–30 min puros, sin relleno ni backtracking
5. **Feedback esencial** — todo elemento comunica algo útil, nada decorativo
6. **Legibilidad** — ningún peligro mata por sorpresa en la primera pasada

### Referentes
| Juego | Referencia específica |
|---|---|
| Terraria | Gancho con física de péndulo, encadenamiento aéreo |
| Shovel Knight | Pixel art 2D, progresión por biomas |
| Celeste | Muerte instantánea sin penalización, foco en movimiento |
| Neon White | Movimiento encadenado como habilidad central |

---

## 3. Reglas duras (NO negociables)

- ❌ **No hay sistema de vidas/salud.** Cualquier contacto con enemigo/proyectil/trampa = reinicio instantáneo del nivel.
- ❌ **El jugador NO ataca.** No hay combate ofensivo. Solo evasión y movimiento.
- ❌ **No hay rutas alternativas ni secretos.** Niveles lineales.
- ❌ **No hay checkpoints.** Al morir se reinicia el nivel completo.
- ❌ **No hay texto ni cutscenes en pantalla.** Aprendizaje implícito por diseño de nivel.
- ✅ **Fade-out de muerte < 0.5s.** El jugador debe estar de vuelta en el nivel en < 2s.
- ✅ **Legibilidad obligatoria.** Si el jugador no puede saber a primera vista si una superficie es grappleable o peligrosa, el nivel está mal diseñado.
- ✅ **Arte definitivo desde semana 4.** Hasta entonces placeholder **no primitivo** (no cubos blancos).

---

## 4. Personaje y controles

### 4.1 Parámetros de movimiento (SerializeField en `PlayerMovement.cs`)

| Variable | Valor base | Nota |
|---|---|---|
| `moveSpeed` | 8–10 u/s | Max en 0.5s |
| `jumpForce` | 12 | Velocidad inicial vertical |
| `fallGravity` | 5.5 | Gravedad al caer |
| `maxFallSpeed` | -20 u/s | Velocidad terminal |
| `normalGravity` | 1 | Gravedad base |
| `hangGravity` | 2 | Gravedad reducida en apex |
| `hangTimeThreshold` | 0.1s | Ventana para aplicar hang |
| `coyoteTime` | **0.12s** | Ventana post-borde para saltar |
| `jumpBufferTime` | **0.15s** | Buffer de input de salto |
| `jumpCutMult` | **0.5** | Multiplicador al soltar salto antes del apex |
| Control en aire | Mínimo | Poca corrección en salto |
| Fricción suelo | 0.6 | |
| Colisionador player | Cápsula 0.5u × 1u | |
| Input lag target | <50ms (2–3 frames @60fps) | |

### 4.2 Mapeo de controles

| Acción | Teclado | Gamepad |
|---|---|---|
| Mover L/R | A/D o flechas | Stick izq |
| Saltar | Espacio | A |
| Apuntar gancho | Mouse | Stick der |
| Lanzar gancho | Click izq | RT / botón der |
| Soltar gancho | Soltar click | Soltar botón |
| Pausa | ESC / P | Start |
| Reiniciar | R | X |

---

## 5. Sistema del gancho (mecánica core)

### 5.1 Ciclo de estados
`idle → charging → launching → attached → (swinging) → retracting → idle`

> **Nota (2026-04-19):** se agregó el estado `charging` — el jugador sostiene click para cargar distancia (0% = `minGrappleDistance`, 100% = `maxGrappleDistance`). Ver §5.2 campos `minGrappleDistance`, `maxChargeTime`.

### 5.2 Parámetros (SerializeField en `GrappleScript.cs`)

| Variable | Valor | Nota |
|---|---|---|
| `maxGrappleDistance` | 15u | Distancia máxima de lanzamiento |
| `launchSpeed` | 15–20 u/s | Velocidad de vuelo del gancho |
| `snapRadius` | **1.5u** | Asistencia: engancha al punto válido más cercano al cursor |
| `hookGravity` | 18 | Gravedad aplicada al hook en vuelo |
| `maxSwingVelocity` | 15 u/s | Cap al soltar el gancho |
| Tiempo de viaje | 0.3–0.5s | Según distancia |
| Cooldown si falla | 0.3s | |
| Longitud de cuerda | 2–6u variable | Según punto de anclaje |
| Velocidad angular péndulo | ~50°/s | |
| Amortiguación | 98% energía retenida/frame | |
| Retracción | ~0.2s | Tiempo de vuelta al soltar |

### 5.3 Visualización de la cuerda (Bezier cuadrática)
- **En vuelo:** cuerda sale recta del jugador y sigue el hook en tiempo real. El arco del hook hace que la cuerda cambie de ángulo visiblemente.
- **Enganchada:** curva Bezier cuelga naturalmente. Cuanto más cerca el jugador del anclaje, más cuelga. Cuanto más lejos, más tensa.
- **Retrayendo:** cuerda vuelve al jugador en ~0.2s de forma continua.

### 5.4 Superficies grappleables
- **Solo** superficies en la capa `Grappleable` sirven de anclaje.
- ✅ Grappleables: vigas de metal, roca sólida, cadenas, techo de piedra.
- ❌ NO grappleables: madera podrida, vidrio, vegetación, suelo de tierra.
- Los puntos válidos deben señalizarse visualmente (responsabilidad de Arte + GD).

### 5.5 Tipos de anclaje
| Tipo | Comportamiento |
|---|---|
| **Fijo** | Estático. Distancia constante. Paredes, techos, vigas, columnas. |
| **Móvil** | Se mueve. El jugador es arrastrado con él. Plataformas móviles. |
| **Destructible** | 1–2 usos antes de colapsar. Vigas de madera. El colapso puede abrir pasajes. |
| **Reactivo** | Activa un efecto al tirar del gancho (ver sección 7). |

**Regla:** el jugador debe poder anticipar la consecuencia de un tiro antes de ejecutarlo. Nunca sorprender negativamente.

---

## 6. Plataformas (5 tipos)

| Tipo | Comportamiento |
|---|---|
| **Estática** | Inmóvil, soporta peso indefinidamente. Fricción 0.6. Grappleable en bordes marcados. |
| **Móvil** | Trayectoria lineal/circular/pendular. 0.5–3 u/s. Player mantiene velocidad relativa. Grappleable en movimiento. |
| **Frágil** | Timer 1–2s desde pisada. Advertencia en 3 fases (rojo + grietas). Desaparece. Regenera en 5s. Grappleable pero el timer sigue corriendo. |
| **One-Way** | Atravesable desde abajo, soporta peso desde arriba. Grappleable solo desde arriba. |
| **Tracción** | Suspendida. Se desplaza hacia el jugador mientras el gancho está enganchado y la cuerda en tensión. Al soltar: se detiene o retorna (configurable por nivel). |

---

## 7. Obstáculos, trampas e interactuables

### 7.1 Obstáculos ambientales
| Obstáculo | Comportamiento | Daño |
|---|---|---|
| Pinchos estáticos | Zona fija, rojo intenso | Reinicio |
| Fuego | Zona de área con llama + partículas | Reinicio |
| Foso de vacío | Caída fuera del nivel, oscuridad visible | Reinicio |
| Pinchos retráctiles | Ciclo fijo configurable (ej: 1.5s activo / 1.5s retraído). Color rojo, animación visible | Reinicio en estado activo |

### 7.2 Sistema de activadores (1 activador → 1 efecto)
- **Placa de presión:** activador en suelo. Dispara la trampa vinculada una vez por pisada. Sprite diferenciado, clic mecánico al activarse.
- **Roca cayente:** trampa activada por placa. Cae en trayectoria vertical fija; al impactar el suelo continúa horizontal. Se destruye al chocar pared, no rebota. Señalización: sombra proyectada + crujido estructural previo.

**Principio:** el sistema es extensible. Nuevas trampas = mismo esquema activador → efecto.

### 7.3 Objetos interactuables
**Por proximidad/contacto:**
- **Puerta de inicio:** se abre automáticamente al comenzar nivel (solo estética).
- **Puerta de salida:** se abre al llegar el jugador. Trigger de fin de nivel.
- **Palanca/Switch:** contacto alterna ON/OFF de objeto vinculado.

**Exclusivos del gancho:**
- **Pared reactiva:** enganchar argolla en pared de madera y tirar → cae, abre pasaje.
- **Plataforma de tracción:** enganchar + tensión → se acerca al jugador.
- **Viga destructible:** 1–2 usos → colapsa, puede abrir caídas o mover entorno.

---

## 8. Enemigos

### 8.1 Filosofía
Todos los enemigos son **predecibles por diseño**. Ningún comportamiento aleatorio. El jugador aprende patrones y los evita con habilidad. Foco en movimiento del jugador, no en combate.

### 8.2 Patrullero (`PatrollerAI.cs`)
Estados: `idle → patrulla → alerta → persecución → regreso`

| Parámetro | Valor |
|---|---|
| `patrolSpeed` | 2 u/s |
| `chaseSpeed` | 4 u/s |
| `detectionRadius` | 5u |
| Área de patrulla | Rectangular predefinida |
| Contacto | Reinicio |

**Sprite:** 16×24 px. Paleta: grises de piedra con detalles dorado oxidado.
**Animaciones:** idle (2–4 frames), walk (6–8), alert (2, pausa brusca), chase (walk acelerado), ícono "!" amarillo 8×8 sobre cabeza.

### 8.3 Lanzador (`LauncherAI.cs` + `Projectile.cs`)
Estados: `inactivo → cargando → disparando → (proyectil en vuelo)`

| Parámetro | Valor |
|---|---|
| Posición | Fija — nunca se mueve |
| `fireRate` | 2–3s configurable por nivel |
| Dirección de disparo | Fija, definida en nivel (NO apunta al jugador) |
| `projectileSpeed` | 8 u/s constante |
| Vida del proyectil | 10s o fuera de pantalla |
| Contacto cuerpo/proyectil | Reinicio |

**Sprite:** 16×16 px integrado en arquitectura del templo. Paleta: grises con cañón naranja/rojo.
**Proyectil:** 8×8 px, orbe o rayo con loop de 2–4 frames.

### 8.4 Jefe Final: Golem de Piedra (`GolemBoss.cs`)
Guardián del Templo. Custodia la Cámara del Tesoro. **Estático**, 2 fases con patrones diferenciados.

**Fase 1:**
- Dispara proyectiles de piedra en patrones predecibles y lentos.
- Jugador esquiva balanceándose con gancho entre proyectiles.
- Cadencia lenta, permite leer los patrones.

**Fase 2:**
- Cadencia aumenta.
- Añade ráfagas cortas además de proyectiles individuales.
- Algunas plataformas de la arena comienzan a colapsar.
- El jugador debe moverse constantemente con gancho.

**Victoria:** alcanzar el punto débil durante ventanas entre ataques. Al derrotarse: Golem colapsa, cámara del tesoro se ilumina, pantalla de victoria.

**Sprite:** 48×64 px mínimo. Paleta: roca oscura, musgo, runas doradas, punto débil destacado.
**Animaciones:** idle (4–6 frames, respiración pesada), ataque (8–10), Fase 2 (crack visual + ojos brillantes), muerte (10–12 frames de colapso satisfactorio).

---

## 9. Diseño de niveles

### 9.1 Filosofía
- **Lineal:** un único camino hacia la meta.
- **Aprendizaje implícito:** ningún nivel usa texto. El entorno guía con geometría y posicionamiento. "El diseño habla, el texto calla."
- Cada nivel introduce 1–2 mecánicas nuevas y las combina con las anteriores.

### 9.2 Tabla de niveles

| # | Nombre | Mecánicas nuevas | Enemigos | Duración |
|---|---|---|---|---|
| 1 | **Tutorial — Jungla Inicial** | Movimiento, salto, gancho, pinchos retráctiles, pared reactiva, plataforma de tracción, palanca | Patrullero (1) | 2–3 min |
| 2 | **Entrada del Templo** | Gancho obligatorio, plataformas estáticas complejas | — | 4–5 min |
| 3 | **Cámaras de Piedra** | Gancho + plataformas móviles | — | 4–5 min |
| 4 | **Plataformas Peligrosas** | Gancho + plataformas frágiles + Patrullero | Patrullero | 5–6 min |
| 5 | **Las Profundidades** | Todo lo anterior + Lanzador + trampas | Ambos | 5–6 min |
| 6 | **Cámara del Tesoro** | Prueba de maestría + Arena del Golem | Golem | 3–4 min + boss |

### 9.3 Estructura por beats
Cada nivel se diseña en **beats**: momentos con objetivo claro y mecánica específica. Ej. Nivel 1: plataformas en escalera → gap pequeño → foso de vacío con one-way → descubrimiento del gancho (abismo + anclaje visible) → pared reactiva → pinchos retráctiles → etc.

### 9.4 Nivel 6 (estructura especial)
- **Parte 1 — Recorrido:** secuencia de alta dificultad sin enemigos. Solo arte + gancho. 2–3 min.
- **Parte 2 — Arena del Golem:** sala grande con altura para balancearse. Anclajes en techo y paredes. Golem en centro-fondo. 2–3 min.
- **Victoria:** colapso del Golem, cámara dorada se revela, fade dorado, créditos con puntuación y contador de muertes.

---

## 10. Arte y estética

### 10.1 Filosofía visual
Pixel art 8×8 px por tile. Paleta cálida selvática que evoluciona a tonos oscuros y místicos conforme el jugador desciende. El **contraste entre la jungla exterior (Nivel 1) y la cámara dorada (Nivel 6) es el arco visual del juego.**

### 10.2 Paleta por zona

| Zona | Paleta | Tono |
|---|---|---|
| Exterior — Jungla | Verdes, ocres, luz amarilla | Vibrante, cálido, luz solar filtrada |
| Entrada del Templo | Gris piedra, marrón, antorchas | Transición al interior |
| Profundidades | Grises oscuros, morados, azules fríos | Hostil, opresivo |
| Cámara del Tesoro | Dorados, amarillos brillantes | Espectacular, contraste total |

### 10.3 HUD mínimo
Solo lo esencial. Contador de cristales recolectados. Sin barras de vida, sin mapa, sin texto narrativo.

---

## 11. Audio

### 11.1 Filosofía
Solo SFX y música que comunican información útil. Nada decorativo.

### 11.2 SFX prioritarios
- Lanzar gancho: *whoosh* corto
- Enganche: *clink* metálico
- Aterrizaje: impacto (varía por superficie)
- Salto: *pop* suave
- Alerta enemigo: alarma simple
- Proyectil: *pew*
- Recolectar cristal: *ding*
- Muerte: sonido decreciente
- Plataforma frágil: crujido progresivo + colapso
- Placa de presión: clic mecánico
- Roca cayente: crujido estructural de advertencia

### 11.3 Música
Ambiental por zona, sigue la paleta visual. Se silencia en pausa (solo SFX de UI durante pausa).

---

## 12. Coleccionables y progresión

**Cristales de puntuación** (`CrystalPickup.cs`):
- Trigger con el jugador lo recolecta.
- Comunicación con `LevelManager.cs`.
- Display de "cristales obtenidos / total" en pantalla de fin de nivel.
- Pantalla final muestra puntuación total + contador de muertes.

**Curva de dificultad:** 1–2 enseñan sin presión → 3–4 combinan elementos → 5–6 exigen maestría. Picos con respiros.

---

## 13. Arquitectura de código

### 13.1 Scripts principales

| Script | Estado | Ubicación | Responsabilidad |
|---|---|---|---|
| `PlayerMovement.cs` | ✅ Existe (funcional) | `Assets/Scripts/` | Movimiento horizontal, salto, gravedad variable, coyote time, input buffer, detección de suelo. Incluye momentum jump, swing jump boost. |
| `GrappleScript.cs` | ✅ Existe (funcional) | `Assets/Scripts/` | Ciclo de estados del gancho, Linecast de detección, snap assist, LineRenderer Bezier, carga (0–1.5s), climb W/S, obstacleMask. |
| `CamaraScript.cs` | ✅ Existe (funcional) | `Assets/` | Seguimiento del player con Lerp, look-behind horizontal, look-down por velocidad de caída. |
| `MovingPlatform.cs` | 🟡 Existe (senoidal) | `Assets/` | Plataforma móvil horizontal con `Mathf.Sin` + `MovePosition` |
| `VoidScript.cs` | ✅ Existe | `Assets/` | Trigger que recarga la escena al caer al vacío |
| `PatrollerAI.cs` | ⚪ Falta | — | Máquina de estados del Patrullero: idle, patrulla, alerta, persecución, regreso |
| `LauncherAI.cs` | ⚪ Falta | — | Timer de disparo, instanciación de proyectil, dirección fija |
| `Projectile.cs` | ⚪ Falta | — | Movimiento del proyectil, detección de impacto, destrucción por tiempo o borde |
| `GolemBoss.cs` | ⚪ Falta | — | Máquina de estados del Jefe, fases, patrones de ataque, condición de victoria |
| `LevelManager.cs` | ⚪ Falta | — | Inicio, fin, reinicio, puntuación, cristales, fade-out muerte |
| `CrystalPickup.cs` | ⚪ Falta | — | Trigger de recolección, comunicación con LevelManager |

### 13.2 Sistema de Layers (Unity)

| Layer | Uso | Interacciones |
|---|---|---|
| `Default` | Objetos genéricos | — |
| `Floor` | Suelo y plataformas. Detección de `isGrounded` | Colisiona con Player |
| `Grappleable` | Superficies donde el gancho puede engancharse | Detectada por raycast de `GrappleScript` |
| `Obstacle` | Bloquea el gancho sin engancharlo | Bloquea el raycast. No activa enganche |
| `Enemy` | Enemigos | Colisiona con Player → muerte |
| `Hazard` | Trampas (pinchos, fuego) | Trigger con Player → muerte |
| `Collectible` | Cristales | Trigger con Player |

### 13.3 Estados del juego

| Estado | Comportamiento |
|---|---|
| Menú principal | Estética coherente. Opciones: Jugar, Opciones, Salir |
| En juego | HUD mínimo activo. Arranca desde Nivel 1 |
| Pausa | Tiempo congelado. Opciones: Reanudar, Reintentar, Opciones, Menú. ESC reanuda directo. Solo SFX de UI, música silenciada |
| Muerte del jugador | Sin game over. Reinicio automático del nivel en <2s |
| Fin de nivel | Pantalla breve con cristales obtenidos/total y tiempo. Continuar al siguiente |
| Victoria | Pantalla de créditos con puntuación total y contador de muertes |

---

## 14. Equipo (Demonic Arts Company)

| Nombre | Rol | Área principal |
|---|---|---|
| **Bono Dipacce (Bonio)** | Game Design / Programación | GDD, diseño de niveles, parámetros de mecánicas. **Aprobación final en GD y Programación** |
| Fermin Blanco | Programación | PlayerMovement, plataformas, obstáculos, enemigos estándar |
| Eliel Denmon | Programación / Audio | Sistemas de audio, SFX, música, integración sonora |
| Belen Almed | Arte | Sprites de personaje, enemigos, Golem, Tileset Nivel 6 |
| Julieta Cerelli | Arte | Tilesets Niveles 1 y 2, arte de entorno |
| Santiago Calvo | Producción / QA | Backlog, milestones, testing funcional y de feel, tuning de parámetros |

---

## 15. Convenciones

### Código
- Comentarios en **español**.
- **Naming:** PascalCase para clases, camelCase para variables. Nombres de scripts en inglés estándar Unity.
- `[SerializeField] private` para valores ajustables desde Inspector (todo lo de §4.1 y §5.2 es SerializeField).
- Usar `[Header("...")]` para agrupar en Inspector.
- **Cachear** referencias a componentes en `Awake`/`Start` (nunca `GetComponent` en `Update`/`FixedUpdate`).
- **Física en `FixedUpdate`**, inputs en `Update`.
- Movimiento del jugador vía `Rigidbody2D` (`linearVelocity` o `AddForce`), **nunca `Transform.Translate`**.
- Valores de diseño **siempre** en SerializeField — nunca hardcodeados. Santiago hace tuning desde Inspector.

### Git
- Commits en español, imperativo corto (`Fix: bug de grounding`, `Add: coyote time`).
- Branch por feature.
- Tareas de GD/Prog requieren review de Bonio antes de merge.

### Deliverables
- Documentos de equipo en español.
- PPTX/PDF para entregas formales.
- Placeholder art (no primitivo) hasta semana 4, arte definitivo después.

---

## 16. Milestones

| Hito | Deadline | Criterio |
|---|---|---|
| **Parcial 1** | ~3 semanas | MVP ejecutable con loop jugable + placeholder art, backlog, HLP, GDD, propuesta artística, retrospectiva |
| **Alpha** | 2 semanas post-Parcial 1 | Mecánicas 100% completas |
| **Beta** | — | Contenido completo, pulido |
| **Gold** | — | Release |

**Presentación Parcial 1:** 20 min (10 demo + 10 Q&A). Subir 1 día antes a "Entregas → Parcial 1 → Templo Utaki".

**Grading Parcial 1:** funcionalidad MVP, scope vs pitch, detalle backlog, calidad GDD/propuesta artística, cumplimiento entregas previas, autonomía del equipo. **Última oportunidad para recortar scope o cambiar roles.**

---

## 17. Requisitos mínimos de PC

- SO: Windows 10 / macOS 11 / Ubuntu 20.04
- CPU: Intel Core i3 o equivalente
- RAM: 4 GB
- GPU: soporte OpenGL 4.5
- Almacenamiento: ~200 MB
- Resolución mínima: 1920×1080

---

## 18. Recursos clave

- **GDD v3.0** (Abril 2026): `GDD_TemploUtaki_v4_1__1_.docx` — **fuente autoritativa**
- **Pitch:** `Templo_Utaki_Pitch_Mejorado_.pdf`
- **Backlog:** Google Sheets v3 (85 tareas, 4 milestones, 5 áreas, dependencias tracked)
- **HLP:** `HLP_Templo_Utaki_v4.pptx` + versión HTML interactiva
- **Level editor:** `level_editor_templo_utaki.html` (17 tiles, 3 capas, JSON/PNG export)
- **Drive:** carpeta "Produccion de Videojuegos"

---

## 19. Glosario

| Término | Definición |
|---|---|
| **Coyote Time** | Ventana de 0.12s tras salir de un borde en la que aún se puede saltar |
| **Input Buffer** | Input de salto recordado 0.15s; si aterriza en ese tiempo, el salto se ejecuta |
| **Jump Cut** | Soltar salto antes del apex reduce velocidad vertical ×0.5 |
| **Hang Time** | Gravedad reducida en el apex para dar sensación de control |
| **Snap** | Asistencia de enganche: si hay superficie válida en 1.5u del cursor, engancha al punto más cercano |
| **Grappleable** | Capa de Unity asignada a superficies donde el gancho puede engancharse |
| **DistanceJoint2D** | Componente que simula péndulo manteniendo distancia fija entre dos puntos |
| **Bezier cuadrática** | Curva usada para renderizar la cuerda con cuelgue natural según distancia y tensión |
| **Tile** | Unidad mínima de arte de nivel. En Templo Utaki: 8×8 píxeles |
| **Beat** | Unidad de diseño de nivel: un momento con objetivo claro y mecánica específica |
| **Milestone** | Hito de producción: Prototipo, Alpha, Beta, Gold |
| **MVP** | Minimum Viable Product: versión mínima jugable con loop principal funcionando |

---

## 20. Reglas para Claude

1. **GDD v3.0 manda.** Si una tarea o código contradice el GDD, señalar la discrepancia antes de actuar.
2. **No inventar features.** Si algo no está en el GDD (ej: ataque del jugador, sistema de vida, checkpoints, secretos), no implementarlo sin confirmación de Bonio.
3. **Validar asunciones.** Antes de afirmar que el código tiene X comportamiento, leer el script.
4. **Repo exacto:** `Templo_Utaki` con underscore. Branch `"Camara&Fisicas"` entre comillas en shell.
5. **Idioma:** responder a Bonio y generar outputs en español salvo que pida lo contrario.
6. **Scope discipline:** si algo empuja el scope más allá de lo del GDD, avisar antes de implementar.
7. **Iteración rápida:** Bonio prefiere feedback visual rápido sobre documentación exhaustiva. Salidas simples y limpias > sobrecargadas.
8. **Física en FixedUpdate, input en Update.** Nunca mezclar.
9. **Comentarios en español.** Naming en inglés estándar Unity.
10. **Todo valor de diseño va en SerializeField.** Nunca hardcodear magic numbers — Santiago hace tuning desde Inspector.
11. **Antes de marcar tarea de GD/Prog como completa:** requiere review de Bonio.

---

## 21. Estado actual del código (snapshot 2026-04-19)

> Snapshot actualizado al 2026-04-19. Sesión de alineación con GDD completada.

### 21.1 Bloqueantes de compilación ✅ RESUELTOS

1. ~~`GrappleScript.cs` — fields duplicados~~ → **✅ Eliminados.**
2. ~~`PlayerMovement.cs` — `image.fillAmount`/`grappleTime`~~ → **✅ Eliminados.** Se descartó el sistema "Tiempo de Cuerda".

### 21.2 Desviaciones del GDD 🟡 (pendientes al 2026-04-19)

| Área | Estado | Acción pendiente |
|---|---|---|
| ~~Joint del gancho~~ | ✅ `DistanceJoint2D` con `maxDistanceOnly=true` | — |
| ~~`moveSpeed`/`jumpForce` visibilidad~~ | ✅ `[SerializeField] private` | — |
| ~~`jumpCooldown`~~ | ✅ Removido | — |
| ~~Control del aire~~ | ✅ `MoveTowards` con `freeAirAccel=4`, preserva momentum | — |
| ~~`snapRadius` efectivo~~ | ✅ 1.5u (default 1.5f, sin `*0.5`) | — |
| ~~Cooldown si gancho falla~~ | ✅ 0.3s implementado | — |
| ~~`hookGravity` en vuelo~~ | ✅ 18 u/s², trayectoria parabólica | — |
| ~~`swingDamping`~~ | ✅ 0.02 (98%/frame) | — |
| ~~Feature "Rebote"~~ | ✅ Removida | — |
| ~~Input System package~~ | ✅ Decisión: quedarse con `Input.GetKey` legacy | Desinstalar package desde Unity Editor |
| `moveSpeed` valor script | 🟡 Default 12 (GDD: 8–10) | Ajustar Inspector a 8–10 |
| `jumpForce` valor Inspector | 🟡 Puede ser 15 en escena | Verificar Inspector → poner 12 |
| `fallGravity` Inspector | 🟡 Puede ser 1.5 en escena | Ajustar a 5.5 |
| Colisionador player | 🟡 Puede ser 1×2 en escena | Ajustar a 0.5u×1u en Inspector |
| Layer del gancho | 🟡 `Grapple` (layer 6) | Renombrar → `Grappleable` en Unity Editor |
| Layers faltantes | 🟡 Faltan Obstacle, Enemy, Hazard, Collectible | Crear en Unity Editor |
| `obstacleMask` (nuevo) | 🟡 Field existe, no asignado en Inspector | Asignar `Floor` (mínimo) desde Inspector |

### 21.3 Bugs lógicos

1. ~~`CamaraScript.cs` — `rb` de la cámara en lugar del player~~ → **✅ Arreglado.** `player.GetComponent<Rigidbody2D>()` en `Start`.
2. ~~`CamaraScript.cs` — bloque look-down duplicado~~ → **✅ Eliminado.**
3. ~~`CamaraScript.cs` — `orthographicSize` en Update~~ → **✅ Movido a `Start`.**
4. ~~`GrappleScript.cs` — camera duplicada~~ → **✅ Resuelto al eliminar el bloque duplicado.**
5. ~~`GrappleScript.cs` — `ropeProgressionSpeed` huérfano en escena~~ → 🟡 **Pendiente limpiar en Inspector.**
6. ~~`PlayerMovement.cs` — `maxGrappleTime`, `image`, `counterForce` huérfanos~~ → **✅ Resueltos** (campos eliminados del script).
7. ~~Componente `PlayerMovement` duplicado en Player (escena)~~ → 🟡 **Pendiente eliminar desde Inspector.**
8. **`MovingPlatform.cs`** — Sin `Kinematic` ni parentado, player no hereda velocidad. 🟡 Pendiente (Fase 2).
9. **`VoidScript.cs`** — Sin fade-out. 🟡 Pendiente — se resuelve al crear `LevelManager` (Sesión 4).
10. ~~`isGrounded` en `Update`~~ → **✅ Movido a `FixedUpdate`.**
11. ~~`OverlapBox` del groundCheck hardcodeado~~ → **✅ Reemplazado por `groundCheckWidth=0.45f` / `groundCheckHeight=0.05f` SerializeField.** Gizmo ahora muestra tamaño real y cambia rojo/verde según estado.

### 21.4 Features del GDD NO implementadas ⚪

**Plataformas (3/5 faltan):** Frágil, One-Way, Tracción.
**Enemigos (4/4 faltan):** Patrullero, Lanzador, Proyectil, Golem.
**Obstáculos/Trampas:** Pinchos estáticos, Pinchos retráctiles, Fuego, Foso de vacío (parcial — `VoidScript` lo cubre), Placa de presión, Roca cayente.
**Interactuables:** Puerta de inicio, Puerta de salida, Palanca, Pared reactiva (gancho), Viga destructible (gancho), Plataforma de tracción (gancho).
**Sistemas:** `LevelManager`, `CrystalPickup`, fade-out de muerte <0.5s, pantalla fin de nivel, Menú principal, Pausa (ESC), Victoria/créditos, Contador de muertes.
**Niveles:** 5 de 6 faltan + Arena del Golem. Solo hay `SampleScene` con placeholders.
**Arte:** Sin tileset 8×8, sin sprites de enemigos/Golem, sin animaciones. Assets actuales: círculos (`Circle.prefab`), cuadrados, placeholder `Tiempo de Cuerda`, material de cuerda.
**Audio:** No hay módulo, no hay SFX, no hay música. AudioSource no presente en escena.
**Input:** ✅ Decisión cerrada — quedarse con `Input.GetKey` legacy. Desinstalar package desde Unity Editor.
**Señalización grappleable:** Highlight implementado al cargar el gancho (rango `maxGrappleDistance`). Pendiente: pulso verde a <8u en todo momento (Fase 3).

### 21.5 Lo que funciona y conviene conservar ✅

- **Estados del gancho** (`idle/charging/launching/attached/retracting`) — flujo completo y funcional.
- **Coyote time (0.12s), jump buffer (0.15s), jump cut, gravedad variable** — alineado al GDD §4.1.
- **Momentum jump** (`jumpHBoost=4`, `jumpHBoostThreshold=0.8`) — boost horizontal al saltar en carrera.
- **Swing jump boost** (`swingJumpWindow=0.2s`, `swingJumpBoostMult=1.3`) — +30% altura al saltar post-gancho. Refuerza pilar "movimiento encadenado".
- **Carga del gancho** (click sostenido → `minGrappleDistance` a `maxGrappleDistance`) — añade skill expression. Subir a GDD v3.1.
- **Climb de cuerda** (W/S mientras enganchado, `climbSpeed=6`) — subir/bajar por la cuerda. Subir a GDD v3.1.
- **Trayectoria parabólica del hook** (`hookGravity=18`) — arco realista alineado al GDD §5.2.
- **Linecast de colisión** (grappleable + obstacleMask) — hook rebota en sólidos no-grappleables, retrae con cooldown 0.3s.
- **Highlight al cargar** (rango maxGrappleDistance, color amarillo) — señalización grappleable activa.
- **LineRenderer Bezier cuadrática + onda** con `AnimationCurve` — alineado GDD §5.3. Segmentos: default 20, escena 80.
- **Gizmos de debug** (rango blanco, snapRadius amarillo, punto verde, hook cian, cuerda azul) — excelente para tuning.
- **groundCheck SerializeField** (`groundCheckWidth=0.45`, `groundCheckHeight=0.05`), gizmo rojo/verde en Play mode.
- **Look-behind horizontal de cámara**, **look-down por caída** (ambos funcionando post-bugfix).
- **CapsuleCollider2D + FreezeRotationZ** — configuración correcta.

### 21.6 Escena y assets

- **Escena única:** `Assets/Scenes/SampleScene.unity` (~2755 líneas YAML).
- **Prefabs:** `Circle.prefab` (placeholder genérico, escala 0.5, SpriteRenderer).
- **Materiales:** `Cuerda.mat` + `Cuerda.png` (material del LineRenderer).
- **Escena contiene:** Player (CapsuleCollider — verificar tamaño 0.5×1u, Rigidbody2D, DistanceJoint2D, LineRenderer, PlayerMovement, GrappleScript), GroundChecker (hijo del player), CamaraFollow + Main Camera (CamaraScript), Square×3 (Floor), Circle×8 (layer Grapple→renombrar Grappleable), PatrolPoint×2 (sin AI aún), Void (VoidScript), Global Light 2D, EventSystem.
- **Layers definidas:** Default(0), TransparentFX(1), Ignore Raycast(2), Floor(3), Water(4), UI(5), Grapple(6), Player(7). **Pendiente en Editor: renombrar Grapple→Grappleable, crear Obstacle, Enemy, Hazard, Collectible.**
- **Sorting Layers:** solo "Default". Agregar Background/Foreground cuando entre el arte.
- **Valores del Inspector — verificar y alinear:**
  - `PlayerMovement`: `moveSpeed: 8–10`, `jumpForce: 12`, `fallGravity: 5.5`. Eliminar `jumpCooldown` si queda en escena.
  - `GrappleScript`: `snapRadius: 1.5`, `swingDamping: 0.02`, `hookGravity: 18`, `maxFlightTime: 0.6`, `failCooldown: 0.3`. Asignar `obstacleMask` (Floor mínimo). `grappleLayerMask`: actualizar a layer Grappleable cuando se renombre.
  - `CapsuleCollider2D`: `size: (0.5, 1)`.
  - Eliminar `ropeProgressionSpeed` huérfano de la escena si persiste.

### 21.7 Decisiones cerradas (2026-04-19)

1. ~~`SpringJoint2D` vs `DistanceJoint2D`~~ → **✅ DistanceJoint2D** con `maxDistanceOnly=true`.
2. ~~Feature "Rebote"~~ → **✅ Removida.**
3. ~~Sistema "Tiempo de Cuerda"~~ → **✅ Descartado.** Fields eliminados del script.
4. ~~Input legacy vs Input System~~ → **✅ Quedarse con `Input.GetKey` legacy.** Desinstalar package desde Unity Editor.
5. ~~Componente `PlayerMovement` duplicado~~ → **🟡 Pendiente eliminar desde Inspector.**
6. ~~Organización de scripts~~ → **🟡 Pendiente mover desde Editor** (subcarpetas Player, Camera, World, Enemies, Systems).
7. **4 features nuevas** (carga gancho, climb, swing jump, momentum jump) → **✅ Mantener. Pendiente documentar en GDD v3.1.**

### 21.8 Plan priorizado rumbo a Parcial 1

> Orientado a MVP jugable. Cada grupo es revisable por Bonio antes de avanzar al siguiente.

**Fase 0 — Desbloqueo ✅ COMPLETO:**
1. ✅ Compilación arreglada (GrappleScript dedupe, PlayerMovement image/grappleTime).
2. ✅ Bugs CamaraScript arreglados (rb, duplicado, orthographicSize).
3.✅ Eliminar `PlayerMovement` duplicado del Player en escena (hacer en Inspector).
4. ✅ Decisiones de joint e input cerradas.

**Fase 1 — Alineación con GDD ✅ MAYORMENTE COMPLETO:**
5. ✅ Visibilidad `[SerializeField] private` en `PlayerMovement`.
6. 🟡 Consolidar scripts en subcarpetas (hacer desde Editor).
7. ✅ Renombrar `Grapple` → `Grappleable`. Crear layers `Obstacle`, `Enemy`, `Hazard`, `Collectible` (hacer desde Editor).
8. ✅ `snapRadius` 1.5u, cooldown 0.3s, `hookGravity` 18, `swingDamping` 0.02. 🟡 Inspector: verificar `jumpForce`→12, `fallGravity`→5.5, colisionador→0.5×1.
9. ✅ Cooldown 0.3s implementado.
10. ✅ Feature "Rebote" removida.
11. ✅ `jumpCooldown` removido.
12. ✅ `obstacleMask`: hook colisiona con todo sólido, retrae en no-grappleables.
13. ✅ `groundCheckWidth/Height` SerializeField (default 0.45×0.05). Gizmo rojo/verde.

**Fase 2 — MVP (2–3 sesiones, pre-Parcial 1):**
12. `LevelManager.cs` con fade-out muerte <0.5s, reinicio <2s, contador de muertes.
13. `CrystalPickup.cs` + HUD mínimo (contador de cristales obtenidos/total).
14. **Nivel 1 (Tutorial — Jungla Inicial)** jugable con placeholders no-primitivos (usar el level editor HTML).
15. Una plataforma Frágil funcional como prueba del sistema.
16. Un Patrullero funcional (sin animación final) para validar `PatrollerAI.cs`.
17. Pantalla de fin de nivel (cristales + tiempo + botón Continuar).

**Fase 3 — Pulido demo (1 sesión):**
18. Pausa (ESC) con menú mínimo.
19. Señalización visual grappleable (highlight verde a <8u).
20. Feedback visual crítico: destello al lanzar, cambio de color al enganchar, fade-out muerte, "!" sobre enemigo alerta.
21. Build PC 1920×1080 para demo de Parcial 1.

**Fuera de Parcial 1 (Alpha y después):** Niveles 2-6, Lanzador, Proyectil, Golem, todas las trampas, todo el audio, arte definitivo.

---

## 22. Plan de implementación hacia MVP (Parcial 1)

> Convierte el diagnóstico de §21 en un plan ejecutable por sesiones. Cada sesión tiene DoD revisable por Bonio antes de avanzar (§15).

### 22.1 Definición del MVP (Parcial 1)

Demo jugable de 2–3 minutos que cumple:

- **Loop completo:** arranque → jugar → morir → reiniciar → terminar → pantalla fin.
- **Nivel 1 "Tutorial - Jungla Inicial"** jugable end-to-end con placeholders **no primitivos**.
- **Gancho funcional** (lanzar, enganchar, péndulo, retraer) con feel correcto.
- **Salto fluido** (coyote 0.12s, buffer 0.15s, jump cut 0.5, gravedad variable) — ya existe.
- **Muerte por contacto** → fade-out <0.5s → reinicio <2s, sin vidas ni game over.
- **1 Patrullero** visible y evitable.
- **1 Plataforma Frágil** funcional.
- **≥3 cristales** recolectables con HUD.
- **Pantalla fin de nivel:** cristales obtenidos/total + tiempo + botón "Continuar".
- **Build PC 1920×1080** ejecutable standalone.

Todo lo no listado (Niveles 2-6, Lanzador, Golem, audio definitivo, arte final) = **post-Parcial 1**.

### 22.2 Necesidades vs inventario

| # | Necesidad MVP | Estado | Acción |
|---|---|---|---|
| 1 | Proyecto compila | ✅  | Fase 0 — dedupe fields |
| 2 | Player alineado al GDD | ✅ | Edits §22.4.1 y §22.4.2 |
| 3 | Cámara sin bugs | 🟡 Bug `rb` | Edit §22.4.3 |
| 4 | Layers correctas | 🟡 Grapple→Grappleable + 4 nuevas | §23.3 |
| 5 | Input coherente | 🟡 Legacy vs System | Decisión §21.7.4 |
| 6 | Nivel 1 jugable | 🔴 Solo `SampleScene` | Crear `Level_01_JunglaInicial.unity` |
| 7 | LevelManager | 🔴 No existe | §22.5.1 |
| 8 | Cristales | 🔴 No existen | §22.5.2 |
| 9 | Patrullero | 🔴 No existe | §22.5.3 |
| 10 | Plataforma Frágil | 🔴 No existe | §22.5.4 |
| 11 | Fade-out muerte | 🔴 No existe | Integrado en LevelManager |
| 12 | HUD cristales | 🔴 No existe | Canvas + TMP_Text |
| 13 | Pantalla fin de nivel | 🔴 No existe | Canvas dedicado |
| 14 | Build standalone | 🔴 No configurado | §23.11 |

### 22.3 Roadmap detallado por sesiones

Cada sesión apunta a un entregable revisable. Todos los valores siguen §4.1 y §5.2.

#### Sesión 1 — Desbloqueo (Fase 0)
**Objetivo:** proyecto compila y corre sin romperse.
1. `GrappleScript.cs` — borrar el segundo bloque duplicado de fields (líneas 29-35). Dejar solo el primero (20-26).
2. `PlayerMovement.cs` línea 125 — **decisión binaria**:
   - **A (default):** borrar el bloque `image.fillAmount += ...; grappleTime = 0f;` y el `using UnityEngine.UI` si queda huérfano.
   - **B (si Bonio quiere "Tiempo de Cuerda"):** declarar `[SerializeField] private Image image;` y `private float grappleTime;`, agregar lógica de consumo/regeneración.
3. `CamaraScript.cs` — en `Start()` reemplazar `rb = GetComponent<Rigidbody2D>()` por `rb = player.GetComponent<Rigidbody2D>()`. Quitar `[RequireComponent(typeof(Rigidbody2D))]`. Eliminar bloque duplicado de look-down.
4. En la escena Player → eliminar el `PlayerMovement` duplicado.
5. Cerrar decisiones §21.7.1 (joint) y §21.7.4 (input) con Bonio.

**DoD:** Console sin errores ni warnings, Play mode corre, gancho funciona, cámara sigue al player.

#### Sesión 2 — Alineación GDD del Player (Fase 1 parte A)
**Objetivo:** valores y estructura alineados al GDD.
6. `PlayerMovement.cs`: `public` → `[SerializeField] private` en `moveSpeed` y `jumpForce`. Mover cálculo de `isGrounded` a `FixedUpdate`.
7. Reorganizar scripts en subcarpetas:
   - `Assets/Scripts/Player/` ← `PlayerMovement.cs`, `GrappleScript.cs`
   - `Assets/Scripts/Camera/` ← `CamaraScript.cs`
   - `Assets/Scripts/World/` ← `MovingPlatform.cs`, `VoidScript.cs`
   - `Assets/Scripts/Enemies/` (vacía, para Sesión 5)
   - `Assets/Scripts/Systems/` (vacía, para Sesión 4)
   - **Mover siempre desde el Editor, no desde Explorer** (Unity ajusta `.meta`).
8. `Project Settings → Tags and Layers`:
   - Renombrar layer 6 `Grapple` → `Grappleable`.
   - Crear `Obstacle`, `Enemy`, `Hazard`, `Collectible`.
9. Inspector del Player — alinear a GDD:
   - `jumpForce: 15 → 12`
   - `fallGravity: 1.5 → 5.5`
   - `CapsuleCollider2D.size: (1, 2) → (0.5, 1)`
   - `snapRadius` efectivo → 1.5u (script usa `snapRadius * 0.5`; poner `snapRadius = 3` o revisar fórmula).
10. Remover `jumpCooldown` del script y de la escena.

**DoD:** Feel del salto similar a Celeste; colisionador del player a tamaño GDD; layers renombradas.

#### Sesión 3 — Alineación GDD del Gancho (Fase 1 parte B)
**Objetivo:** gancho 100% alineado al GDD §5.
11. Si decisión §21.7.1 = `DistanceJoint2D`: reemplazar `SpringJoint2D` en script y en GameObject. Configurar `maxDistanceOnly = true`, `autoConfigureDistance = false`.
12. Implementar **cooldown 0.3s** al fallar el raycast. Timer en `Update`, bloquea `GrappleLaunch` mientras >0.
13. Implementar **`hookGravity = 18`** en vuelo — reemplazar `MoveTowards` lineal por integración de velocidad con gravedad en `UpdateLaunching`.
14. Recalibrar `swingDamping`: `linearDamping` ~0.02 al engancharse (98% retenido/frame).
15. Feature "Rebote" (§21.7.2): si se descarta, borrar `bounceLayerMask` + bloque `Vector2.Reflect`.
16. Limpiar fields huérfanos en escena: `ropeProgressionSpeed`, `maxGrappleTime`, `image`, `counterForce`.

**DoD:** péndulo rígido tipo cuerda, hook con arco parabólico, cooldown correcto.

#### Sesión 4 — Systems y muerte (Fase 2 parte A)
**Objetivo:** sistema de muerte + HUD mínimo.
17. Crear `Assets/Scripts/Systems/LevelManager.cs` (§22.5.1).
18. Crear `Assets/Scripts/Systems/CrystalPickup.cs` (§22.5.2).
19. Canvas `HUD` en escena con `TMP_Text` "Cristales: X/Y" + `Image` negro fullscreen (alpha 0) para el fade.
20. Refactor `VoidScript.cs`: reemplazar `SceneManager.LoadScene` por `LevelManager.Instance.PlayerDied()`.
21. Crear prefab `Crystal` (CircleCollider2D isTrigger + SpriteRenderer placeholder + `CrystalPickup`).
22. Primer test de build standalone (aunque sea solo con SampleScene) para detectar problemas de URP/plataforma temprano.

**DoD:** muerte → fade <0.5s → reaparece en spawn <2s; HUD cuenta cristales.

#### Sesión 5 — Nivel 1 + Patrullero + Frágil (Fase 2 parte B)
**Objetivo:** demo jugable con enemigo y plataforma peligrosa.
23. Renombrar `SampleScene.unity` → `Sandbox_Mecanicas.unity` (playground, no MVP).
24. Crear escena `Assets/Scenes/Level_01_JunglaInicial.unity`. Copiar Player, cámara, Global Light 2D, EventSystem, Canvas HUD.
25. Armar beat-map del Nivel 1 (GDD §9.3): entrada → escalera → gap → abismo con gancho → pared reactiva (placeholder, sin mecánica completa en MVP) → pinchos estáticos → meta con cristal final.
26. Crear `Assets/Scripts/Enemies/PatrollerAI.cs` (§22.5.3). Colocar 1 instancia.
27. Crear `Assets/Scripts/World/FragilePlatform.cs` (§22.5.4). Colocar 1 instancia.
28. Colocar 3 cristales en ubicaciones que requieran usar gancho.
29. Canvas `EndOfLevel` (inactivo al inicio); se activa en el trigger de salida mostrando cristales/tiempo + botón "Continuar".
30. Añadir `Sandbox_Mecanicas` y `Level_01_JunglaInicial` al Build Settings.

**DoD:** demo jugable 2–3 min, enemigo visible, plataforma frágil, 3 cristales, pantalla fin.

#### Sesión 6 — Pulido y Build (Fase 3)
**Objetivo:** ejecutable demostrable.
31. Highlight de superficies grappleables cuando `dist(player, superficie) < 8u` (modificar `SpriteRenderer.color` con pulso verde).
32. Fade-out muerte tweeneado (manual con `Mathf.Lerp` + corrutina; DOTween opcional si el equipo lo banca).
33. Pausa con ESC: Canvas `PauseMenu`, `Time.timeScale = 0f`, botones Reanudar/Reintentar/Menú.
34. Feedback visual: destello al lanzar, color del LineRenderer al enganchar, ícono "!" sobre Patrullero en estado alerta.
35. Build PC 1920×1080 (§23.11). Probar en máquina ajena si hay tiempo.

**DoD MVP:** build standalone corre en PC, demo cumple §22.1.

### 22.4 Edits a scripts existentes

#### 22.4.1 `PlayerMovement.cs`
| Cambio | Zona | Razón |
|---|---|---|
| `public float moveSpeed` → `[SerializeField] private float moveSpeed = 8f` | Declaración | GDD §4.1 + regla §15 |
| `public float jumpForce` → `[SerializeField] private float jumpForce = 12f` | Declaración | GDD §4.1 + regla §15 |
| Borrar o implementar `image.fillAmount` + `grappleTime` | ~línea 125 | Bloqueante compilación |
| Mover `isGrounded = Physics2D.OverlapBox(...)` a `FixedUpdate` | Update → FixedUpdate | Regla §15 |
| Blend de input aéreo (`Lerp` sobre `velocity.x`), no reemplazo | Control en aire | GDD §4.1 "control aire mínimo" |
| Eliminar `jumpCooldown`, `jumpCooldownTimer`, `jumpReady` | Declaración y uso | No figura en GDD |
| Verificar que `isHanging` sigue siendo proxy a `GrappleScript.isGrappling` | `update gravity` | Consistencia estado gancho |

#### 22.4.2 `GrappleScript.cs`
| Cambio | Zona | Razón |
|---|---|---|
| Borrar segundo bloque de fields duplicados | Líneas 29-35 | Bloqueante compilación |
| `SpringJoint2D` → `DistanceJoint2D` (si decisión §21.7.1) | Field `joint` + componente en GameObject | GDD §5 |
| Implementar `cooldownTimer` 0.3s tras fallo de raycast | Inicio de `GrappleLaunch()` | GDD §5.2 |
| Hook en vuelo con `hookGravity = 18` | `UpdateLaunching()` | GDD §5.2 |
| `linearDamping = 0.02` al engancharse (98% retenido) | `SnapAndAttach()` | GDD §5.2 |
| Borrar `bounceLayerMask` + `Vector2.Reflect` si §21.7.2 = remover | `UpdateLaunching()` | No figura en GDD |
| Unificar referencia de cámara: borrar field `cam` o usar siempre `cam` en lugar de `Camera.main` | `GrappleLaunch()` + gizmos | §21.3.4 |

#### 22.4.3 `CamaraScript.cs`
| Cambio | Zona | Razón |
|---|---|---|
| `rb = GetComponent<Rigidbody2D>()` → `rb = player.GetComponent<Rigidbody2D>()` | `Start()` | Bug §21.3.1 |
| Quitar `[RequireComponent(typeof(Rigidbody2D))]` | Clase | Residuo del bug |
| Eliminar bloque duplicado de look-down | `FixedUpdate` | §21.3.2 |
| Mover `cam.orthographicSize = orthoSize` a `Start` u `OnValidate` | Update → Start | §21.3.3 |

#### 22.4.4 `MovingPlatform.cs`
| Cambio | Razón |
|---|---|
| Soportar modo lineal (ida y vuelta entre waypoints) como alternativa al senoidal | GDD §6 "lineal/circular/pendular" |
| `Rigidbody2D.bodyType = Kinematic` + `MovePosition` | Herencia de velocidad al Player |
| Aplicar velocidad relativa al Player al pararse encima (OnCollisionStay2D o parentado temporal) | GDD §6 "velocidad relativa" |

#### 22.4.5 `VoidScript.cs`
| Cambio | Razón |
|---|---|
| Reemplazar `SceneManager.LoadScene(...)` por `LevelManager.Instance.PlayerDied()` | GDD §3 fade <0.5s |

### 22.5 Scripts nuevos (esqueletos)

Implementación concreta en sesiones 4-5. Aquí solo contrato de cada script.

#### 22.5.1 `Assets/Scripts/Systems/LevelManager.cs`
**Responsabilidad:** singleton por escena. Controla inicio, muerte, reinicio, fin de nivel, contadores y fade-out.
**Fields esperados:**
- `[SerializeField] private CanvasGroup fadeCanvas;`
- `[SerializeField] private float fadeDuration = 0.4f;`
- `[SerializeField] private Transform spawnPoint;`
- `[SerializeField] private int totalCrystals;`
- `[SerializeField] private GameObject endOfLevelPanel;`
- `private int crystalsCollected;`
- `private int deathCount;`
- `private float levelStartTime;`

**API pública:** `PlayerDied()`, `CollectCrystal()`, `LoadNext()`, `RestartLevel()`, `ShowEndOfLevel()`.

#### 22.5.2 `Assets/Scripts/Systems/CrystalPickup.cs`
**Responsabilidad:** trigger con Player → `LevelManager.CollectCrystal()` → desactivar.
**Fields:** `[SerializeField] private AudioClip sfxPickup;` (cableado aunque el audio llegue después).
**Lifecycle:** `OnTriggerEnter2D(Collider2D other)` chequea `other.CompareTag("Player")`.

#### 22.5.3 `Assets/Scripts/Enemies/PatrollerAI.cs`
**Responsabilidad:** máquina de estados del Patrullero (GDD §8.2).
**Fields esperados:**
- `[SerializeField] private float patrolSpeed = 2f;`
- `[SerializeField] private float chaseSpeed = 4f;`
- `[SerializeField] private float detectionRadius = 5f;`
- `[SerializeField] private Transform pointA;`
- `[SerializeField] private Transform pointB;`
- `[SerializeField] private LayerMask playerLayer;`
- `private Transform player;` (cache en `Start` via `GameObject.FindWithTag("Player")`).

**Estados:** `enum State { Idle, Patrol, Alert, Chase, Return }`.
**Colisión:** `OnCollisionEnter2D` / `OnTriggerEnter2D` con Player → `LevelManager.Instance.PlayerDied()`.
**Debug:** `OnDrawGizmos` con ruta A↔B y radio de detección.
**Versión mínima MVP:** solo Patrol entre A y B (sin Alert/Chase) — si choca con el calendario.

#### 22.5.4 `Assets/Scripts/World/FragilePlatform.cs`
**Responsabilidad:** colapsa 1–2s después de ser pisada (GDD §6).
**Fields:**
- `[SerializeField] private float breakDelay = 1.5f;`
- `[SerializeField] private float regenDelay = 5f;`
- `[SerializeField] private SpriteRenderer sr;`
- `[SerializeField] private Color[] warningColors;` (3 fases: verde/amarillo/rojo).
- `private bool triggered;`

**Lifecycle:** `OnCollisionEnter2D` con Player inicia corrutina `Countdown()` que interpola color, al `breakDelay` desactiva `Collider2D` + `SpriteRenderer`, tras `regenDelay` reactiva.

#### 22.5.5 Post-MVP (diferidos)
`LauncherAI.cs`, `Projectile.cs`, `GolemBoss.cs`, `OneWayPlatform.cs`, `TractionPlatform.cs`, `PressurePlate.cs`, `FallingRock.cs`, `Lever.cs`, `Door.cs`, `ReactiveWall.cs`, `DestructibleBeam.cs`, `RetractableSpikes.cs`. Documentados en GDD §6-8 para Alpha.

### 22.6 Cambios a las escenas

- **`SampleScene.unity` → `Sandbox_Mecanicas.unity`** — playground de testing, no entra al MVP jugable pero sí al Build Settings como índice 0 (para debug).
- **`Level_01_JunglaInicial.unity`** — escena nueva del MVP. Jerarquía mínima:
  - `Player` (prefabear el GO del Sandbox).
  - `MainCamera` + `CameraFollow`.
  - `Grid` (vacío ahora; Tilemap cuando entre el arte).
  - `Level/Ground/*` — plataformas estáticas (layer `Floor`).
  - `Level/Grappleables/*` — anclajes (layer `Grappleable`).
  - `Level/Platforms/Fragile_01` (layer `Floor`, script `FragilePlatform`).
  - `Level/Enemies/Patroller_01` (layer `Enemy`, script `PatrollerAI`, con `pointA/pointB` hijos).
  - `Level/Collectibles/Crystal_01..03` (layer `Collectible`, script `CrystalPickup`).
  - `Level/Hazards/Spikes_01` (layer `Hazard`, trigger que llama `LevelManager.PlayerDied()`).
  - `Level/Triggers/LevelExit` (trigger que llama `LevelManager.ShowEndOfLevel()`).
  - `SpawnPoint` (empty, referenciado por `LevelManager`).
  - `Canvas_HUD` (TMP `CrystalsCounter`, `FadeImage` negro alpha 0).
  - `Canvas_EndOfLevel` (inactivo al inicio).
  - `LevelManager` (empty, script, referencias cableadas).
  - `Global Light 2D`, `EventSystem`.

### 22.7 Definition of Done del MVP

Una demo es MVP-válida si al correr el build:
- [ ] Proyecto compila sin errores ni warnings nuevos.
- [ ] Al abrir el `.exe`, arranca en 1920×1080 fullscreen.
- [ ] Jugador se mueve, salta y usa el gancho.
- [ ] Jugador muere al tocar al Patrullero y reaparece <2s.
- [ ] Plataforma Frágil colapsa tras pisada.
- [ ] Los 3 cristales se recolectan e incrementan el HUD.
- [ ] Al tocar la meta aparece la pantalla de fin con cristales/tiempo/botón Continuar.
- [ ] ESC pausa el juego con `Time.timeScale = 0f`.
- [ ] R reinicia el nivel actual.
- [ ] Sin crashes en 5 playthroughs consecutivos.

### 22.8 Riesgos y mitigaciones

| Riesgo | Prob. | Mitigación |
|---|---|---|
| `DistanceJoint2D` rompe el feel actual | Media | Branch `feel-springjoint`; A/B comparar; volver si Bonio prefiere Spring |
| Migración a Input System atrasa > 1 sesión | Alta | Diferir a Alpha si choca con Parcial 1; Legacy es aceptable para MVP |
| Tilemap + Grappleable dependen de arte | Alta | MVP con colliders a mano sobre placeholders; Tilemap en Alpha |
| Patrullero completo quema una sesión entera | Media | Versión mínima Patrol A↔B sin Alert/Chase; subir a completo en Alpha |
| Fade-out no llega a tiempo | Baja | Fallback: flash blanco 1 frame + reload |
| Build standalone falla por URP 2D | Baja | Build de prueba en Sesión 4, no en Sesión 6 |
| Componente duplicado + field huérfano rompen al mover scripts | Media | Tocar scripts SIEMPRE desde el Editor, nunca Explorer |

---

## 23. Instructivo de Unity 6000.0.30f1

> Paso a paso para levantar, configurar y build-ear el proyecto en Unity 6000.0.30f1. Complementa §22.

### 23.1 Abrir el proyecto

1. Abrir **Unity Hub**.
2. Verificar que el Editor `6000.0.30f1` esté instalado (Installs → Add si falta).
3. `Projects → Add → Add project from disk` → elegir `C:\Users\Casto\Desktop\Templo_Utaki`.
4. Primer abre puede tardar 3–10 min (importa shaders URP, compila scripts).
5. Si la Console muestra errores rojos: **no entrar en Play Mode**; resolver antes (§21.1).

### 23.2 Project Settings esenciales

`Edit → Project Settings`:
- **Player → Resolution and Presentation:** `Fullscreen Mode = Fullscreen Window`, `Default Screen Width = 1920`, `Height = 1080`, `Resizable Window = off`.
- **Player → Other Settings:** `Color Space = Linear`, `Api Compatibility Level = .NET Standard 2.1`.
- **Physics 2D:** `Gravity Y = -30` (matchea tune de `fallGravity=5.5`). `Queries Start In Colliders = off`.
- **Time:** `Fixed Timestep = 0.02` (50 Hz estándar). No tocar.
- **Graphics:** verificar que `Scriptable Render Pipeline Settings` apunte al URP Asset 2D.
- **Input System Package (solo si se migra):** `Active Input Handling = Both` o `Input System Package`.

### 23.3 Layers (Tags and Layers)

`Edit → Project Settings → Tags and Layers`:
- **Renombrar** layer 6 `Grapple` → `Grappleable`.
- **Crear:**
  - 8 → `Obstacle`
  - 9 → `Enemy`
  - 10 → `Hazard`
  - 11 → `Collectible`
- **Layer Collision Matrix** (`Physics 2D → Layer Collision Matrix`):

| | Floor | Grappleable | Obstacle | Enemy | Hazard | Collectible | Player |
|---|---|---|---|---|---|---|---|
| Player | ✅ | ✅ | ✅ | ✅ (contact) | ✅ (trigger) | ✅ (trigger) | — |
| Enemy | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | ✅ |
| Collectible | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Hazard | ❌ | ❌ | ❌ | ❌ | — | ❌ | ✅ |

### 23.4 Tags

`Tags and Layers → Tags`:
- Verificar: `Player`, `Floor`, `MainCamera` (ya existen).
- Crear: `Crystal`, `Exit`, `Enemy`, `Hazard`, `LevelManager`, `SpawnPoint`.

### 23.5 Input System (solo si §21.7.4 = migrar)

1. `Window → Package Manager → Unity Registry → Input System` — verificar 1.11.2 instalado.
2. Prompt "Enable new Input System" → aceptar → Unity reinicia.
3. Abrir `Assets/InputSystem_Actions.inputactions` (ya existe).
4. Action Map `Gameplay` con Actions:
   - `Move` (Value Vector2)
   - `Jump` (Button)
   - `Grapple` (Button)
   - `Aim` (Value Vector2)
   - `Pause` (Button)
   - `Restart` (Button)
5. Bindings: teclado (WASD/flechas, Space, Click, Mouse Position, ESC, R) + gamepad (stick izq, A, RT, stick der, Start, X).
6. Marcar `Generate C# Class` → destino `Assets/Scripts/Player/InputActions.cs`.
7. En `PlayerMovement.cs` y `GrappleScript.cs`: sustituir `Input.GetKey(...)` por `inputActions.Gameplay.Move.ReadValue<Vector2>()`, etc.

### 23.6 URP 2D Renderer

1. `Window → Rendering → Render Pipeline Converter` — verificar que no queden materiales Built-in.
2. En escena, `Main Camera → Rendering → Renderer` debe apuntar al `2D Renderer`.
3. `Global Light 2D` debe existir en la escena (ya existe en SampleScene).

### 23.7 Build Settings

1. `File → Build Profiles` (Unity 6).
2. Platform: `Windows, Mac, Linux` → `Switch Platform` si no está.
3. Target: `Windows`, Arch: `x86_64`.
4. **Scenes in Build:**
   - `Assets/Scenes/Sandbox_Mecanicas.unity` (índice 0 — dev).
   - `Assets/Scenes/Level_01_JunglaInicial.unity` (índice 1 — MVP).
   - (Futuras: Level_02..Level_06, Arena_Golem).
5. **Compression Method:** `LZ4`.
6. **Development Build:** on durante desarrollo; **off** para el entregable final.

### 23.8 Crear un script desde el Editor

1. En Project, click derecho en la carpeta destino → `Create → MonoBehaviour Script`.
2. Nombrar en PascalCase (`LevelManager`, no `level_manager`).
3. Doble click para abrir en el IDE configurado (Rider / VS / VSCode).
4. Convenciones §15: comentarios en español, naming en inglés Unity, `[SerializeField] private` para valores ajustables.

### 23.9 Crear una escena

1. `File → New Scene → 2D (URP)` Template.
2. `Save As` → `Assets/Scenes/Level_XX_Nombre.unity`.
3. Camera: `Orthographic`, `Size ~5.4` (para 1080p @ 100 PPU), `Background = Solid Color`.
4. Añadirla a Build Settings (§23.7).

### 23.10 Armar Nivel 1 con placeholders

1. Abrir `Level_01_JunglaInicial.unity`.
2. Arrastrar prefab `Player` desde `Assets/Prefabs/` (prefabear primero si no existe).
3. Crear `Level/Ground`: sprites cuadrados escalados como plataformas, `BoxCollider2D`, layer `Floor`.
4. Crear `Level/Grappleables`: circles del Sandbox copiados, layer `Grappleable`.
5. `Level/Enemies/Patroller_01`: GameObject vacío con `PatrollerAI`, `Rigidbody2D Kinematic`, `CapsuleCollider2D`; hijos `PointA` y `PointB` vacíos como waypoints.
6. `Level/Platforms/Fragile_01`: sprite con `BoxCollider2D`, `FragilePlatform`, layer `Floor`.
7. `Level/Collectibles/Crystal_01..03`: prefab `Crystal` duplicado en 3 posiciones que requieran gancho.
8. `Level/Hazards/Spikes_01`: sprite con `BoxCollider2D isTrigger`, layer `Hazard`, script pequeño que llame `LevelManager.Instance.PlayerDied()` en `OnTriggerEnter2D` con Player.
9. `Level/Triggers/LevelExit` al final del nivel.
10. `Canvas_HUD`: Canvas `Screen Space - Overlay`, `TMP_Text "Cristales: 0/3"`, `FadeImage` negro alpha 0 fullscreen.
11. `Canvas_EndOfLevel`: inactivo al inicio.
12. `LevelManager`: GameObject vacío con script, referencias cableadas (fadeCanvas, spawnPoint, totalCrystals, endOfLevelPanel).

### 23.11 Build final PC

1. `Ctrl + S` (guardar escena).
2. `File → Build Profiles → Build`.
3. Destino: `Builds/MVP_Parcial1/` (**fuera** de `Assets/`).
4. Unity genera `.exe` + `Data/`.
5. Probar `.exe` doble click: debe arrancar en 1920×1080 fullscreen.
6. Para debug: marcar "Development Build" + "Script Debugging".

### 23.12 Atajos útiles

| Atajo | Acción |
|---|---|
| `Ctrl + P` | Play / Stop |
| `Ctrl + Shift + P` | Pause |
| `Ctrl + S` | Guardar escena |
| `F` | Focus en GameObject seleccionado |
| `W / E / R / T / Y` | Tool Move / Rotate / Scale / Rect / Transform |
| `Ctrl + D` | Duplicar |
| `Ctrl + Z` | Undo |
| `Alt + click` en Hierarchy | Colapsar hijos |
| `Ctrl + Shift + F` | Mover GameObject a la view actual |
| `V + drag` | Vertex snap |

### 23.13 Git con este repo

El nombre de branch tiene `&`, así que hay que citarlo siempre:

```bash
git clone --branch "Camara&Fisicas" https://github.com/uadelaburos-Prog/Templo_Utaki.git
git checkout "Camara&Fisicas"
git push origin "Camara&Fisicas"
```

- Nunca editar `.meta` a mano.
- Nunca borrar archivos desde el Explorador: hacerlo desde el Editor (Unity ajusta `.meta`).
- `.gitignore` debe excluir `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, `*.csproj`, `*.sln`.
- Commits en español imperativo corto. Branch por feature. Review de Bonio antes de merge (§15).

---

## 24. Checklist orden de ejecución (quick reference)

Meta-checklist del camino más corto a MVP. Cada item referencia sección detallada.

- [ ] **S1.1** Dedupe fields en `GrappleScript.cs` (§22.3 Sesión 1).
- [ ] **S1.2** Resolver `image` + `grappleTime` en `PlayerMovement.cs` (§22.3 Sesión 1).
- [ ] **S1.3** Fix `rb` en `CamaraScript.cs` + bloque duplicado (§22.3 Sesión 1).
- [ ] **S1.4** Borrar `PlayerMovement` duplicado del Player en escena (§22.3 Sesión 1).
- [ ] **S1.5** Cerrar decisiones §21.7.1 (joint) y §21.7.4 (input).
- [ ] **S2.1** Refactor visibilidad + `isGrounded` a FixedUpdate (§22.4.1).
- [ ] **S2.2** Mover scripts a subcarpetas (§22.3 Sesión 2).
- [ ] **S2.3** Renombrar layer `Grapple` → `Grappleable`; crear 4 layers nuevas (§23.3).
- [ ] **S2.4** Alinear valores del Inspector del Player al GDD (§22.3 Sesión 2).
- [ ] **S2.5** Remover `jumpCooldown`.
- [ ] **S3.1** Swap a `DistanceJoint2D` si decisión lo confirma.
- [ ] **S3.2** Cooldown 0.3s gancho al fallar.
- [ ] **S3.3** `hookGravity = 18` al hook en vuelo.
- [ ] **S3.4** Recalibrar damping del swing.
- [ ] **S3.5** Resolver feature Rebote.
- [ ] **S3.6** Limpiar fields huérfanos en escena.
- [ ] **S4.1** Crear `LevelManager.cs`.
- [ ] **S4.2** Crear `CrystalPickup.cs`.
- [ ] **S4.3** Canvas HUD + fade image.
- [ ] **S4.4** Refactor `VoidScript.cs`.
- [ ] **S4.5** Prefab `Crystal`.
- [ ] **S4.6** Build de prueba temprano.
- [ ] **S5.1** Renombrar `SampleScene` → `Sandbox_Mecanicas`.
- [ ] **S5.2** Crear `Level_01_JunglaInicial.unity`.
- [ ] **S5.3** Armar beat-map del Nivel 1.
- [ ] **S5.4** `PatrollerAI.cs` + 1 instancia.
- [ ] **S5.5** `FragilePlatform.cs` + 1 instancia.
- [ ] **S5.6** 3 cristales.
- [ ] **S5.7** Canvas `EndOfLevel` + trigger de salida.
- [ ] **S5.8** Scenes in Build.
- [ ] **S6.1** Highlight grappleable.
- [ ] **S6.2** Fade-out tweeneado.
- [ ] **S6.3** PauseMenu con ESC.
- [ ] **S6.4** Feedback visual (destello, color gancho, "!" patrullero).
- [ ] **S6.5** Build PC 1920×1080 final.
- [ ] **DoD** Validar §22.7 entero.
