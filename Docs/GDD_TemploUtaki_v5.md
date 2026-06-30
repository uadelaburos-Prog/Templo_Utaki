**TEMPLO UTAKI**

─────────────────────────────────

**GAME DESIGN DOCUMENT**

*Plataformas 2D con Gancho  —  Pixel Art 8×8  —  Unity 6*

| **Estudio** | **Demonic Arts Company** |
| --- | --- |
| Versión | v5.6 — Junio 2026  ·  Cristales persistentes al morir  ·  Fin de nivel con tiempo/muertes/cristales  ·  Doble contador de muertes  ·  Audio centralizado en AudioManager (música por zona, idempotente)  ·  Panel de controles + ESC en menús  ·  base v5.5 (selector de niveles, sistemas de evento, pinchos en secuencia) |
| Motor | Unity 6000.0.30f1 — URP 2D |
| Plataforma | PC (escalable a consolas) |
| Duración estimada | 20–30 minutos |
| Estado | En desarrollo — Parcial 1 |

Belen Almed  ·  Julieta Cerelli  ·  Fermin Blanco  ·  Eliel Denmon  ·  Santiago Calvo  ·  Bono Dipacce

**  ****ÍNDICE**

| **PARTE I — DISEÑO DEL JUEGO** |
| --- |
| 1. Visión General |
| 2. Personaje y Controles |
| 3. Sistema del Gancho |
| 4. Entorno: Plataformas y Obstáculos |
| 5. Sistema de Enemigos |
| 5.1 Filosofía |
| 5.2 Guerrero Espectral |
| 5.3 Momia del Templo |
| 5.4 Lanzador de Proyectiles |
| 5.5 Sistema de Llaves |
| 5.6 Jefe Final: El Golem |
| 6. Diseño de Niveles |
| 7. Estética y Dirección Visual |
| 8. Audio |
| 9. UI y Sistemas de Juego |
| **PARTE II — ESPECIFICACIONES TÉCNICAS** |
| 10. Stack Tecnológico |
| 11. Arquitectura de Código |
| 12. Equipo de Desarrollo |
| 13. Glosario |
| 14. Registro de Features |
| 15. Parámetros Técnicos |
| 15.1 Movimiento del Jugador |
| 15.2 Sistema del Gancho |
| 15.3 Plataformas |
| 15.4 Enemigos |
| 15.5 Sistema de Daño y Reinicio |
| 15.6 Audio — Prioridades |
| 15.7 Arte — Especificaciones |
| 15.8 Sistema de Llaves |

**PARTE I**

*DISEÑO DEL JUEGO*

**1.  ****VISIÓN GENERAL**

**1.1 Concepto Principal**

Templo Utaki es un juego de plataformas 2D en pixel art donde un explorador se infiltra en un antiguo templo usando un gancho como herramienta principal de movimiento. La propuesta central es la fantasía de balancearse: cargar el gancho, lanzarlo, sentir el peso del cuerpo en el swing y soltar en el momento exacto para salir disparado hacia el siguiente punto. Ese ciclo — lanzar, balancear, soltar, encadenar — es el núcleo de todo lo que el juego pide al jugador.

El templo no es un escenario decorativo: es un sistema de obstáculos, guardianes y secretos que el explorador atraviesa en 20–30 minutos. Los enemigos no se combaten directamente — se evitan o se eliminan usando el propio entorno en su contra. Al final del recorrido, un Golem de Piedra custodia la Cámara del Tesoro. No hay diálogos ni texto en pantalla. El mundo comunica todo por sí solo.

**1.2 Especificaciones**

| **Parámetro** | **Valor** |
| --- | --- |
| Género | Plataformas / Aventura 2D |
| Plataforma | PC (escalable a consolas) |
| Perspectiva | 2D Lateral |
| Resolución objetivo | 1920×1080 (1080p) |
| Cantidad de niveles | 6 niveles |
| Duración total | 20–30 minutos (jugador promedio) |
| Estilo visual | Pixel Art 8×8 |
| Tipos de enemigos | 3 (Guerrero Espectral, Momia del Templo, Lanzador de Proyectiles) + Jefe Final |
| Tipos de plataformas | 5 (estática, móvil, frágil, one-way, tracción) |
| Items recolectables | Cristales de puntuación, Llaves |

**1.3 Pilares de Diseño**

**—  **Movimiento Fluido: el gancho debe sentirse natural y satisfactorio desde el primer lanzamiento. Lanzar, balancear, soltar — cada acción tiene peso y respuesta inmediata.

**—  **Física Verosímil: el péndulo se comporta de forma consistente y predecible, tendiendo a la física real. Hay asistencias de diseño (sistema de carga, snap al anclaje) pero el jugador nunca siente que el juego lo lleva de la mano.

**—  **Combate Ambiental: el jugador no ataca. Usa el entorno — trampas, plataformas, proyectiles enemigos — para eliminar adversarios tácticamente. La evasión siempre es una opción válida.

**—  **Accesibilidad: desafío moderado y progresivo. La muerte es rápida e indolora — reinicio en segundos, no una punición. Los niveles incluyen checkpoints que permiten reanudar desde un punto intermedio.

**—  **Concisión: 20–30 minutos de juego puro, sin relleno ni backtracking. Cada segundo exige al jugador que use el gancho.

**—  **Feedback Esencial: todo elemento visual o sonoro comunica información útil. Lo decorativo no existe.

**—  **Legibilidad: el jugador nunca muere por sorpresa. Todo peligro está señalizado antes de poder impactar.

**1.4 Referentes**

| **Juego** | **Referencia específica** |
| --- | --- |
| Terraria | Gancho con física de péndulo, encadenamiento de movimientos aéreos. |
| Shovel Knight | Pixel art 2D, progresión por biomas con paletas diferenciadas. |
| Celeste | Muerte instantánea sin penalización, reinicio en segundos, foco en movimiento. |
| Neon White | Movimiento encadenado como habilidad central, fluidez como objetivo. |

**1.5 Contexto Narrativo**

Un audaz aventurero se adentra en un peligroso templo ubicado en una densa jungla. Su objetivo es la riqueza eterna, para lograrlo debe atravesar todas las adversidades del lugar, sorteando obstáculos y enemigos.

**2.  ****PERSONAJE Y CONTROLES**

**2.1 El Explorador**

El personaje jugable es un explorador aventurero clásico. Ágil, competente con el gancho. Su identidad visual es responsabilidad del equipo de arte: silueta clara y reconocible incluso a 8×8 px, equipamiento de exploración coherente con el pixel art selvático del juego.

| *[ INSERTAR IMAGEN ]  Concept art del Explorador — sprite idle + spritesheet de animaciones* |
| --- |

**2.2 Movimiento**

El explorador corre, salta y tiene control mínimo en el aire. La física de movimiento prioriza la inercia: hay aceleración al arrancar y desaceleración al soltar. Los valores exactos de velocidad, aceleración, gravedad y fricción están en (*ver Sección 15.1*)

**2.3 Mapeo de Controles**

| **Acción** | **PC — Teclado / Ratón** |
| --- | --- |
| Mover izquierda | A / ← |
| Mover derecha | D / → |
| Saltar | ESPACIO |
| Apuntar gancho / llave | Movimiento del ratón |
| Cargar / Lanzar gancho | Mantener + Soltar Click Izquierdo |
| Lanzar llave (si se sostiene) | Mantener + Soltar Click Izquierdo |
| Soltar gancho (enganchado) | Soltar Click Izquierdo |
| Acortar soga (enganchado) | W |
| Alargar soga / retraer objeto hookeable | S |
| Pausa | ESC / P |
| Reiniciar nivel | R |

**3.  ****SISTEMA DEL GANCHO**

**3.1 Descripción General**

El gancho es la mecánica central del juego. No es solo un medio de transporte — es la forma en que el explorador se relaciona con cada elemento del nivel. Se usa para cruzar abismos, ganar altura, activar mecanismos, mover objetos, traer llaves y encadenar movimientos en el aire.

El lanzamiento funciona por carga: mantener Click izquierdo acumula fuerza. Cuanto más tiempo se sostiene, mayor el alcance y la velocidad inicial. Soltar dispara. Una vez enganchado a una superficie válida, el jugador oscila bajo la gravedad como un péndulo — sin asistencia de fuerza adicional. El dominio del gancho está en saber cuándo cargar, cuánto, a qué ángulo lanzar y en qué momento exacto soltar para aprovechar el impulso acumulado.

Cuando el gancho impacta un objeto Hookeable en lugar de una superficie Grappleable, no se produce enganche: el gancho queda tenso contra el objeto y presionar S lo retrae hacia el jugador, o bien lo jala si el objeto es movible.

**3.2 Ciclo de Estados del Gancho**

El sistema opera como una máquina de estados con 7 estados:

*Figura 1 — Ciclo de vida completo del gancho*

| **Estado** | **Transición** | **Qué hace** |
| --- | --- | --- |
| REPOSO | Click sostenido → CARGANDO | Gancho inactivo, listo para cargar. |
| CARGANDO | Soltar → LANZAMIENTO | Mantener Click acumula fuerza. Ícono junto al personaje se llena progresivamente. |
| LANZAMIENTO | Hit Grappleable → ENGANCHADO / Hit Hookeable → ENGANCHE HOOKEABLE / Inválido → RETRAYENDO / Sin alcance → RETRAYENDO | Gancho vuela con fuerza y alcance proporcionales a la carga. Gravedad actúa: describe arco. |
| ENGANCHADO | → BALANCEANDO | Gancho fijo en superficie Grappleable. Jugador cuelga y puede oscilar. |
| ENGANCHE HOOKEABLE | S → retrae objeto / Soltar → RETRAYENDO | Gancho tenso contra objeto Hookeable. No hay cuelgue. Presionar S retrae el objeto hacia el jugador (llave) o jala el objeto (pared reactiva, plataforma de tracción). Soltar suelta el objeto. |
| BALANCEANDO | Soltar → RETRAYENDO | Jugador oscila como péndulo bajo gravedad. W acorta la soga, S la alarga. |
| RETRAYENDO | → REPOSO (cooldown 0.3s) | La cuerda regresa al jugador con animación completa. Ocurre al soltar, al impactar superficie inválida o por alcance insuficiente. |

**3.3 Sistema de Carga**

El jugador presiona y mantiene Click izquierdo para cargar el gancho. Un ícono junto al personaje se llena progresivamente. Al soltar, el gancho se lanza con la fuerza y el alcance correspondientes al tiempo cargado.

Los valores exactos de tiempos de carga, alcances mínimo y máximo, radio de snap y cooldown están en (*ver Sección 15.2*)

**Ícono de carga**

**—  **Aparece junto al personaje mientras se mantiene Click izquierdo.

**—  **Se llena progresivamente representando la potencia acumulada.

**—  **Placeholder actual: barra de progreso. Arte define el ícono definitivo.

**—  **Desaparece al soltar el botón.

**⚠  ***Ficha de Arte — Ícono de carga: el diseño definitivo reemplaza la barra placeholder. Debe ser legible a 8×8 px y coherente con la estética pixel art del personaje.*

**3.4 Lanzamiento**

**—  **El jugador apunta con el ratón y mantiene Click izquierdo para cargar.

**—  **Al soltar, el gancho parte en dirección al cursor con la fuerza y el alcance acumulados.

**—  **La gravedad actúa sobre el gancho: describe un arco, no va en línea recta.

**—  **Snap automático: si hay una superficie grappleable en el radio de snap (proporcional a la carga), el gancho se engancha al punto válido más cercano.

**—  **Si el gancho impacta una superficie no grappleable: regresa al jugador con la animación de retracción completa.

**—  **Si el gancho no alcanza ningún anclaje: se retrae. Cooldown 0.3s.

**3.5 Balanceo — Física de Péndulo**

**—  **Una vez enganchado, el jugador oscila bajo el punto de anclaje por gravedad pura. No se aplica ninguna fuerza adicional.

**—  **Para ganar velocidad, el jugador debe engancharse con impulso horizontal previo y soltar en el momento correcto.

**—  **Al soltar, la velocidad acumulada en el swing se conserva completamente.

**Ajuste de longitud de soga**

Mientras el jugador está enganchado puede modificar la longitud de la cuerda en tiempo real. Presionar W acorta la soga, atrayendo al jugador hacia el punto de anclaje. Presionar S la alarga, alejándolo. Esto afecta directamente la física del péndulo: una soga más corta produce oscilaciones más rápidas, una más larga produce oscilaciones más lentas y de mayor amplitud.

**—  **Acortar la soga permite ganar altura sin soltar el gancho.

**—  **Alargar la soga en el punto alto del swing genera más inercia hacia abajo.

**—  **La misma tecla S que alarga la soga sirve para retraer objetos grappleados hacia el jugador cuando el gancho está enganchado en un objeto interactuable (llave, objeto reactivo).

Velocidad de ajuste de soga y longitudes mínima/máxima están en (*ver Sección 15.2*)

**3.6 Comportamiento ante Superficies Inválidas**

Cuando el gancho impacta una superficie que no pertenece ni a la capa Grappleable ni a la capa Hookeable, regresa al jugador ejecutando la animación de retracción completa. Resulta en RETRAYENDO → REPOSO con cooldown de 0.3s.

**3.7 Visualización de la Cuerda**

**—  **Durante el vuelo: la cuerda sale del jugador y sigue la posición del gancho en tiempo real.

**—  **Una vez enganchada: curva Bezier cuadrática, cuelga naturalmente entre jugador y anclaje.

**—  **Al retraerse: la cuerda vuelve hacia el jugador de forma continua.

**3.8 Superficies Grappleables y Objetos Hookeables**

El gancho interactúa con dos categorías distintas de objetos del nivel, diferenciadas por capa de Unity:

**Capa Grappleable — para anclaje y movimiento**

**—  **Solo superficies en la capa Grappleable pueden servir de punto de anclaje para balancearse.

**—  **Grappleables típicos: vigas de metal, paredes de roca sólida, cadenas, techo de piedra.

**—  **No grappleables: madera podrida, vidrio, vegetación, suelo de tierra.

**—  **Los puntos de anclaje válidos se señalizan visualmente en el diseño de nivel. El jugador nunca debe adivinar qué puede agarrar.

**Capa Hookeable — para interacción y manipulación**

**—  **Los objetos Hookeables son aquellos con los que el gancho interactúa para moverlos, tirarlos o traerlos. El jugador no se cuelga de ellos.

**—  **Hookeables típicos: paredes reactivas (se tiran), vigas destructibles (se rompen), plataformas de tracción (se acercan), llaves (se retrae con S).

**—  **Las llaves pertenecen a la capa Hookeable y además tienen el tag 'Key' para que los scripts las identifiquen.

**—  **Un objeto puede ser Hookeable sin ser Grappleable, y viceversa. La distinción es intencional.

| *Regla de diseño: si un jugador no puede saber a primera vista si puede engancharse a una superficie o interactuar con ella, el nivel está mal diseñado. La legibilidad visual de anclajes e interacciones es responsabilidad de Arte y Game Design.* |
| --- |

**4.  ****ENTORNO: PLATAFORMAS Y OBSTÁCULOS**

**4.1 Tipos de Plataformas**

**Plataforma Estática**

**—  **Sólida, inmóvil, soporta peso indefinidamente.

**—  **Fricción: control adecuado sin resbalar.

**—  **El gancho puede engancharse en bordes o puntos marcados.

**—  **Feedback: sonido de pasos al caminar; partícula de polvo al aterrizar.

**Plataforma Móvil**

**—  **Se mueve en trayectoria predefinida: lineal, circular o pendular.

**—  **El jugador mantiene la velocidad relativa de la plataforma al estar sobre ella.

**—  **El gancho puede engancharse mientras se mueve.

**—  **Feedback: sonido mecánico continuo.

**Plataforma Frágil**

**—  **Timer de rotura: 1–2 segundos desde que el jugador la pisa.

**—  **Advertencia visual progresiva: cambio de color a rojo + grietas en 3 fases.

**—  **Al romperse desaparece completamente. Regenera en 5 segundos.

**—  **El gancho puede engancharse en ella, pero el timer sigue corriendo.

**—  **Feedback: crujido progresivo al pisar; colapso al romperse.

**Plataforma One-Way**

**—  **Se puede atravesar desde abajo sin colisionar.

**—  **Soporta peso desde arriba con normalidad.

**—  **El gancho puede engancharse desde arriba. No desde abajo.

**Plataforma de Tracción**

**—  **Se desplaza hacia el jugador mientras el gancho está enganchado y la cuerda en tensión.

**—  **Al soltar el gancho, se detiene o retorna según configuración del nivel.

**—  **Feedback: sonido mecánico de deslizamiento.

**4.2 Obstáculos Ambientales**

| **Obstáculo** | **Comportamiento** | **Señalización** | **Daño** |
| --- | --- | --- | --- |
| Pinchos | Zona fija. Sin movimiento. SpikeMode.Estatico — siempre activos, cualquier contacto causa reinicio independientemente del estado interno. | Rojo intenso, estático. | Contacto = reinicio. |
| Fuego | Zona de área. Estático. | Animación de llama + partículas. | Contacto = reinicio. |
| Foso de Vacío | Caída fuera del nivel. Muerte por vacío: reinicio inmediato sin animación dramática. | Oscuridad visible. Sin borde marcado. | Caída = reinicio. |
| Llamarada Retráctil | Ciclo activo/inactivo configurable. Dispara una columna de fuego hacia arriba, más alta que los pinchos retráctiles. Comportamiento idéntico a los pinchos retráctiles en lógica de ciclo. | Brillo anaranjado en el suelo antes de activarse + sonido de ignición como advertencia. | Contacto activo = reinicio. Mata al Espectral. El único hazard cíclico que lo elimina. |
| Pinchos Retráctiles | Ciclo fijo configurable (ej: 1.5s activo / 1.5s retráctil). SpikeMode.Retractil — solo activos en estado Desplegado. Dirección configurable: Arriba / Abajo / Izquierda / Derecha. Grupos (SpikeGroup) soportan tres modos de ciclo: Sincronizado (todos juntos), Desfasado (distribuido automáticamente) y Secuencial (activación en orden con delay entre spikes). **Secuencia manual:** cada pincho individual puede coordinarse a mano desde su propio Inspector combinando `faseInicial` (punto del ciclo donde arranca, 0–1) y `delayInicial` (espera oculto antes del primer ciclo) — ambos parámetros se combinan, permitiendo armar cascadas/ondas colocando pinchos sueltos sin necesidad de un SpikeGroup. | Animación de salida + color rojo. Ciclo constante y predecible. Flecha direccional en Gizmos (Editor). | Contacto en estado Desplegado = reinicio. |

| *Regla universal: todos los obstáculos deben ser visibles antes de que el jugador pueda impactarlos. Ningún obstáculo mata por sorpresa en la primera pasada de un nivel correctamente diseñado.* |
| --- |

**Llamarada Retráctil — Detalle de diseño**

La Llamarada Retráctil es el hazard más versátil del juego por una razón: es el único hazard cíclico capaz de eliminar al Guerrero Espectral. Esto abre situaciones de diseño únicas donde el jugador puede atraer al Espectral hacia una Llamarada durante su órbita o su dash, o posicionarse al otro lado de ella cuando el Espectral inicia el Windup.

**—  **Física del fuego: la columna sube verticalmente desde el suelo. No tiene componente horizontal.

**—  **Altura: significativamente mayor que los pinchos retráctiles — el jugador no puede saltarla en el momento activo.

**—  **Ciclo: igual al sistema de pinchos retráctiles — mismos modos de grupo (Sincronizado, Desfasado, Secuencial) y configuración de timers desde el Inspector.

**—  **Señalización: el brillo previo en el suelo da al jugador una ventana de lectura antes del disparo. El ciclo es siempre predecible.

**—  **Interacción con el Espectral: al cruzar la columna activa, el Espectral muere instantáneamente — misma lógica que la Zona de Fuego estática, pero en forma cíclica y colocable con precisión en niveles de plataformas.

| *ℹ  Diferencia clave: la Zona de Fuego es una barrera ambiental permanente. La Llamarada Retráctil es una trampa de timing — hay ventanas seguras para cruzar. Esta diferencia es fundamental para diseñar secciones donde el jugador deba cruzar al mismo tiempo que atrae al Espectral.* |
| --- |

Parámetros técnicos (ciclo, altura, script) en (*ver Sección 15.3*).

**4.3 Trampas Ambientales con Activador**

Las trampas ambientales no están activas permanentemente: requieren un activador para dispararse. Un activador dispara exactamente un efecto. La relación es 1 a 1.

**Placa de Presión**

**—  **Activador en el suelo. Se activa al pisarla.

**—  **Dispara la trampa ambiental vinculada una única vez por pisada.

**—  **Señalización: sprite diferenciado del suelo normal. Visible antes de pisarla.

**—  **Feedback: clic mecánico al activarse.

**Roca Cayente**

**—  **Trampa activada por placa de presión. Al pisarse, una roca cae desde el techo en trayectoria vertical fija, luego se desplaza horizontalmente hasta impactar en pared.

**—  **El jugador debe esquivarla usando el gancho o el salto.

**—  **Contacto con la roca = reinicio inmediato. La roca se destruye al impactar con una pared. No rebota.

**—  **Señalización: sombra proyectada en el suelo antes de caer + sonido de crujido estructural como advertencia.

| *Principio de expansión: el sistema de activadores es extensible. Nuevas trampas pueden agregarse usando el mismo esquema: activador → efecto.* |
| --- |

**4.4 Puntos de Anclaje del Gancho**

El punto de anclaje no es un objeto específico: es una propiedad asignable a cualquier objeto del nivel. Arte y Game Design deciden dónde aplica en cada nivel.

**Anclaje Fijo**

**—  **Estático. La cuerda mantiene distancia constante.

**—  **Asignable a: paredes, techos, vigas, columnas.

**Anclaje Móvil**

**—  **El punto de anclaje se mueve. El jugador es arrastrado con él mientras la cuerda está activa.

**—  **Asignable a: plataformas móviles, objetos en movimiento.

**Anclaje Destructible (BreakableAnchor)**

**—  **Se activa al engancharse: el gancho dispara la secuencia de rotura inmediatamente al conectar.

**—  **Tres fases de advertencia visual (igual que la Plataforma Frágil) antes del colapso final, con la cuerda retrayéndose automáticamente al romperse.

**—  **Modo configurable desde el Inspector: `permanentBreak = true` — el objeto se destruye permanentemente al romperse; `permanentBreak = false` — regenera tras un `regenDelay` configurable.

**—  **Si el jugador se engancha mientras ya está en proceso de rotura, el gancho se registra pero no reinicia la corrutina.

**—  **Señalización por apariencia del material: grietas visibles o textura desgastada indican que el anclaje es destructible.

**Anclaje Reactivo**

**—  **Al engancharse y tirar, activa un efecto en el objeto.

**—  **Asignable a: paredes reactivas, objetos que el gancho puede mover. Ver sección 4.5.

**4.5 Objetos Interactuables**

**Activados por Proximidad o Contacto**

**—  **Puerta de Inicio: se abre automáticamente al comenzar el nivel. Solo animación estética.

**—  **Puerta de Salida de Nivel: se abre al llegar el jugador al final del recorrido. Trigger de fin de nivel. No requiere llave ni condición previa — su apertura indica que el nivel está completo.

**—  **Puerta Intermedia de Llave: bloquea el paso en un punto interior del nivel. Solo se abre acercándose con la llave vinculada. Ver Sección 5.5 para el sistema completo de llaves.

**—  **Palanca / Switch: contacto del jugador alterna estado ON/OFF de un objeto vinculado (plataforma, pinchos, etc.).

**Activados Exclusivamente por el Gancho (capa Hookeable)**

**—  **Pared Reactiva: al enganchar una argolla en una pared débil y tirar, la pared cae con animación de rotación abriendo un pasaje. Al terminar la caída, la pared queda como superficie sólida permanente sobre la que el jugador puede caminar — funciona como puente. El resultado (pasaje + plataforma) es predecible visualmente antes del intento.

**—  **Plataforma de Tracción: al enganchar y mantener tensión, la plataforma se desplaza hacia el jugador.

**—  **Viga Destructible: el gancho rompe la viga en 1–2 usos. Puede colapsar estructuras, abrir caídas o generar movimiento.

**—  **Llave: al enganchar la llave y presionar S, se retrae hacia el jugador. Ver Sección 5.5.

| *Principio de diseño: el jugador debe poder anticipar la consecuencia de un tiro de gancho antes de ejecutarlo. Si el objeto es de madera y tiene una argolla visible, el resultado es predecible. El juego nunca sorprende negativamente con una interacción de gancho.* |
| --- |

**4.6 Sistemas de Evento y Escenas Scriptadas**

Algunos momentos del juego — en particular la escena estilo "Indiana Jones" del Nivel 5 — encadenan varias consecuencias a partir de un único disparador. El sistema está diseñado por composición con `UnityEvent`: un objeto recolectable dispara el evento y cada consecuencia se cablea desde el Inspector, sin un manager central. Los efectos exponen métodos públicos sin parámetros para colgarse directamente del evento.

**Reliquia Disparadora (RelicaPickup)**

**—  **Ídolo/reliquia recolectable por proximidad (OverlapCircle contra la capa del jugador), con flotación visual. Misma lógica de recolección que un cristal.

**—  **Al recogerse dispara **una sola vez** un `UnityEvent` desde el que se cablea toda la consecuencia (activar/desactivar objetos, lanzar la roca, cambiar el skin del jugador). Por diseño actual, todos los efectos se disparan al instante.

**Objeto Activable (ObjetoActivable)**

**—  **Pared, terreno o trampa genérica que se enciende o apaga por evento (desde la reliquia, una palanca, una placa, etc.). Habilita/deshabilita los Collider2D y Renderer del objeto y sus hijos.

**—  **Estado inicial configurable (`activoInicial`). Opción de animación de aparición deslizándose desde un offset (terreno que "emerge"). API: `Activar()`, `Desactivar()`, `Alternar()`.

**Roca Rodante (RollingBoulder)**

**—  **Roca redonda estilo Indiana Jones. Permanece dormida (sin simular física) hasta `Activar()`: cae por gravedad y rueda horizontalmente en la dirección configurada hacia la ruta de escape del jugador, girando acorde a su velocidad.

**—  **Contacto con el jugador = reinicio. Aplasta también a la Momia (friendly fire ambiental), coherente con el resto de las trampas.

**—  **Se distingue de la **Roca Cayente** (Sección 4.3): la Cayente cae vertical y luego se desliza hasta una pared; la Rodante es una persecución horizontal continua activada por evento.

**Cambio de Skin del Jugador (PlayerSkinSwapper)**

**—  **Cambia "todos los sprites" del jugador a una variante intercambiando el `RuntimeAnimatorController` del Animator (se recomienda un Animator Override Controller, que reusa estados/transiciones y solo sustituye los clips). Las animaciones se conservan intactas. Opción de tinte del SpriteRenderer.

**—  **API: `CambiarVariante()`, `RestaurarOriginal()`, `Alternar()`. Pensado para reflejar visualmente un evento narrativo (ej. recoger la reliquia).

| *Nota de diseño: el sistema reemplaza al antiguo `SecuenciaEventos` (orquestador temporizado), retirado en favor de disparar las consecuencias directamente desde el recolectable. Si en el futuro se necesitan retardos entre efectos, se reintroducirá temporización sin volver a un manager central.* |
| --- |

**5.  ****SISTEMA DE ENEMIGOS**

**5.1 Filosofía de Diseño**

Los enemigos de Templo Utaki son predecibles por diseño. El jugador puede aprender sus patrones y evitarlos con habilidad. No hay comportamientos aleatorios ni imprevisibles. El foco está en el movimiento del jugador, no en el combate directo.

El jugador no tiene ataque. Sin embargo, puede eliminar enemigos de forma táctica usando el entorno: trampas, obstáculos y la física del nivel actúan como herramientas de combate indirecto. La eliminación ambiental es opcional — siempre existe una ruta de evasión — pero recompensa la lectura del espacio y la planificación.

Sistema de daño unificado: cualquier contacto con un enemigo, proyectil o trampa reinicia el nivel. La muerte por trampa activa una secuencia dramática breve (SpotlightOverlay + pausa + animación Death en UnscaledTime) antes del reinicio. La muerte por caída al vacío es inmediata. Si hay un checkpoint activo, el jugador reaparece desde ese punto.

**5.2 Enemigo Tipo 1A: El Guerrero Espectral**

**Descripción**

Espíritu guerrero de un antiguo guardián del templo. Semitransparente, con tocado de plumas fantasmales y pintura de guerra que emite un brillo tenue. Su naturaleza espectral le permite atravesar casi todas las superficies del nivel — paredes, suelo, plataformas. Las únicas superficies que lo detienen son las paredes con ornamentación dorada o rúnica.

**Comportamiento general**

El Espectral no persigue al jugador en línea recta. En lugar de eso, merodea a su alrededor con un movimiento orgánico: combina una componente tangencial (girar en torno al jugador, en sentido elegido al azar en cada encuentro) con un resorte radial suave que cierra distancia de a poco hasta una distancia mínima "respirante" (oscila sinusoidalmente). No describe un círculo rígido — se ve natural y evita pegarse al jugador. Cuando el jugador permanece quieto durante un intervalo sostenido **y** el Espectral ya está suficientemente cerca, telegrafía un dash con un parpadeo blanco breve y se lanza en línea recta hacia la posición del jugador. Si impacta una pared rúnica (capa SpectralWall) durante el dash, se detiene inmediatamente.

**Máquina de Estados**

*Figura 2 — Máquina de estados del Guerrero Espectral*

| **Estado** | **Cuándo activa** | **Qué hace el Espectral** |
| --- | --- | --- |
| PATRULLA | Estado inicial / tras perder al jugador | Recorre su ruta entre dos puntos predefinidos. Si detecta al jugador, entra en Orbita. |
| IDLE | Al llegar al extremo de su ruta | Pausa breve antes de invertir dirección. |
| ORBITA | Jugador detectado | Merodeo orgánico alrededor del jugador (tangencial + resorte radial), sin atacar. Espera que el jugador se detenga y estar a tiro (≤ dashTriggerRange). |
| WINDUP | Jugador quieto un tiempo sostenido y dentro de dashTriggerRange | Telegraph de ataque (0.35s): retrocede levemente y parpadea en blanco. Señal visible para el jugador. |
| DASH | Tras completar el Windup | Se lanza en línea recta a alta velocidad hacia la posición del jugador, con overshoot. Un rastro visual (AfterimageTrail) marca su trayectoria. |
| RECOVER | Tras completar el Dash o impactar una pared rúnica | Pausa breve (0.4s) + cooldown antes de volver a orbitar o retirarse. |
| REGRESO | Jugador fuera de rango | Vuelve a su punto de patrulla. |
| MUERTO | Contacto con fuego | Desintegración inmediata. Suelta la llave si la portaba. |

| *ℹ  El objetivo del Windup (parpadeo blanco) es dar al jugador una ventana clara para moverse. Quien se mueva constantemente nunca verá un Dash. El desafío está en gestionar el movimiento mientras navega el nivel.* |
| --- |

**Traversal**

El Espectral flota — no le afecta la gravedad y atraviesa toda la geometría del nivel. Solo las paredes con ornamentación dorada o rúnica (capa SpectralWall) lo detienen. El jugador puede identificarlas a primera vista.

**Eliminación Ambiental**

**—  **El contacto con cualquier zona de fuego (estática o Llamarada Retráctil) mata al Espectral instantáneamente. Es el único método de eliminación.

**—  **El jugador puede posicionarse al otro lado de una zona de fuego y dejar que el Espectral cruce durante el dash o la órbita.

**—  **Las paredes rúnicas pueden usarse para interrumpir el dash y reposicionarse.

**—  **Si portaba una llave, esta cae al suelo con física en el punto de muerte.

Parámetros técnicos completos (velocidades, radios, timers) en (*ver Sección 15.4*).

**Ficha de Arte**

| **Elemento** | **Descripción** |
| --- | --- |
| Tamaño sprite | 16×24 px. Silueta de guerrero con tocado ornamentado. |
| Paleta | Azul espectral semitransparente, detalles de plumas y pintura de guerra brillantes. |
| Efectos visuales | Semitransparencia. Rastro de copias fantasma durante el dash. Parpadeo blanco en Windup. |
| Animación: patrulla / órbita | 6–8 frames. Flotación sutil y desplazamiento etéreo. Loop. |
| Animación: windup | 2–3 frames. Retroceso con parpadeo blanco rítmico. |
| Animación: dash | 4–6 frames. Alta velocidad con trail de copias. |
| Animación: muerte | 6–8 frames. Desintegración en partículas de fuego. |
| Ícono de alerta | «!» amarillo sobre la cabeza. 8×8 px. Visible al detectar al jugador. |

| *[ INSERTAR IMAGEN ]  Concept art + spritesheet del Guerrero Espectral* |
| --- |

**5.3 Enemigo Tipo 1B: La Momia del Templo**

**Descripción**

Guardián sacrificial reanimado. Sacerdote o guerrero envuelto en vendas con ornamentos de piedra y oro roto. Totalmente físico — obedece las mismas leyes de movimiento que el jugador: no atraviesa paredes, no puede cruzar abismos, cae si no hay plataforma. Camina de forma lenta y pesada dejando polvo a su paso.

Por su naturaleza física, la Momia es vulnerable a todas las trampas ambientales del nivel. El jugador puede usarla como objetivo de cualquier elemento del entorno. Esto la convierte en el enemigo más tácticamente rico del juego.

**Comportamiento — Traversal**

**—  **Es totalmente física (Rigidbody2D Dynamic, gravedad). Tiene las mismas limitaciones de movimiento que el jugador: no atraviesa paredes, no cae de plataformas voluntariamente durante la patrulla.

**—  **Patrulla por rebote — sin waypoints: camina en su dirección inicial hasta detectar (con raycasts) un borde de plataforma o una pared insuperable, y entonces gira. No usa puntos A/B predefinidos.

**—  **Salta paredes superables: si detecta una pared adelante pero está despejada por encima de `jumpClearanceHeight`, salta para superarla en lugar de girar. Durante la persecución también salta si el jugador está bastante más arriba (`alturaSaltoPersecucion`), aunque no haya pared.

**—  **Detección con línea de visión: solo entra en persecución si el jugador está dentro del radio de detección **y** hay línea de visión despejada — un Linecast contra paredes/suelo (`maskVisionBloqueo`) bloquea la detección. No detecta al jugador a través de muros ni del piso.

**—  **En persecución es "más tonta": no respeta los bordes y puede caer al vacío persiguiendo al jugador. No empuja contra paredes que no puede superar (evita el wall-stick), pero sigue saltando para intentar alcanzarlo.

**—  **Puede caer al vacío si el entorno bajo sus pies desaparece: si patrulla sobre una plataforma frágil y el jugador la activa pisándola primero, la plataforma cede y la Momia cae con ella. Igual con una plataforma de tracción arrastrada con el gancho fuera de su posición.

**—  **Muere al contacto con: fuego, pinchos (estáticos o retráctiles en estado activo), roca cayente, caída al vacío.

**—  **No muere por plataformas frágiles — su peso las rompe pero el colapso no la daña, solo la expone al vacío debajo.

**Máquina de Estados**

*Figura 3 — Máquina de estados de la Momia del Templo*

La Momia (`MummyAI.cs`) usa cuatro estados: Idle, Patrulla, Persecución y Muerto. No tiene estados Alerta ni Regreso separados — la persecución arranca directamente al detectar (con línea de visión) y termina por distancia.

| **Estado** | **Transición** | **Qué hace** |
| --- | --- | --- |
| IDLE | Pausa de giro (duracionIdle 0.4s) → PATRULLA · Detecta jugador → PERSECUCIÓN | Detenido brevemente al girar en un borde o pared. Polvo cayendo. |
| PATRULLA | Borde/pared insuperable → gira (IDLE) · Detecta jugador (radio + línea de visión) → PERSECUCIÓN | Se mueve a 2 u/s por rebote. Respeta bordes y paredes. Salta paredes superables. Muestra «!» al detectar. |
| PERSECUCIÓN | Jugador supera radioAbandonar (7u) → PATRULLA | Se mueve a 3.5 u/s hacia el jugador. No respeta bordes (puede caer). Salta paredes superables y salta si el jugador está más arriba. |
| MUERTO | Contacto con trampa ambiental | Animación de colapso (Dead). Suelta la llave si la portaba. Collider y físicas desactivados de inmediato. |

**Eliminación Ambiental**

La Momia puede eliminarse con cualquier trampa del nivel. El jugador debe leer su ruta de patrulla y usar el entorno estratégicamente. La eliminación ambiental es táctica — requiere preparación y timing — pero nunca es el único camino.

**—  **Fuego: atraerla hacia una zona de fuego durante la persecución.

**—  **Pinchos estáticos: posicionarse detrás de pinchos y dejar que la Momia camine hacia ellos.

**—  **Pinchos retráctiles: timing preciso — activar la persecución cuando los pinchos están en ciclo de salida.

**—  **Roca cayente: pisar la placa de presión con la Momia bajo la trayectoria de caída.

**—  **Plataforma frágil: hacer que la Momia patrulle sobre una plataforma frágil y pisarla primero para activar el timer. La plataforma colapsa bajo sus pies cuando el timer agota.

**—  **Plataforma de tracción: arrastrar con el gancho la plataforma sobre la que patrulla la Momia, dejándola sin suelo sobre un abismo.

**—  **Proyectil del Lanzador: llevar a la Momia a la línea de fuego del Lanzador de Proyectiles. Friendly fire ambiental.

**—  **Pared reactiva: tirar una pared con el gancho mientras la Momia está del otro lado. La pared al caer la aplasta.

**Ficha de Arte**

| **Elemento** | **Descripción** |
| --- | --- |
| Tamaño sprite | 16×24 px. Silueta envuelta en vendas, más ancha que el Espectral. |
| Paleta | Beige envejecido, vendas ocres, ornamentos dorados rotos. Ojos que brillan en ámbar. |
| Animación: idle | 2–4 frames. Respiración pesada, polvo cayendo. Loop. |
| Animación: walk | 6–8 frames. Paso lento y arrastrado. Vendas que ondean. |
| Animación: alerta | 2 frames. Pausa brusca. Ojos brillan más. |
| Animación: chase | 6–8 frames. Igual que walk, más acelerada. |
| Animación: muerte | 8–10 frames. Colapso de vendas y polvo. Ornamentos caen. |
| Ícono de alerta | «!» amarillo sobre la cabeza. 8×8 px. |

| *[ INSERTAR IMAGEN ]  Concept art + spritesheet de la Momia del Templo* |
| --- |

**5.4 Enemigo Tipo 2: El Lanzador de Proyectiles**

**Descripción**

Serpiente maligna integrada en la arquitectura del templo. No se mueve del lugar. Dispara proyectiles en una dirección fija predefinida con cadencia constante. El jugador aprende el timing y lo esquiva usando el gancho o el salto.

**Máquina de Estados**

*Figura 4 — Máquina de estados del Lanzador de Proyectiles*

**Comportamiento por Estado**

**—  **INACTIVO: estado inicial. Sin movimiento ni disparo. Espera el primer tick del timer.

**—  **CARGANDO: el timer interno cuenta el intervalo configurado. Animación de carga opcional.

**—  **DISPARANDO: lanza el proyectil en la dirección configurada. Dura 1 frame. Regresa a CARGANDO.

**—  **PROYECTIL EN VUELO: entidad independiente. Se destruye al impactar al jugador, una superficie, o al agotar su tiempo de vida.

Los valores exactos de cadencia, velocidad, alcance y vida del proyectil están en (*ver Sección 15.4*)

**Uso Ambiental**

Los proyectiles del Lanzador afectan a los Patrulleros físicos (Momia). Si el jugador logra que un proyectil impacte a una Momia en persecución, esta muere. Esto habilita una táctica avanzada: usar al Lanzador como aliado involuntario para eliminar a la Momia.

**Ficha de Arte**

| **Elemento** | **Descripción** |
| --- | --- |
| Tamaño sprite | 16×16 px. Mecanismo integrado en la arquitectura del templo. |
| Paleta | Verdes llamativos y rojos. |
| Animación: carga | 4–6 frames. Brillo creciente. |
| Animación: disparo | 2 frames. Flash de disparo. |
| Sprite: proyectil | 8×8 px. Orbe o rayo, visualmente distinguible del fondo. |
| Animación: proyectil | 2–4 frames de loop. Rotación o pulso. |

| *[ INSERTAR IMAGEN ]  Concept art + spritesheet del Lanzador y su proyectil* |
| --- |

**5.5 Sistema de Llaves**

**Descripción General**

Las llaves son objetos físicos del mundo que desbloquean puertas específicas del nivel. Cada llave está vinculada a exactamente una puerta, configurada desde el Inspector de Unity. Tener una llave en la mano restringe el uso del gancho — el jugador debe decidir cuándo portarla y cuándo depositarla estratégicamente para recuperar movilidad.

**ℹ  ***Principio de diseño: la llave nunca debe sentirse como una penalización, sino como un puzle de movimiento. El jugador siempre tiene herramientas (lanzarla, dejarla, recuperarla con el gancho) para resolver la situación.*

**Obtención**

**—  **Llaves en el mundo: colocadas en el nivel por Game Design. El jugador las recoge pasando sobre ellas o enganchándolas con el gancho y retrayéndolas con S.

**—  **Llaves dentro de enemigos: portadas por un Guerrero Espectral o una Momia. La única forma de obtenerlas es eliminar al enemigo mediante trampas ambientales. Al morir, la llave cae al suelo con física.

**—  **Si la llave cae al vacío (void), reaparece en su posición original del nivel.

**Comportamiento con la Llave en mano**

**—  **El jugador corre y salta con normalidad.

**—  **El gancho no está disponible mientras se sostiene la llave.

**—  **El jugador puede lanzar la llave usando el mismo esquema del gancho: Click izquierdo con barra de carga + cursor para dirección.

**—  **El jugador puede acercarse a la puerta vinculada sosteniendo la llave para abrirla. No necesita lanzarla a la cerradura.

**Lanzamiento de la Llave**

El lanzamiento de la llave sigue exactamente el mismo sistema de carga que el gancho: mantener Click izquierdo acumula fuerza, soltar dispara en dirección al cursor. La llave tiene física al vuelo y al impactar superficies.

**—  **Lanzar con carga mínima (tap rápido de Click) deposita la llave en el suelo junto al jugador. Es la forma de soltarla voluntariamente sin gancho.

**—  **Uso principal: lanzar la llave al otro lado de un abismo o zona inaccesible sin gancho, luego cruzar usando el gancho, y finalmente recoger la llave del suelo o retraerla con el gancho.

**—  **Si la llave cae al vacío, reaparece en su posición original del nivel.

**—  **La llave puede ser recogida pasando sobre ella, o enganchándola con el gancho (capa Hookeable) y presionando S para retraerla.

**Puertas de Llave**

**—  **Las puertas de llave tienen una marca visual clara que indica que requieren una llave específica.

**—  **La relación llave-puerta es 1 a 1, configurable desde el Inspector.

**—  **Al acercarse sosteniendo la llave vinculada, la puerta se abre automáticamente. La apertura tiene una animación de 5 frames: el sprite sube hacia arriba progresivamente hasta desaparecer. El collider se desactiva de inmediato al comenzar la animación.

**—  **Una puerta abierta permanece abierta durante el resto de la sesión del nivel.

**—  **Al reiniciar el nivel, las puertas vuelven a cerrarse y las llaves reaparecen en sus posiciones originales.

**Ficha de Arte — Llave**

| **Elemento** | **Descripción** |
| --- | --- |
| Tamaño sprite | 8×8 px. Forma de llave ornamentada, coherente con la estética del templo. |
| Paleta | Dorado brillante. Distinguible del fondo en todos los biomas. |
| Animación: idle (en suelo) | 2–4 frames. Brillo pulsante. Loop. |
| Animación: portada | Sin animación propia — el personaje la sostiene visible. |
| Sprite: puerta con llave | Marca dorada en la puerta. Misma paleta que la llave. |

| *[ INSERTAR IMAGEN ]  Concept art de la llave y puerta vinculada* |
| --- |

**5.6 Jefe Final: El Golem de Piedra**

**Descripción**

El Guardián del Templo. Un Golem de Piedra gigante que custodia la Cámara del Tesoro. Es el obstáculo final del juego. Tiene dos fases de combate con patrones de ataque diferenciados. La arena donde se desarrolla el combate forma parte del diseño del Nivel 6.

| *[ INSERTAR IMAGEN ]  Concept art del Golem de Piedra — Tamaño relativo al jugador* |
| --- |

Los parámetros técnicos del Golem (fases, cadencia, condición de victoria) están en (*ver Sección 15.4*)

**Fase 1**

**—  **El Golem dispara proyectiles de piedra en patrones predecibles y lentos.

**—  **El jugador esquiva usando el gancho para balancearse entre los proyectiles.

**—  **Cadencia inicial: lenta. El jugador tiene tiempo de leer los patrones.

**Fase 2**

**—  **La cadencia de disparo aumenta notablemente.

**—  **El Golem añade ráfagas cortas además de proyectiles individuales.

**—  **El entorno cambia: algunas plataformas de la arena comienzan a colapsar.

**—  **El jugador debe moverse constantemente usando el gancho.

**Condición de Victoria**

El jugador debe alcanzar el punto débil del Golem durante las ventanas de oportunidad que aparecen entre sus ataques. Al ser derrotado el Golem colapsa, la cámara del tesoro se ilumina y el juego muestra la pantalla de victoria. El diseño exacto del punto débil y la mecánica de impacto queda a cargo de Game Design.

**Ficha de Arte**

| **Elemento** | **Descripción** |
| --- | --- |
| Tamaño sprite | 48×64 px mínimo. Que tape parte visible del nivel. |
| Paleta | Roca oscura, musgo, runas doradas brillantes. Punto débil destacado. |
| Animación: idle | 4–6 frames. Respiración pesada, polvo cayendo. |
| Animación: ataque | 8–10 frames. Movimiento de brazo o boca para disparar. |
| Animación: Fase 2 | Crack visual en el cuerpo, ojos más brillantes. |
| Animación: muerte | 10–12 frames. Colapso de piedra. Satisfactorio y definitivo. |

**6.  ****DISEÑO DE NIVELES**

**6.1 Filosofía**

Los 6 niveles son lineales: un único camino hacia la meta. El diseño lineal mantiene el foco en el movimiento fluido y permite una curva de dificultad controlada. Cada nivel introduce una o dos mecánicas nuevas y las combina con las anteriores.

Aprendizaje implícito: ningún nivel usa texto en pantalla para enseñar mecánicas. El entorno guía al jugador con geometría y posicionamiento. Un punto de anclaje visible sobre un abismo enseña que el gancho es necesario. El diseño habla, el texto calla.

**6.2 Tabla de Niveles**

| **N°** | **Nombre** | **Mecánicas presentes** | **Enemigos** | **Dur. est.** |
| --- | --- | --- | --- | --- |
| 1 | Tutorial — Jungla Inicial | Movimiento, salto, gancho con carga, ajuste de soga, pinchos retráctiles, pared reactiva, plataforma de tracción, palanca, llave simple | Guerrero Espectral | 2–3 min |
| 2 | Entrada del Templo | Gancho obligatorio, plataformas estáticas complejas, llaves intermedias | Ninguno | 4–5 min |
| 3 | Cámaras de Piedra | Gancho + plataformas móviles + llaves dentro de enemigos | Guerrero Espectral | 4–5 min |
| 4 | Plataformas Peligrosas | Gancho + plataformas frágiles + eliminación ambiental | Momia del Templo | 5–6 min |
| 5 | Las Profundidades | Todo lo anterior + Lanzador + friendly fire ambiental | Ambos patrulleros | 5–6 min |
| 6 | Cámara del Tesoro | Prueba de maestría + Jefe Final | Golem | 3–4 min + boss |

**6.3 Nivel 1 — Tutorial: Jungla Inicial**

**Concepto**

El Nivel 1 es el punto de entrada al juego y la única oportunidad de presentar todos los sistemas sin abrumar al jugador. El objetivo es demostrar el juego completo: movimiento, gancho con carga, trampas, interacciones y un enemigo, todo introducido en orden de complejidad creciente. La primera mitad no tiene peligros letales. La segunda mitad combina lo aprendido con consecuencias reales.

| *[ INSERTAR IMAGEN ]  Esquema del Nivel 1 — Layout general (imagen a modo de concepto)* |
| --- |

**Elementos presentes**

**—  **Plataformas: estática, one-way, de tracción.

**—  **Trampas: foso de vacío, pinchos estáticos, pinchos retráctiles, fuego.

**—  **Objetos interactuables: pared reactiva al gancho, llave simple (en el mundo, sin enemigo).

**—  **Enemigo: Guerrero Espectral (1 instancia, zona contenida por pared SpectralWall).

**Beats del Nivel**

| **Beat** | **Zona** | **Qué aprende / enfrenta el jugador** |
| --- | --- | --- |
| 1 | Plataformas iniciales | Movimiento horizontal y salto. Tres plataformas en escalera ascendente. Sin riesgo. |
| 2 | Gap pequeño | El salto tiene alcance limitado. Un gap de 3 unidades requiere impulso previo. |
| 3 | Primer foso de vacío | Foso de 4 unidades. No se puede cruzar saltando. Obliga a usar la plataforma one-way sobre él. |
| 4 | Descubrimiento del gancho | Abismo de 6 unidades. Imposible cruzar saltando. Punto de anclaje dorado centrado. La geometría dice: mirá arriba. |
| 5 | Carga del gancho | El siguiente anclaje está a 8 unidades. Carga mínima no alcanza. El jugador aprende que sostener más tiempo da más alcance. |
| 6 | Pared reactiva | Pared de madera bloquea el paso con argolla visible. El jugador engancha y tira — la pared cae. |
| 7 | Pinchos retráctiles | Corredor con pinchos que salen del suelo cada 1.5s. El jugador aprende el timing y cruza en la ventana segura. |
| 8 | Llave simple | Llave dorada en el suelo junto a una puerta intermedia cerrada. El jugador descubre que agarrarla deshabilita el gancho y que acercarse abre la puerta. |
| 9 | Guerrero Espectral | Primera aparición de un enemigo. Patrulla una zona con paredes SpectralWall. El jugador observa que el Espectral las respeta. Zona de fuego cercana insinúa la mecánica de eliminación. |

**Tutorial Implícito**

**—  **Movimiento: plataformas anchas y bajas. Imposible caer en la primera zona.

**—  **Salto: el primer gap tiene el ancho justo. Sin timing perfecto requerido.

**—  **Foso de vacío: la oscuridad debajo comunica el peligro antes de llegar al borde.

**—  **Gancho — descubrimiento: el abismo de 6 unidades es infranqueable a pie. El punto de anclaje dorado está centrado y visible desde 10 unidades.

**—  **Carga del gancho: el segundo anclaje obliga a cargar más. El jugador experimenta la escala por sí mismo.

**—  **Pared reactiva: la argolla visible sobre la madera es la señal. El jugador que ya sabe usar el gancho lo intenta de forma natural.

**—  **Pinchos retráctiles: el primer corredor tiene un solo pincho con timing lento. El jugador observa el ciclo antes de necesitar cruzar.

**—  **Llave: la puerta cerrada y la llave al lado comunican la relación sin texto. El jugador recoge, intenta el gancho y falla — aprende la restricción.

**—  **Guerrero Espectral: aparece en zona contenida. El jugador tiene tiempo de leer el patrón. Las paredes doradas y el fuego cercano enseñan ambas mecánicas sin forzar el combate.

**Estética**

**—  **Paleta: verdes vibrantes, ocres cálidos, luz solar filtrada. El nivel más colorido del juego.

**—  **Puntos de anclaje: vigas de madera dura, raíces gruesas — visualmente distintos de la vegetación de fondo.

**—  **Pinchos retráctiles: color rojo intenso, animación de salida clara. Legibles sobre el fondo verde.

**—  **Paredes SpectralWall: ornamentación dorada visible incluso sobre el pixel art de jungla.

| *Diseño MVP: este nivel debe poder jugarse en su totalidad con el build del Parcial 1. Todos los sistemas presentes tienen que estar funcionales. El Nivel 1 es la demo del juego.* |
| --- |

**6.4 Nivel 2 — Entrada del Templo**

**Concepto**

El explorador entra al interior del templo. Arquitectura de piedra gris, antorchas, techos altos. El gancho es obligatorio para avanzar. Sin enemigos. Introduce las llaves como puzle de movimiento: el jugador debe trasladar llaves a través de zonas que requieren el gancho para cruzar.

| *[ INSERTAR IMAGEN ]  Esquema del Nivel 2 — A completar por Game Design* |
| --- |

| *[ INSERTAR IMAGEN ]  Tileset completo Nivel 2 — Entrada del Templo* |
| --- |

**6.5 Nivel 3 — Cámaras de Piedra**

**Concepto**

Interior profundo del templo. Las plataformas móviles son el elemento central. Introduce las llaves dentro de enemigos: el Guerrero Espectral porta una llave que el jugador debe obtener conduciéndolo hacia una zona de fuego.

| *[ INSERTAR IMAGEN ]  Esquema del Nivel 3 — A completar por Game Design* |
| --- |

**6.6 Nivel 4 — Plataformas Peligrosas**

**Concepto**

Aparece la Momia del Templo. Las plataformas frágiles se introducen. El jugador aprende la eliminación ambiental: la Momia patrulla sobre plataformas frágiles y de tracción que el jugador puede usar tácticamente para eliminarla.

| *[ INSERTAR IMAGEN ]  Esquema del Nivel 4 — A completar por Game Design* |
| --- |

**6.7 Nivel 5 — Las Profundidades**

**Concepto**

La zona más oscura y hostil del templo. Ambos patrulleros presentes junto al Lanzador. Introduce el friendly fire ambiental: el jugador puede conducir a la Momia hacia la línea de fuego del Lanzador para eliminarla.

**Escena scriptada (estilo Indiana Jones)**

El Nivel 5 contiene un momento set-piece construido con los sistemas de evento (ver Sección 4.6). Al recoger una **reliquia/ídolo** (RelicaPickup), un único disparador encadena las consecuencias cableadas desde el Inspector: por ejemplo el techo cede (ObjetoActivable), una **roca rodante** (RollingBoulder) persigue al jugador por el corredor, surgen trampas (ObjetoActivable) y el jugador cambia de apariencia (PlayerSkinSwapper). Las consecuencias se disparan al instante al recoger la reliquia.

| *ℹ  El set-piece refuerza el clímax del recorrido más hostil. La roca rodante crea una secuencia de huida de lectura inmediata; el resto de efectos se ajustan desde el UnityEvent de la reliquia sin tocar código.* |
| --- |

| *[ INSERTAR IMAGEN ]  Esquema del Nivel 5 — A completar por Game Design* |
| --- |

**6.8 Nivel 6 — Cámara del Tesoro**

**Concepto**

El nivel final tiene dos partes: un recorrido de alta dificultad que combina todas las mecánicas aprendidas, y la arena del Golem de Piedra. La cámara es visualmente espectacular — dorada, brillante, en contraste total con la oscuridad de las Profundidades.

| *[ INSERTAR IMAGEN ]  Esquema del Nivel 6 — Recorrido + Arena del Golem — A completar por Game Design* |
| --- |

**7.  ****ESTÉTICA Y DIRECCIÓN VISUAL**

**7.1 Filosofía Visual**

Templo Utaki usa pixel art de 8×8 píxeles por tile, con una paleta cálida y selvática que evoluciona hacia tonos más oscuros y místicos a medida que el jugador desciende en el templo. El contraste entre el exterior selvático (Nivel 1) y la cámara dorada (Nivel 6) es el arco visual del juego.

**7.2 Paleta de Colores**

| **Zona** | **Paleta dominante** | **Descripción** |
| --- | --- | --- |
| Exterior — Jungla | Verdes, ocres, luz amarilla | Nivel 1. Vibrante, cálido, natural. Luz solar filtrada entre árboles. |
| Entrada del Templo | Gris piedra, marrón, antorchas | Niveles 2–3. Transición al interior. Menos luz, más sombra. |
| Interior Profundo | Negros, ocres, fuego | Niveles 4–5. Oscuro, amenazante. Fuentes de luz artificiales. |
| Cámara del Tesoro | Dorado, crema, brillos | Nivel 6. Oscuridad rota por el brillo del tesoro. |

**7.3 Especificaciones Técnicas de Arte**

**—  **Tamaño de tile base: 8×8 píxeles.

**—  **Resolución de pantalla: 1920×1080. Los tiles se escalan con factor entero para mantener la nitidez del pixel art.

**—  **Paleta por nivel: cada nivel tiene su propia paleta de ≤16 colores para mantener coherencia visual.

**—  **Sin antialiasing en sprites — el pixel art debe verse nítido, nunca suavizado.

**—  **Partículas y efectos: sprites animados simples, sin shaders complejos.

**7.4 Animaciones del Personaje**

La tabla completa de animaciones del personaje con cantidades de frames está en (*ver Sección 15.7*)

**8.  ****AUDIO**

**8.1 Filosofía**

Los SFX son la prioridad máxima. Cada acción del gancho, cada tipo de superficie y cada amenaza tiene una respuesta sonora clara y diferenciada. El jugador debe poder entender qué está pasando. La música es ambiental — sostiene la atmósfera sin competir con los SFX.

**8.2 SFX Prioritarios**

La lista completa de SFX con descripción y nivel de prioridad está en (*ver Sección 15.6*)

**8.3 Música**

**—  **La música es ambiental: loops sin puntos de inicio obvios, sin melodía dominante que distraiga.

**—  **Varía por zona: la jungla tiene sonidos naturales + percusión suave. El interior del templo tiene drones oscuros + percusión lenta.

**—  **La arena del jefe final tiene una pieza diferenciada: más rítmica, mayor intensidad.

**—  **El volumen de la música se reduce durante momentos de alta tensión para dar protagonismo a los SFX.

**—  **La música es **continua**: pasar entre niveles de la misma zona —o reiniciar el nivel tras morir— **no** reinicia la pista; solo cambia (con crossfade) al cambiar de zona. La pista de cada escena se configura de forma centralizada en el `AudioManager` (mapa por buildIndex), accesible desde el menú.

**9.  ****UI Y SISTEMAS DE JUEGO**

**9.1 HUD**

El HUD es mínimo. Solo muestra información que el jugador necesita para tomar decisiones. Sin barra de salud, sin minimapa, sin indicadores de cooldown.

**—  **Contador de cristales recolectados / total del nivel (esquina superior derecha).

**—  **Contador de muertes en el nivel actual (opcional — puede ocultarse en menú de opciones).

**—  **Ícono de llave (esquina inferior izquierda): aparece cuando el jugador sostiene una llave. Desaparece al soltarla o usarla. Indica visualmente que el gancho no está disponible.

**9.2 Coleccionables — Cristales de Puntuación**

Distribuidos a lo largo de cada nivel, los cristales son opcionales. No afectan la progresión. Su función es dar replay value y recompensar la exploración dentro del diseño lineal.

| **Parámetro** | **Descripción** |
| --- | --- |
| Recolección | Contacto directo del jugador. Feedback: 'ding' + brillo breve. |
| Efecto en juego | Ninguno. Solo puntuación. |
| Al morir / reaparecer | Los cristales **ya recogidos no reaparecen** — el progreso de cristales se conserva al morir (persistencia por posición en el `GameLoopManager`). El contador se mantiene. |
| Al reiniciar manual (R) o cambiar de nivel | Los cristales reaparecen y el contador vuelve a cero. No se acumulan entre niveles. |
| Cantidad por nivel | Sin número fijo. Game Design los ubica según el layout del nivel. |
| HUD | Indicador pequeño siempre visible: cristales recogidos en el nivel actual. |
| Fin de nivel | Muestra cristales obtenidos / total del nivel. |

**9.3 Estados del Juego**

| **Estado** | **Descripción** |
| --- | --- |
| Menú Principal | Jugar, Opciones, Salir. Submenús (opciones/controles) organizados por pestañas. ESC cierra el panel abierto. Transición suave con fade antes de cargar o salir. MenuManager autónomo — sin dependencia de GameLoopManager. |
| En Juego | HUD mínimo activo. El juego arranca desde el Nivel 1. |
| Pausa | Tiempo congelado. Opciones: Reanudar, Reintentar, Opciones (sonido), Controles, Menú Principal. ESC reanuda directamente. Los subpaneles (opciones/controles) se cierran solos al reanudar. |
| Muerte — por trampa | Secuencia dramática breve (SpotlightOverlay + pausa corta) antes del reinicio automático. |
| Muerte — por caída al vacío | Reinicio automático inmediato, sin secuencia. Si hay checkpoint activo, reaparece desde ese punto. |
| Fin de Nivel | Pantalla breve con tres datos: tiempo en nivel (mm:ss), muertes en el nivel y cristales obtenidos / total. Continuar al siguiente. |
| Victoria | Pantalla de créditos con puntuación total y contador de muertes (acumulado de la partida). |

**9.4 Sistema de Checkpoints**

Los niveles pueden contener zonas de checkpoint colocadas por Game Design. Al entrar en un checkpoint, el juego guarda la posición del jugador en ese momento. Si el jugador muere después, reaparece desde ese punto en lugar de desde el inicio del nivel.

| *ℹ  Principio de diseño: el checkpoint suaviza la dificultad en niveles largos sin eliminar el desafío — define solo el punto de reaparición. Los cristales recogidos se conservan al morir (no dependen del checkpoint). Reiniciar explícitamente el nivel (tecla R) borra el checkpoint activo y reinicia los cristales del nivel.* |
| --- |

**—  **Solo hay un checkpoint activo por sesión de nivel. Activar uno nuevo sobreescribe el anterior.

**—  **Al morir, el nivel se recarga y el jugador aparece en la posición del checkpoint. Los cristales ya recogidos se conservan (persisten entre recargas), no se restauran a un valor anterior.

**—  **Al avanzar al siguiente nivel o reiniciar manualmente, el checkpoint se borra. El siguiente intento arranca desde el inicio.

**9.5 Selector de Niveles**

Pantalla de selección de niveles con desbloqueo por progresión. Reutiliza la mecánica nativa de los botones de UI de Unity: el botón se resalta al pasar el cursor (estado Highlighted) y carga el nivel al presionar.

**—  **Persistencia: el progreso se guarda con `PlayerPrefs` (clave única con el mayor nivel desbloqueado). Persiste entre sesiones de juego. Fuente única de verdad: `ProgresoNiveles`.

**—  **Desbloqueo: al completar un nivel se desbloquea el siguiente (lo escribe `GameLoopManager.NivelCompleto()`). El selector (`SelectorNiveles`) lee el progreso y habilita o bloquea cada botón.

**—  **El **Nivel 1 está siempre desbloqueado** (marca `siempreDesbloqueado` por botón, además de la garantía del primer nivel en `ProgresoNiveles`).

**—  **Estado visual: cada botón intercambia su sprite según el estado — sprite normal cuando está desbloqueado, sprite de bloqueado (candado) cuando no. El botón bloqueado además queda no interactuable.

**—  **Convención de Build Settings: índice 0 = menú, 1..N = niveles jugables.

| *ℹ  Principio de diseño: el selector da acceso rápido a niveles ya superados (replay, búsqueda de cristales) sin permitir saltear contenido no alcanzado. El reseteo de progreso (testing/opciones) nunca bloquea el Nivel 1.* |
| --- |

**PARTE II**

*ESPECIFICACIONES TÉCNICAS*

**10.  ****STACK TECNOLÓGICO**

**10.1 Tecnologías**

| **Componente** | **Tecnología / Versión** |
| --- | --- |
| Motor de juego | Unity 6000.0.30f1 |
| Pipeline de renderizado | Universal Render Pipeline 2D (URP 17.0.3) |
| Lenguaje | C# |
| Sistema de input | Unity Input System 1.11.2 |
| Control de versiones | Git — GitHub (uadelaburos-Prog/Templo_Utaki) |
| Física del gancho | DistanceJoint2D + Rigidbody2D |
| Cuerda visual | LineRenderer — 20 segmentos — curva Bezier cuadrática |
| Resolución objetivo | 1920×1080 (fija en PC) |
| FPS objetivo | 60 FPS estables |

**10.2 Requisitos Mínimos de PC**

**—  **Sistema operativo: Windows 10 / macOS 11 / Ubuntu 20.04

**—  **Procesador: Intel Core i3 o equivalente

**—  **Memoria RAM: 4 GB

**—  **GPU: soporte OpenGL 4.5

**—  **Almacenamiento: ~200 MB

**—  **Resolución mínima: 1920×1080

**11.  ****ARQUITECTURA DE CÓDIGO**

**11.1 Scripts Principales**

| **Script** | **Responsabilidad** | **Estado** |
| --- | --- | --- |
| PlayerMovement.cs | Movimiento horizontal, salto, gravedad variable (rise/fall/swing scale), coyote time, jump cut, air accel/decel, detección de suelo (rb.GetContacts con groundNormalThreshold), SFX salto y aterrizaje. | ✅ Implementado |
| GrappleScript.cs | Ciclo de estados del gancho, sistema de carga (chargeTimer), ajuste de longitud de soga (W/S), raycast, snap automático, LineRenderer (Bezier), anclaje reactivo (ReactiveWall.OnHooked), retracción de objetos, highlight de superficies grappleables. Llama OnHooked() en BreakableAnchor al engancharse. | ✅ Implementado |
| MummyAI.cs | Momia del Templo. Máquina de estados: Idle, Patrulla, Persecución, Muerto. Patrulla por rebote (sin waypoints) con raycasts de borde y pared. Salta paredes superables y salta en persecución si el jugador está más arriba. Detección con línea de visión (Linecast contra maskVisionBloqueo — no detecta a través de paredes/suelo). Ícono «!». SFX alerta. Suelta llave (KeyCarrier) al morir. Muerte vía GameLoopManager. | ✅ Implementado |
| PatrollerAI.cs | Patrullero físico genérico (base histórica). Estados Idle, Patrulla, Persecución, Regreso con waypoints PuntoA/PuntoB. Velocidades 2/4/3 u/s. No usado por la Momia actual (la reemplaza MummyAI.cs). | ✅ Implementado (legacy) |
| MovingPlatform.cs | Plataforma móvil con herencia de velocidad al jugador. Movimiento vía Lerp lineal con rebote. | ✅ Implementado |
| FragilePlatform.cs | Plataforma frágil: timer de rotura, 3 fases visuales (amarillo→naranja→rojo), regeneración, SFX crujido y rotura. | ✅ Implementado |
| OneWayPlatform.cs | Plataforma one-way: atravesable desde abajo. | ✅ Implementado |
| SpikeHazard.cs | Pinchos estáticos y retráctiles. 3 estados: Retraído / Asomando (sin daño) / Desplegado. DireccionSalida enum (Arriba/Abajo/Izquierda/Derecha). SpikeGroup con ModoCiclo: Sincronizado / Desfasado / Secuencial (delayEntreSpikes=0.15s). | ✅ Implementado |
| VoidScript.cs | Foso de vacío: trigger → PlayerDied(fromVoid:true). Reinicio inmediato sin SpotlightOverlay. | ✅ Implementado |
| GameLoopManager.cs | Gestión de nivel: inicio, fin, reinicio, cristales, contador de muertes. **Cristales persistentes**: HashSet por posición (no reaparecen al morir; se reinician al Reintentar o cambiar de nivel). **Dos contadores de muertes**: global de la partida (HUD/victoria) y por nivel (panel de fin de nivel). Panel de fin de nivel con tres textos (tiempo mm:ss, muertes del nivel, cristales). Referencia a panelControles (lo cierra por seguridad al despausar/cambiar escena; el switching de pestañas es por Inspector). Sistema de muerte: distingue hazard (RutinaMuerteDramatica con SpotlightOverlay + timeScale=0) y vacío (RutinaReinicio inmediata). Flag isDying previene muertes simultáneas. Sistema de checkpoints: GuardarCheckpoint(), AplicarSpawnCheckpoint(), LimpiarCheckpoint(). NivelCompleto() desbloquea el siguiente nivel vía ProgresoNiveles.DesbloquearHasta(). Ya **no** gestiona música. DontDestroyOnLoad. Hijo directo: SpotlightCanvas. Instanciar en Nivel 1. | ✅ Implementado |
| AudioManager.cs | Singleton con DontDestroyOnLoad. SFX espaciales (FxObject). **Autoridad de música por escena**: array musicaPorEscena indexado por buildIndex, suscrito a sceneLoaded, reproduce la pista de cada escena (incluido el menú). PlayMusic **idempotente** (no reinicia si ya suena ese clip → continuidad al morir y entre escenas de la misma zona) con crossfade (SwapingVolume). Control de volumen vía AudioMixer. Se configura una sola vez desde el menú. | ✅ Implementado |
| CamaraScript.cs | Seguimiento con Lerp, look-behind horizontal, look-down proporcional a velocidad de caída. SnapToPlayer() al respawnear desde checkpoint. | ✅ Implementado |
| CrystalPickup.cs | Trigger de recolección, comunicación con GameLoopManager. | ✅ Implementado |
| HazardFire.cs | Zona de fuego: mata Player (fromVoid=false) + llama SpectralPatrollerAI.Morir() y PatrollerAI.Morir(). | ✅ Implementado |
| ReactiveWall.cs | Pared reactiva al gancho. Animación de rotación de caída. Al finalizar Derribo() pasa a RigidbodyType2D.Static — queda como puente sólido permanente. OnHooked/OnReleased. | ✅ Implementado |
| SpectralPatrollerAI.cs | Máquina de estados del Guerrero Espectral: patrulla (waypoints A/B), idle, regreso, órbita orgánica (tangencial + resorte radial; orbitRadius 3u ± 0.4u sinusoidal, orbitVelocity 1.5 rad/s), windup con parpadeo blanco (0.35s), dash (18 u/s, 8u, cooldown 2s, dashTriggerRange 3.5u, playerStillTime 1.5s), recover (0.4s). Detección radio 6u / abandono 8.5u. AfterimageTrail durante dash. isTrigger forzado en Awake. Bloqueado solo por SpectralWall. Muerte por HazardFire (tag SpectralEnemy). | ✅ Implementado |
| CheckpointZone.cs | Objeto en escena (trigger one-shot). Al entrar el jugador: llama GameLoopManager.GuardarCheckpoint(). Restaura estado visual del animator al recargar (tolerancia 0.1u). SFX de activación. | ✅ Implementado |
| BreakableAnchor.cs | Superficie grappleable destructible. Se activa mediante OnHooked(GrappleScript) al engancharse. Tres fases de advertencia visual antes de la rotura. El gancho se retrae automáticamente al romperse. Parámetros: permanentBreak (bool), regenDelay (float, default 5s). | ✅ Implementado |
| MenuManager.cs | Gestión del menú principal. Autónomo — no depende de GameLoopManager. IniciarJuego(), IrANivel(indice), Salir(), AbrirOpciones(), CerrarOpciones(). ESC cierra el panel abierto (array panelesCerrablesConEsc, uno por pulsación). Transiciones con CanvasGroup fade 0.4s (unscaledDeltaTime); ya **no** corta la música al entrar a un nivel (el destino la cambia con crossfade), solo StopMusic al salir del juego. La música del menú la maneja el AudioManager (buildIndex 0). | ✅ Implementado |
| SelectorNiveles.cs | Selector de niveles (UI). Habilita/bloquea botones según el progreso (ProgresoNiveles) e intercambia el sprite de cada botón (normal/bloqueado). Cablea el onClick para cargar el nivel con fade. Flag siempreDesbloqueado por nivel. RefrescarBotones() en OnEnable. | ✅ Implementado |
| ProgresoNiveles.cs | Persistencia del progreso de niveles vía PlayerPrefs (clase estática). MaxDesbloqueado, EstaDesbloqueado(idx), DesbloquearHasta(idx), Reiniciar(). El Nivel 1 nunca baja del mínimo. | ✅ Implementado |
| RelicaPickup.cs | Reliquia/ídolo recolectable por proximidad (OverlapCircle) con flotación. Al recogerse dispara un UnityEvent (una sola vez) que cablea las consecuencias de la escena scriptada. | ✅ Implementado |
| ObjetoActivable.cs | Activa/desactiva paredes, terreno o trampas por evento (Collider2D + Renderer propios e hijos). activoInicial, animación de aparición por offset. Activar()/Desactivar()/Alternar(). | ✅ Implementado |
| RollingBoulder.cs | Roca rodante estilo Indiana Jones. Dormida hasta Activar(): cae y rueda horizontalmente girando con su velocidad. Contacto con jugador = reinicio; aplasta a la Momia. | ✅ Implementado |
| PlayerSkinSwapper.cs | Cambia el RuntimeAnimatorController del jugador a una variante (Override Controller recomendado), conservando estados/transiciones. Tinte opcional. CambiarVariante()/RestaurarOriginal()/Alternar(). | ✅ Implementado |
| KeyDoor.cs | Vínculo llave-puerta. Detección de proximidad del jugador con llave (radioApertura 2u). Animación de apertura por frames con desplazamiento ascendente (duracionFrame 0.08s). Collider desactivado al comenzar la animación. | ✅ Implementado |
| KeyItem.cs | Física de la llave, recolección por proximidad (radioPickup 0.8u) o gancho, lanzamiento con sistema de carga (velocidadMin 6 / velocidadMax 20, maxCargaTiempo 1.5s), rozamiento en suelo, respawn al caer al void, cooldown de recogida 0.5s. | ✅ Implementado |
| KeyCarrier.cs | Componente para enemigos que portan una llave. SoltarLlave() la deja caer con física en el punto de muerte. offsetPortada configurable. | ✅ Implementado |
| LauncherAI.cs | Timer de disparo (cadencia 2.5s), instanciación de proyectil, dirección de disparo fija configurable. | ✅ Implementado |
| Projectile.cs | Movimiento del proyectil (velocidad 8 u/s), detección de impacto (jugador y Momia), destrucción por tiempo (tiempoVida 10s) o borde. | ✅ Implementado |
| LevelExit.cs | Trigger de fin de nivel → NivelCompleto(). | ✅ Implementado |
| CrystalPickup.cs | Recolección por proximidad (pickupRadius 0.6u), flotación + rotación visual, comunicación con GameLoopManager. | ✅ Implementado |
| OneWayPlatform.cs | Plataforma one-way: atravesable desde abajo, bajar con S/↓ (duracionBajar 0.3s). | ✅ Implementado |
| SpikeHazard.cs / SpikeGroup.cs | Pinchos estáticos y retráctiles (3 fases: Retraído / Asomando sin daño / Desplegado). DireccionSalida enum. SpikeGroup orquesta grupos con ModoCiclo (Sincronizado / Desfasado / Secuencial, delayEntreSpikes 0.15s). **Secuencia manual:** `faseInicial` (0–1) y `delayInicial` se combinan — tras la espera oculta, el pincho arranca en el punto del ciclo de la fase. Permite cascadas con pinchos sueltos sin SpikeGroup. | ✅ Implementado |
| HazardFire.cs | Zona/llamarada de fuego con ciclo configurable (inactivo/creciendo/activo/menguando). Mata Player y elimina Espectral (SpectralPatrollerAI) y Momia (MummyAI/PatrollerAI). | ✅ Implementado |
| Lever.cs | Palanca/switch: alterna estado ON/OFF de objeto vinculado por contacto. | ✅ Implementado |
| ReactiveWall.cs | Pared reactiva al gancho. Cae al superar distanciaDerribo (1.5u), animación de rotación (angulosCaida 90°, duracionCaida 0.4s). Al terminar pasa a RigidbodyType2D.Static (puente sólido). IHookable. | ✅ Implementado |
| BreakableAnchor.cs | Anclaje grappleable destructible. OnHooked() dispara la rotura (breakDelay 0.6s, 3 fases). Retracción automática del gancho al romperse. permanentBreak o regenDelay 5s. | ✅ Implementado |
| SpotlightOverlay.cs | Efecto de muerte dramática (shader Custom/SpotlightOverlay): radius 0.18, softness 0.10, maxAlpha 0.92. Hijo del GameLoopManager. | ✅ Implementado |
| CameraZone.cs | Zonas que ajustan los límites de cámara al entrar (extLeft/Right/Bottom/Top, transitionSpeed). | ✅ Implementado |
| ParallaxBackground.cs | Desplazamiento parallax de capas de fondo según la cámara. | ✅ Implementado |
| IHookable.cs | Interfaz para objetos interactuables por gancho (OnHooked/OnReleased). Implementada por ReactiveWall y BreakableAnchor. | ✅ Implementado |
| GolemBoss.cs | Máquina de estados del Jefe Final, fases, patrones de ataque, condición de victoria. | 📋 Planeado |

**11.2 Sistema de Capas y Tags (Unity)**

**Capas (Layers)**

| **Capa** | **Uso** | **Interacciones clave** |
| --- | --- | --- |
| Default | Objetos genéricos sin comportamiento especial. | — |
| Floor | Suelo y plataformas. Detección de isGrounded. | Colisiona con Player y Momia. No con Espectral. |
| Grappleable | Superficies de anclaje para movimiento con gancho. El jugador se cuelga de ellas. | Detectada por Raycast del GrappleScript para enganche. |
| Hookeable | Objetos que el gancho puede interactuar o mover, pero no servir de anclaje. Incluye las llaves. | Detectada por Raycast del GrappleScript para interacción y retracción con S. |
| Obstacle | Bloquea físicamente el paso del gancho sin activar enganche ni interacción. | Bloquea el Raycast. No activa ninguna acción. |
| Enemy | Enemigos. Activa muerte del jugador al contacto. | Colisiona con Player. |
| Hazard | Trampas: pinchos, fuego. | Trigger con Player y Momia. Activa muerte. El fuego activa muerte del Espectral también. |
| SpectralWall | Paredes doradas/rúnicas que bloquean al Guerrero Espectral. | Colisiona con Espectral. Ignorada por Player y Momia. |
| Collectible | Cristales de puntuación. | Trigger con Player. |

**Tags**

| **Tag** | **Objeto** | **Uso** |
| --- | --- | --- |
| Player | El explorador. | Identificación para colisiones de enemigos y trampas. |
| Key | Llaves del nivel. | GrappleScript detecta el tag para activar retracción con S. KeyDoor detecta el tag para verificar apertura. |
| SpectralEnemy | Guerrero Espectral. | HazardFire.cs detecta el tag para activar muerte del Espectral al contacto con fuego. |
| Crystal | Cristales de puntuación. | CrystalPickup.cs. |

**11.3 Parámetros Clave Configurables**

La tabla maestra de todos los parámetros configurables del juego está en (*ver Sección 15*)

| **Parámetro** | **Valor actual** | **Notas** |
| --- | --- | --- |
| Velocidad de carrera | 9 u/s | SerializeField en PlayerMovement.cs |
| Velocidad de salto | 12 u/s | SerializeField en PlayerMovement.cs |
| Coyote time | 0.12s | SerializeField en PlayerMovement.cs |
| Salto en cola (jumpQueued) | Hasta aterrizar | PlayerMovement.cs — no es ventana de 0.15s; se mantiene sin expirar hasta tocar suelo |
| Tiempo mín. de carga | 0.1s | SerializeField en GrappleScript.cs |
| Tiempo máx. de carga | 1.5s | SerializeField en GrappleScript.cs — ⚠️ Auditado (era 1.0s) |
| Alcance mínimo | 3 u | SerializeField en GrappleScript.cs — ⚠️ Auditado (era 5u) |
| Alcance máximo | 10 u | SerializeField en GrappleScript.cs — ⚠️ Auditado (era 15u) |
| Radio de snap | 0.4 u | SerializeField en GrappleScript.cs — ⚠️ Auditado (era 1.5u) |
| Cooldown de fallo | 0.3s | SerializeField en GrappleScript.cs |
| Velocidad proyectil Lanzador | 8 u/s | SerializeField en Projectile.cs |
| Cadencia de disparo | 2.5s | SerializeField en LauncherAI.cs |

**12.  ****EQUIPO DE DESARROLLO**

**12.1 Roles**

| **Nombre** | **Rol** | **Área principal** |
| --- | --- | --- |
| Bono Dipacce | Game Design / Prog. | GDD, diseño de niveles, parámetros de mecánicas. Aprobación final en GD y Programación. |
| Fermin Blanco | Programación | PlayerMovement, plataformas, obstáculos, enemigos estándar. |
| Eliel Denmon | Programación / Audio | Sistemas de audio, SFX, música ambiental, integración sonora. |
| Belen Almed | Arte | Sprites del personaje, enemigos, Golem, Tileset Nivel 6. |
| Julieta Cerelli | Arte | Tilesets Niveles 1 y 2. Arte de entorno. |
| Santiago Calvo | Producción / QA | Backlog, milestones, testing funcional y de feel. Tuning de parámetros. |

**12.2 Herramientas**

**—  **Motor: Unity 6000.0.30f1

**—  **Control de versiones: Git — GitHub

**—  **Gestión de tareas: Jira Cloud

**—  **Documentación: Google Drive — carpeta 'Produccion de Videojuegos'

**—  **Arte: Aseprite (pixel art)

**—  **Audio: a definir por el área de sonido

**13.  ****GLOSARIO**

| **Término** | **Definición** |
| --- | --- |
| Carga del gancho | Sistema que escala fuerza y alcance del lanzamiento según el tiempo que se mantiene presionado Click izquierdo. |
| Ajuste de soga | Mecánica que permite modificar la longitud de la cuerda mientras el jugador está enganchado. W acorta, S alarga. |
| Eliminación ambiental | Mecánica táctica por la cual el jugador usa trampas o elementos del entorno para eliminar enemigos sin atacarlos directamente. |
| Guerrero Espectral | Patrullero fantasmal. Atraviesa la mayoría de superficies. Solo muere por fuego. |
| Momia del Templo | Patrullero físico. Mismas limitaciones de movimiento que el jugador. Vulnerable a todas las trampas ambientales. |
| Paredes doradas / rúnicas | Superficies que bloquean el paso del Guerrero Espectral. Visualmente distinguibles por su ornamentación dorada o brillante. |
| Llave | Objeto físico del mundo que desbloquea una puerta específica. Portarla deshabilita el gancho. |
| Puerta de llave | Puerta que requiere una llave vinculada para abrirse. Se abre por proximidad al sostener la llave correspondiente. |
| Friendly fire ambiental | Situación en la que el proyectil del Lanzador elimina a una Momia del Templo por interposición del jugador. |
| Coyote Time | Ventana de 0.12s después de salir de un borde en la que el jugador aún puede saltar. |
| Input Buffer / jumpQueued | El juego recuerda el input de salto hasta aterrizar. Implementación real: jumpQueued se anota al presionar Espacio y se mantiene sin expirar hasta tocar suelo (no es una ventana temporizada de 0.15s como decían versiones previas). Si el jugador aterriza con el salto en cola, este se ejecuta. |
| Jump Cut | Al soltar el botón de salto antes del apex, la velocidad vertical se reduce a ×0.5. La altura del salto varía según cuánto tiempo se mantiene el botón presionado, no según la velocidad horizontal previa. La distancia horizontal en el aire depende de la velocidad de movimiento al momento del salto, pero no es un requisito: el salto tiene impulso propio independiente de si el jugador venía corriendo o parado. |
| Hang Time | Gravedad reducida en el apex del salto para dar mayor sensación de control. |
| Grappleable | Capa de Unity asignada a superficies donde el gancho puede engancharse para balancearse. El jugador se cuelga de ellas y oscila como péndulo. |
| Hookeable | Capa de Unity asignada a objetos con los que el gancho puede interactuar o mover, pero no servir de anclaje. Incluye paredes reactivas, vigas destructibles, plataformas de tracción y llaves. |
| Tag Key | Identificador de Unity asignado a las llaves. Permite que GrappleScript las detecte para retracción con S y que KeyDoor verifique si el jugador la porta. |
| Snap | Asistencia de enganche: si hay una superficie válida en el radio proporcional a la carga, el gancho se engancha al punto más cercano. |
| DistanceJoint2D | Componente de Unity que simula la física del péndulo manteniendo distancia fija entre dos puntos. |
| Bezier cuadrática | Curva matemática usada para renderizar la cuerda con cuelgue natural según distancia y tensión. |
| Tile | Unidad mínima de arte del nivel. En Templo Utaki: 8×8 píxeles. |
| isGrounded | Variable booleana que indica si el jugador está tocando el suelo. |
| Milestone | Hito de producción. Los principales son: Prototipo, Alpha, Beta, Gold. |
| MVP | Minimum Viable Product: versión mínima jugable con el loop principal funcionando. |
| Beat | Unidad de diseño de nivel: un momento con un objetivo claro y una mecánica específica. |
| SerializeField | Atributo de Unity que expone una variable privada de C# al Inspector para editarla sin recompilar. |
| SpotlightOverlay | Efecto visual de muerte dramática: overlay negro con círculo despejado centrado en el jugador. Shader CG personalizado (Custom/SpotlightOverlay). Parámetros: Radio (0.18), Suavidad (0.10), Alpha máximo (0.92). Hijo del GameLoopManager para persistir entre escenas. |
| fromVoid | Parámetro booleano de PlayerDied(). True = muerte por vacío (reinicio inmediato, sin animación). False = muerte por hazard (secuencia dramática con SpotlightOverlay). |
| isDying | Flag en GameLoopManager que previene múltiples rutinas de muerte simultáneas (ej: pincho + vacío en el mismo frame). El primer llamado entra; los siguientes retornan inmediatamente. Se resetea en OnSceneLoaded. |
| UnscaledTime | Tiempo de Unity no afectado por Time.timeScale. Permite reproducir animaciones y corrutinas mientras el juego está pausado (timeScale = 0). Usado por la animación Death y el fade del SpotlightOverlay. |
| Ground Normal Threshold | Umbral de normal Y (0.5 por defecto ≈ 60°) para determinar si una superficie cuenta como suelo en isGrounded. Usa rb.GetContacts con filtro de normal Y. Evita que paredes verticales sean detectadas como suelo. Rango: 0.5–1.0. |
| Línea de visión (Momia) | La Momia (MummyAI.cs) solo detecta al jugador si, además de estar en el radio, hay línea de visión despejada: un Physics2D.Linecast contra maskVisionBloqueo (paredes/suelo) bloquea la detección. Evita que detecte al jugador a través de muros o del piso. Si maskVisionBloqueo está vacío, usa maskSuelo. |
| Patrulla por rebote | Modo de patrulla sin waypoints: el enemigo (Momia) camina en una dirección hasta detectar un borde o una pared insuperable con raycasts, y entonces gira. Reemplaza el esquema PuntoA/PuntoB del PatrollerAI legacy. |
| Órbita orgánica | Movimiento del Guerrero Espectral alrededor del jugador combinando una componente tangencial (giro) con un resorte radial suave (acercamiento gradual hasta orbitRadius). No es un círculo ni elipse rígidos. |

***PARTE II  (cont.)***

**14.  ****REGISTRO DE FEATURES**

*Templo Utaki  —  Demonic Arts Company  —  GDD v4*

**Leyenda**

| **✅ Completo** | Feature implementada y funcional en el build actual. |
| --- | --- |
| **🔄 WIP** | Parcialmente implementada o con bugs conocidos. |
| **📋 Planeado** | Diseñada en el GDD. Pendiente de implementación. |

**Tabla de Features**

| **ID** | **Feature** | **Descripción** | **Área** | **Notas** |
| --- | --- | --- | --- | --- |
| **▌ MOVIMIENTO BASE** |
| **F-001** | **Correr horizontal** | El jugador se desplaza izq/der con A/D o flechas. Aceleración al iniciar, desaceleración al soltar. | Movimiento | *Vel: 8–10 u/s. Aceleración: 16–20 u/s².* |
| **F-002** | **Salto base** | El jugador salta al presionar ESPACIO. Velocidad vertical inicial fija. | Movimiento | *Vel. vertical: 12 u/s. Altura: 5–6 u.* |
| **F-003** | **Jump Cut** | Al soltar ESPACIO antes del apex, la velocidad vertical se reduce a ×0.5. | Movimiento | *Ver PlayerMovement.cs — pendiente implementar.* |
| **F-004** | **Coyote Time** | Ventana de 0.12s tras salir de un borde donde el salto sigue siendo válido. | Movimiento | *Ausente en Prototipo.* |
| **F-005** | **Salto en cola (jumpQueued)** | El juego recuerda el input de salto hasta aterrizar. Implementación real: jumpQueued se mantiene sin expirar hasta tocar suelo (no una ventana de 0.15s). | Movimiento | *PlayerMovement.cs — ⚠️ Auditado: no es buffer temporizado.* |
| **F-006** | **Hang Time** | Gravedad reducida en el apex del salto para mayor sensación de control. | Movimiento | *Parámetro ajustable desde Inspector.* |
| **F-007** | **Control en el aire** | El jugador tiene influencia mínima sobre su trayectoria en el aire. | Movimiento | *No full-air-control.* |
| **F-008** | **Detección de suelo (isGrounded)** | Detecta si el jugador está en contacto con el suelo para habilitar salto y fricción. | Movimiento | *rb.GetContacts con filtro de normal Y ≥ groundNormalThreshold (0.7 ≈ 45°). Cero allocations por frame. Reemplaza Physics2D.OverlapBox que detectaba paredes verticales como suelo.* |
| **F-009** | **Caída con velocidad terminal** | La velocidad de caída está limitada a un máximo configurable. | Movimiento | *Vel. terminal: 12 u/s.* |
| **F-010** | **Fricción de suelo al aterrizar** | Al aterrizar, la fricción reduce la velocidad horizontal del jugador. | Movimiento | *Fricción: 0.6.* |
| **▌ GANCHO** |
| **F-011** | **Apuntado con ratón** | El cursor determina la dirección de lanzamiento del gancho en todo momento. | Gancho | *Input lag **<** 50ms.* |
| **F-012** | **Sistema de carga del gancho** | Mantener Click izquierdo carga el gancho. Fuerza y alcance escalan con el tiempo de carga. Soltar dispara. | Gancho | *⚠️ Auditado: alcance 3u→10u, carga máx 1.5s. Escala lineal.* |
| **F-013** | **Ícono de carga junto al personaje** | Mientras se mantiene Click, aparece un ícono junto al personaje que se llena progresivamente. Placeholder: barra. | Gancho | *Arte define ícono definitivo.* |
| **F-014** | **Lanzamiento del gancho** | Al soltar Click, el gancho se dispara en dirección al cursor con la fuerza y alcance acumulados. | Gancho | *launchSpeed 20 u/s. Arco con gravedad (hookGravity 18). maxFlightTime 0.6s.* |
| **F-015** | **Snap automático al anclaje** | Si hay superficie grappleable en el radio de impacto, el gancho se engancha al punto válido más cercano. | Gancho | *snapRadius 0.4u.* |
| **F-016** | **Retracción por superficie inválida** | Si el gancho impacta superficie no grappleable, regresa al jugador con animación de retracción completa. | Gancho | *Cooldown 0.3s.* |
| **F-017** | **Física de péndulo (balanceo)** | Una vez enganchado, el jugador oscila por gravedad pura sin fuerza adicional. | Gancho | *DistanceJoint2D. GrappleSwing usa AddForce — pendiente corregir.* |
| **F-018** | **Soltar gancho** | Al soltar Click estando enganchado, el gancho se libera y el jugador conserva la velocidad acumulada. | Gancho | *Conservación de momentum al soltar.* |
| **F-019** | **Retracción visual de la cuerda** | Al soltar el gancho, la cuerda se retrae visualmente hacia el jugador. | Gancho | *Estado RETRAYENDO en la máquina de estados.* |
| **F-020** | **Visualización de la cuerda (Bezier)** | La cuerda se renderiza con curva Bezier cuadrática: cuelga naturalmente según distancia y tensión. | Gancho | *LineRenderer 20 segmentos. Curva ausente en Prototipo.* |
| **F-021** | **Resaltado de anclajes válidos** | Los puntos de anclaje alcanzables según la carga actual se destacan visualmente. | Gancho | *Color de resaltado a definir por Arte.* |
| **F-022** | **Cooldown por fallo** | Si el gancho no alcanza anclaje válido o impacta superficie inválida, cooldown de 0.3s antes de relanzar. | Gancho |  |
| **F-023** | **Restricción de superficies** | Solo superficies en la capa 'Grappleable' admiten anclaje. Madera podrida, vidrio, vegetación y tierra no son válidas. | Gancho | *Layer 'Grappleable' en Unity.* |
| **▌ PLATAFORMAS Y ENTORNO** |
| **F-024** | **Plataforma estática** | Sólida, inmóvil. Soporta peso indefinido. | Entorno | *Fricción: 0.6.* |
| **F-025** | **Plataforma móvil** | Trayectoria predefinida. El jugador mantiene velocidad relativa sobre ella. | Entorno | *El gancho puede engancharse mientras se mueve.* |
| **F-026** | **Plataforma frágil** | Se rompe 1–2s tras ser pisada. Advertencia visual progresiva. Regenera en 5s. | Entorno | *El gancho puede engancharse pero el timer sigue.* |
| **F-027** | **Plataforma one-way** | Atravesable desde abajo. Soporta peso desde arriba. Gancho desde arriba solamente. | Entorno |  |
| **F-028** | **Plataforma de tracción** | Se desplaza hacia el jugador mientras el gancho está en tensión. | Entorno | *Al soltar, se detiene o retorna según config.* |
| **F-029** | **Pinchos estáticos** | Zona fija de muerte instantánea. | Entorno | *Layer Hazard.* |
| **F-030** | **Pinchos retráctiles** | Ciclo fijo configurable. Predecibles. Dirección configurable: Arriba / Abajo / Izquierda / Derecha. Grupos con tres modos: Sincronizado, Desfasado, Secuencial. | Entorno | *Nivel 1 los introduce. DireccionSalida enum en SpikeHazard.cs. ModoCiclo en SpikeGroup.cs.* |
| **F-031** | **Fuego** | Zona de área estática. Muerte al contacto. | Entorno |  |
| **F-032** | **Foso de vacío** | Caída fuera del nivel. Oscuridad comunica el peligro. | Entorno | *Trigger en zona baja del nivel.* |
| **F-033** | **Roca cayente** | Trampa activada por placa. Cae vertical, luego horizontal hasta pared. | Entorno | *Sombra proyectada + sonido de advertencia.* |
| **F-034** | **Placa de presión** | Activador en el suelo. Dispara trampa vinculada una vez por pisada. | Entorno | *Sistema extensible: activador → efecto.* |
| **F-035** | **Palanca / Switch** | Alterna estado ON/OFF de objeto vinculado. | Entorno |  |
| **F-036** | **Pared reactiva al gancho** | Al enganchar argolla y tirar, la pared cae con animación de rotación abriendo un pasaje. Al terminar la caída queda como superficie sólida permanente (puente caminable). | Entorno | *Nivel 1 lo introduce. ReactiveWall.cs — pasa a RigidbodyType2D.Static al finalizar Derribo().* |
| **F-037** | **Viga destructible** | El gancho la rompe en 1–2 usos. | Entorno |  |
| **F-038** | **Anclaje fijo** | Estático. Distancia constante. | Entorno | *Asignable a paredes, techos, vigas, columnas.* |
| **F-039** | **Anclaje móvil** | Se mueve. El jugador es arrastrado con él. | Entorno |  |
| **F-040** | **Anclaje destructible (BreakableAnchor)** | Se activa al engancharse. Tres fases de advertencia visual. Gancho se retrae automáticamente al colapsar. Configurable: permanentBreak o regeneración con regenDelay. | Entorno | *BreakableAnchor.cs. GrappleScript.cs llama OnHooked() en SnapAndAttach().* |
| **F-041** | **Anclaje reactivo** | Al tirar, activa un efecto en el objeto. | Entorno |  |
| **F-042** | **Puerta de salida** | Trigger de fin de nivel. Se abre al llegar el jugador. | Entorno | *Comunica con LevelManager.* |
| **▌ ENEMIGOS** |
| **F-043** | **Momia — Patrulla** | Patrulla por rebote a 2 u/s (sin waypoints): camina hasta un borde o pared insuperable y gira. Salta paredes superables. | Enemigos | *MummyAI.cs. PatrollerAI.cs queda como patrullero genérico legacy.* |
| **F-044** | **Momia — Detección** | Radio 5u **+ línea de visión** (Linecast contra maskVisionBloqueo): no detecta a través de paredes/suelo. Al detectar muestra «!» y persigue directo (sin estado ALERTA). | Enemigos | *⚠️ Nuevo: detección por línea de visión.* |
| **F-045** | **Momia — Persecución** | 3.5 u/s hacia el jugador. No respeta bordes (puede caer). Salta paredes superables y si el jugador está más arriba. Abandona al superar radioAbandonar (7u). | Enemigos |  |
| **F-046** | **Momia — Salto** | fuerzaSalto 8, jumpCooldown 0.6s. Supera paredes despejadas por encima de jumpClearanceHeight (1.0u). | Enemigos | *Detección de pared/borde con raycasts.* |
| **F-047** | **Patrullero — Daño** | Contacto = reinicio. Sin hitbox de ataque. Suelta la llave (KeyCarrier) al morir. | Enemigos | *Momia y Espectral.* |
| **F-048** | **Lanzador — Disparo fijo** | Proyectiles en dirección fija cada 2–3s. No apunta al jugador. | Enemigos | *LauncherAI.cs.* |
| **F-049** | **Lanzador — Proyectil** | 8 u/s. Destruido al impactar, al contactar jugador o a los 10s. | Enemigos | *Projectile.cs. 8×8 px.* |
| **F-050** | **Lanzador — Daño** | Contacto con Lanzador o proyectil = reinicio. | Enemigos | *Sin cooldown. Sin combate.* |
| **F-051** | **Golem Fase 1** | Proyectiles de piedra lentos y predecibles. El jugador esquiva con gancho. | Enemigos | *GolemBoss.cs. Arena en Nivel 6.* |
| **F-052** | **Golem Fase 2** | Cadencia aumenta. Ráfagas cortas. Plataformas de la arena comienzan a colapsar. | Enemigos |  |
| **F-053** | **Golem — Condición de victoria** | El jugador alcanza el punto débil del Golem en las ventanas entre ataques. | Enemigos | *Mecánica a cargo de Game Design.* |
| **▌ SISTEMA DE DAÑO Y MUERTE** |
| **F-054** | **Muerte instantánea** | Cualquier contacto con enemigo, proyectil o trampa = reinicio. Sin vidas ni salud. | Sistema | *Muerte por hazard: animación dramática + SpotlightOverlay + pausa (Time.timeScale = 0). Muerte por vacío: reinicio inmediato. Jugador de vuelta **<** 2s.* |
| **F-055** | **Reinicio automático** | Tras la muerte, el nivel se reinicia sin pantalla de game over. | Sistema | *Los cristales ya recogidos **se conservan** (no reaparecen); reinician solo con R o al cambiar de nivel. La música no se reinicia.* |
| **F-056** | **Contador de muertes** | Dos registros: global de la partida (HUD + pantalla de victoria) y por nivel (panel de fin de nivel). | Sistema | *Global = contadorMuertes; por nivel = muertesNivel (se resetea al entrar a un nivel nuevo o Reintentar).* |
| **▌ COLECCIONABLES** |
| **F-057** | **Cristales de puntuación** | Opcionales. Contacto directo los recolecta. No afectan progresión. | Coleccionables | *CrystalPickup.cs.* |
| **F-058** | **Contador HUD de cristales** | Esquina superior: cristales recogidos / total. Siempre visible. | Coleccionables |  |
| **F-059** | **Resumen fin de nivel** | Pantalla breve con tres textos separados: tiempo en nivel (mm:ss), muertes en el nivel y cristales obtenidos / total. | Coleccionables | *txtTiempoPanel / txtMuertesPanel / txtCristalesPanel en GameLoopManager.* |
| **▌ UI Y ESTADOS DEL JUEGO** |
| **F-060** | **Menú Principal** | Jugar, Opciones, Salir. Submenús por pestañas (opciones/controles); ESC cierra el panel abierto. Fade-out antes de cargar escena o salir. Al entrar a un nivel la música cambia con crossfade (no se corta). MenuManager autónomo. | UI | *MenuManager.cs. CanvasGroup fade 0.4s (unscaledDeltaTime). ESC: panelesCerrablesConEsc.* |
| **F-061** | **Pausa** | Tiempo congelado. Reanudar, Reintentar, Opciones (sonido), Controles, Menú Principal. ESC reanuda directo. | UI | *Sin penalización por pausar. Panel de controles (panelControles) entre las pestañas; se cierra solo al reanudar.* |
| **F-062** | **Pantalla de fin de nivel** | Tiempo (mm:ss), muertes del nivel y cristales al completar. Continuar al siguiente. | UI |  |
| **F-063** | **Pantalla de victoria** | Créditos con puntuación total y contador de muertes. | UI | *Fade-out dorado en Nivel 6.* |
| **F-064** | **HUD mínimo** | Solo contador de cristales. Sin barra de salud, minimapa ni cooldowns. | UI |  |
| **F-065** | **Transición de muerte** | SpotlightOverlay con fade-in configurable (default 0.10s). Solo aplica a muerte por hazard. Muerte por vacío: sin transición visual, reinicio directo. | UI |  |
| **F-066** | **Reiniciar nivel (R)** | El jugador puede reiniciar el nivel en cualquier momento. | UI |  |
| **▌ AUDIO** |
| **F-067** | **SFX — Lanzamiento** | 'Whoosh' corto y seco al disparar. | Audio | *Prioridad Alta.* |
| **F-068** | **SFX — Carga del gancho** | Tensión creciente mientras se mantiene click. Escala con carga. | Audio | *Prioridad Alta.* |
| **F-069** | **SFX — Enganche** | 'Clink' metálico al conectar con anclaje. | Audio | *Prioridad Alta.* |
| **F-070** | **SFX — Retracción fallo** | Cuerda que regresa al fallar contra superficie inválida. | Audio | *Prioridad Media.* |
| **F-071** | **SFX — Salto** | 'Pop' suave. | Audio | *Prioridad Alta.* |
| **F-072** | **SFX — Aterrizaje** | Impacto al aterrizar. Varía por superficie. | Audio | *Prioridad Alta.* |
| **F-073** | **SFX — Plataforma frágil** | Crujido progresivo al pisar. Colapso al romperse. | Audio | *Prioridad Alta.* |
| **F-074** | **SFX — Enemigo alerta** | Alarma simple al detectar al jugador. | Audio | *Prioridad Alta.* |
| **F-075** | **SFX — Proyectil** | 'Pew' corto al disparar el Lanzador. | Audio | *Prioridad Alta.* |
| **F-076** | **SFX — Cristal** | 'Ding' brillante al recolectar cristal. | Audio | *Prioridad Media.* |
| **F-077** | **SFX — Muerte** | Sonido decreciente y corto. | Audio | *Prioridad Alta.* |
| **F-078** | **Música — Jungla** | Loop ambiental. Sonidos naturales + percusión suave. Nivel 1. | Audio | *Sin melodía dominante.* |
| **F-079** | **Música — Templo** | Drones oscuros + percusión lenta. Niveles 2–5. | Audio | *Volumen se reduce en alta tensión.* |
| **F-080** | **Música — Jefe Final** | Pieza diferenciada. Más rítmica, mayor intensidad. | Audio |  |
| **▌ SISTEMAS DE EVENTO Y PROGRESIÓN** |
| **F-081** | **Pinchos en secuencia manual** | Cada pincho retráctil suelto puede coordinarse a mano combinando `faseInicial` (0–1) y `delayInicial` (espera oculta) — ambos se combinan. Permite armar cascadas/ondas sin SpikeGroup. | Entorno | *SpikeHazard.cs — antes delayInicial anulaba faseInicial.* |
| **F-082** | **Reliquia disparadora** | Ídolo recolectable por proximidad que dispara un UnityEvent una sola vez al recogerse. Cablea las consecuencias de una escena scriptada. | Eventos | *RelicaPickup.cs. Escena Nivel 5.* |
| **F-083** | **Objeto activable** | Pared/terreno/trampa que se enciende o apaga por evento (Collider2D + Renderer). Estado inicial y animación de aparición configurables. | Eventos | *ObjetoActivable.cs. Activar/Desactivar/Alternar.* |
| **F-084** | **Roca rodante** | Roca estilo Indiana Jones: dormida hasta activarse, cae y rueda horizontalmente. Contacto = reinicio; aplasta a la Momia. | Entorno | *RollingBoulder.cs. Distinta de la Roca Cayente (F-033).* |
| **F-085** | **Cambio de skin del jugador** | Intercambia el RuntimeAnimatorController del jugador a una variante (Override Controller recomendado), conservando animaciones. Tinte opcional. | Eventos | *PlayerSkinSwapper.cs. PlayerItem.controller.* |
| **F-086** | **Selector de niveles con progresión** | Pantalla de selección con desbloqueo por progresión (PlayerPrefs). Botones que intercambian sprite normal/bloqueado. Nivel 1 siempre desbloqueado. | UI | *SelectorNiveles.cs + ProgresoNiveles.cs. Desbloqueo escrito por GameLoopManager.NivelCompleto().* |

**Changelog del Registro**

**Sesión 14***   Jun 2026  —  Persistencia de cristales, fin de nivel, audio centralizado y UI (v5.6)*

**→  **Cristales persistentes (F-055, F-057, Secciones 9.2/9.4): los cristales ya recogidos **no reaparecen al morir** (HashSet por posición en `GameLoopManager`). Reinician solo con R o al cambiar de nivel. Reemplaza el comportamiento anterior ("reaparecen todos"). El checkpoint ya no restaura cristales.

**→  **Panel de fin de nivel con tres datos (F-059, F-062): tiempo (mm:ss), muertes del nivel y cristales, en textos separados (`txtTiempoPanel` / `txtMuertesPanel` / `txtCristalesPanel`).

**→  **Doble contador de muertes (F-056): global de la partida (HUD + victoria) y por nivel (panel de fin de nivel, se resetea al entrar a un nivel nuevo o Reintentar).

**→  **Audio centralizado (F-060, F-078–F-080, Sección 8.3): `AudioManager` pasa a ser autoridad de música por escena (array `musicaPorEscena` por buildIndex, suscrito a sceneLoaded). `PlayMusic` **idempotente** → la música no se reinicia al morir ni entre escenas de la misma zona; crossfade entre zonas. Se configura una sola vez desde el menú. `GameLoopManager` y `MenuManager` ya no gestionan música.

**→  **UI de menús: panel de **controles** integrado a la pausa (referencia en `GameLoopManager`, switching por pestañas en Inspector); **ESC** cierra el panel abierto en el menú principal (`MenuManager.panelesCerrablesConEsc`).

**Sesión 13***   Jun 2026  —  Selector de niveles, sistemas de evento y secuencia de pinchos (v5.5)*

**→  **Selector de niveles con progresión (F-086, Sección 9.5): `SelectorNiveles.cs` + `ProgresoNiveles.cs` (PlayerPrefs). Desbloqueo escrito por `GameLoopManager.NivelCompleto()`. Botones que intercambian sprite normal/bloqueado. Nivel 1 siempre desbloqueado (`siempreDesbloqueado` + garantía en ProgresoNiveles).

**→  **Sistemas de evento / escenas scriptadas (F-082–F-085, Sección 4.6): `RelicaPickup` (disparador por UnityEvent), `ObjetoActivable` (encender/apagar paredes-trampas), `RollingBoulder` (roca rodante Indiana Jones), `PlayerSkinSwapper` (cambio de skin por Override Controller). Para la escena del Nivel 5. Reemplazan al `SecuenciaEventos` (orquestador temporizado) que fue retirado.

**→  **Pinchos en secuencia manual (F-081, F-030, Sección 4.2): `faseInicial` y `delayInicial` ahora se combinan en `SpikeHazard.cs` (antes el delay anulaba la fase). Permite armar cascadas con pinchos sueltos sin SpikeGroup.

**→  **`PlayerItem.controller`: controller de variante del jugador con los clips `ITEM_*`. Se le agregaron los 7 parámetros del Animator del Player (IsRunning, IsGrounded, IsHanging, VelocityY, IsClimbing, Death, IsDead).

**Sesión 12***   Jun 2026  —  Auditoría de sincronización código↔GDD (v5.4)*

**→  **Momia migrada a `MummyAI.cs` (F-043–F-047): patrulla por rebote sin waypoints, salto de paredes superables, persecución sin respetar bordes. El `PatrollerAI.cs` con waypoints queda como patrullero genérico legacy.

**→  **Detección por línea de visión en la Momia (F-044): Linecast contra `maskVisionBloqueo` — ya no detecta al jugador a través de paredes ni del suelo. Gizmo de línea de visión (verde/rojo) en runtime.

**→  **Guerrero Espectral (5.2, 15.4): corregido "elipse dinámica" → órbita orgánica (tangencial + resorte radial). Parámetros reales documentados: radio detección 6u, abandono 8.5u, orbitRadius 3u, dash 18u/s · 8u, cooldown 2s, dashTriggerRange 3.5u, quietud 1.5s, windup 0.35s.

**→  **`LauncherAI`, `Projectile`, `KeyItem`, `KeyDoor`, `KeyCarrier` marcados como ✅ Implementados (estaban como 📋 Planeado). Añadidos a 11.1: `Lever`, `CameraZone`, `ParallaxBackground`, `LevelExit`, `SpotlightOverlay`, `IHookable`.

**→  **Sección 15.2 (Gancho) reescrita con valores reales del build: carga máx 1.5s, alcance 3–10u, snap 0.4u, launchSpeed 20, hookGravity 18, retractSpeed 25, climbSpeed 6, minRopeLength 1, etc. (antes 0.1–1s / 5–15u / 1.5u).

**→  **Sección 15.1: `groundNormalThreshold` corregido a 0.5 (era 0.7). Añadidos swingForce 15, ground check, deathAnimDuration. "Input buffer 0.15s" corregido — es `jumpQueued` sin expirar hasta aterrizar (F-005).

**→  **Sección 15.3 ampliada: fase "Asomando" de pinchos (0.4s sin daño), parámetros de ReactiveWall (distanciaDerribo 1.5u, 90°/0.4s) y breakDelay de BreakableAnchor (0.6s). Cadencia del Lanzador precisada a 2.5s.

**→  **Discrepancia de archivo: el CLAUDE.md apunta a `GDD_TemploUtaki_v4.5.md`; el archivo real es `Docs/GDD_TemploUtaki_v5.md`.

**Sesión 11***   Jun 2026*

**→  **BreakableAnchor implementado (F-040 actualizado): anclaje destructible activado por el gancho, tres fases de advertencia, retracción automática, opción permanentBreak/regeneración.

**→  **ReactiveWall actualizado (F-036): la pared al terminar la caída queda como puente sólido permanente (RigidbodyType2D.Static).

**→  **Pinchos retráctiles actualizados (F-030): dirección configurable (DireccionSalida enum) y modo de ciclo de grupo (ModoCiclo: Sincronizado / Desfasado / Secuencial).

**→  **KeyDoor actualizado: animación de apertura de 5 frames con desplazamiento ascendente. Collider desactivado inmediatamente.

**→  **MenuManager implementado (F-060 actualizado): autónomo, fade-out 0.4s, StopMusic en todas las transiciones.

**→  **SpectralPatrollerAI corregido: isTrigger forzado en Awake para garantizar detección de daño independiente de la config del Inspector.

**→  **Sección 15.3 ampliada: parámetros de dirección y ModoCiclo de pinchos + parámetros de BreakableAnchor.

**Prototipo***   Mar–Abr 2026*

**→  **Registro inicial de todas las features.

**→  **F-008: bug OnCollisionExit2D resuelto — reemplazado por Physics2D.OverlapBox.

**→  **F-001: Transform.Translate reemplazado por rb.linearVelocity en FixedUpdate.

**→  **F-012 / F-013: Sistema de carga del gancho agregado. Placeholder: barra. Alcance 5u–15u, carga 0.1s–1s.

**→  **F-016: Retracción por superficie inválida especificada.

**→  **F-015: isGrappling se activa prematuramente — WIP.

**→  **F-017: GrappleSwing usa AddForce — pendiente corregir.

**→  **F-014: El gancho viaja recto en lugar de arco — bug conocido.

**→  **F-020: Curva Bezier de la cuerda ausente en Prototipo.

**Alpha***   — (próximo milestone)*

**→  **Todas las features marcadas como Planeado pasan a implementación.

**→  **Corrección de bugs pendientes en GrappleScript.cs y PlayerMovement.cs.

**→  **Implementación de coyote time (F-004) e input buffer (F-005).

**→  **Integración de todos los SFX prioritarios.

**→  **Mecánicas de Niveles 2–6 completas al 100%.

**15.  ****PARÁMETROS TÉCNICOS**

Esta sección es la fuente de verdad para todos los valores numéricos del juego. Los parámetros han sido sincronizados con el código del proyecto (build main, 2026-06) tras auditoría técnica — el código tiene prioridad sobre versiones anteriores del GDD para valores numéricos.

**ℹ  ***Los valores marcados como 'Sujeto a cambio' son provisorios y se ajustan durante Alpha según testing de feel.*

**15.1 Movimiento del Jugador**

| **Parámetro** | **Valor implementado** | **Variable en código** | **Script** |
| --- | --- | --- | --- |
| Velocidad de carrera | 9 u/s | moveSpeed = 9f | PlayerMovement.cs |
| Aceleración en aire | 35 u/s² | airAccel = 35f | PlayerMovement.cs |
| Desaceleración en aire | 20 u/s² | airDecel = 20f | PlayerMovement.cs |
| Velocidad salto vertical | 12 u/s | jumpForce = 12f | PlayerMovement.cs |
| Jump Cut multiplicador | ×0.5 | jumpCutMult = 0.5f | PlayerMovement.cs |
| Coyote Time | 0.12s | coyoteTime = 0.12f | PlayerMovement.cs |
| Escala de gravedad — subida | 2.0 | riseGravityScale = 2.0f | PlayerMovement.cs |
| Escala de gravedad — caída | 3.5 | fallGravityScale = 3.5f | PlayerMovement.cs |
| Velocidad máxima de caída | -20 u/s | maxFallSpeed = -20f | PlayerMovement.cs — ⚠️ Auditado |
| Escala de gravedad — swing | 2.5 | swingGravityScale = 2.5f | PlayerMovement.cs |
| Fuerza de swing | 15 | swingForce = 15f | PlayerMovement.cs |
| Ground Normal Threshold | 0.5 (≈60°) | groundNormalThreshold = 0.5f | PlayerMovement.cs — ⚠️ Auditado (GDD previo decía 0.7) |
| Ancho ground check | 0.45 u | groundCheckWidth = 0.45f | PlayerMovement.cs |
| Alto ground check | 0.05 u | groundCheckHeight = 0.05f | PlayerMovement.cs |
| Duración anim. muerte | 1.5s | deathAnimDuration = 1.5f | PlayerMovement.cs |
| CapsuleCollider | 0.5×1 u | — | (prefab Inspector) |

**Salto en cola (jumpQueued):** el GDD anterior describía un "input buffer de 0.15s". El código real **no** usa una ventana temporizada: `jumpQueued` se anota al presionar Espacio (o si ya estaba presionado al soltar otra acción) y **se mantiene hasta aterrizar sin expirar**. En la práctica funciona como un buffer permanente hasta tocar suelo, no como un buffer de 0.15s. Ver Glosario y F-005.

**15.2 Sistema del Gancho**

Sincronizado con `GrappleScript.cs` (build main). El GDD previo (carga 0.1–1.0s, alcance 5–15u, snap 1.5u) quedó obsoleto — estos son los valores reales.

| **Parámetro** | **Valor** | **Variable** | **Notas** |
| --- | --- | --- | --- |
| Tiempo máx. de carga | 1.5s | maxChargeTime = 1.5f | Escala lineal del alcance. Sujeto a cambio en Alpha. |
| Alcance mínimo | 3 u | minGrappleDistance = 3f | Carga mínima. |
| Alcance máximo | 10 u | maxGrappleDistance = 10f | Carga máxima. |
| Radio de snap | 0.4 u | snapRadius = 0.4f | Asistencia de enganche al punto válido más cercano. |
| Velocidad de lanzamiento | 20 u/s | launchSpeed = 20f | Velocidad del gancho en vuelo. |
| Gravedad del gancho en vuelo | 18 | hookGravity = 18f | Hace que el gancho describa un arco. |
| Tiempo máx. de vuelo | 0.6s | maxFlightTime = 0.6f | Si no engancha, se retrae. |
| Velocidad de retracción | 25 u/s | retractSpeed = 25f | Retracción normal al soltar. |
| Velocidad de retracción por fallo | 8 u/s | failRetractSpeed = 8f | Retracción tras impacto inválido. |
| Cooldown por fallo | 0.3s | failCooldown = 0.3f | Solo en fallo; soltar voluntario no tiene cooldown. |
| Velocidad máx. de swing | 15 u/s | maxSwingVelocity = 15f | Tope de velocidad en el péndulo. |
| Amortiguación de swing | 0.02 | swingDamping = 0.02f | Pérdida sutil de energía del péndulo. |
| Velocidad de ajuste de soga (W/S) | 6 u/s | climbSpeed = 6f | Acortar (W) / alargar (S). |
| Longitud mínima de soga | 1 u | minRopeLength = 1f | Límite al acortar con W. La máxima es la longitud al momento de enganchar (≤ alcance). |
| Fuerza de retracción de objetos | 15 | grabForce = 15f | Para tirar de objetos Hookeables. |
| Segmentos LineRenderer | 20 | segments = 20 | Curva Bezier cuadrática. |
| Ancho de cuerda | 0.15 | ropeWidth = 0.15f | LineRenderer. |
| Velocidad de enderezado de cuerda | 5 | straightenSpeed = 5f | Al tocar piso, la línea pasa de Bezier a recta. |

**15.3 Plataformas**

| **Plataforma / Parámetro** | **Valor** | **Notas** |
| --- | --- | --- |
| Estática — fricción | 0.6 | Control adecuado sin resbalar. |
| Móvil — velocidad | 2 u/s (default) | speed en MovingPlatform.cs. Recorrido por moveOffset (default 3,0) con Lerp lineal y rebote. |
| Frágil — timer de rotura | 1.2s | breakDelay en FragilePlatform.cs (dividido en 2 fases iguales). |
| Frágil — duración rompiéndose | 0.4s | breakingAnimDuration: sprite "Rompiéndose" antes de desaparecer. |
| Frágil — fases de advertencia | 3 | Progresivas (color + grietas) hasta la rotura. |
| Frágil — tiempo de regeneración | 5s | regenDelay. Después de romperse completamente. |
| Pinchos retráctiles — fase retraído | 1.5s | tiempoRetraido. Configurable por nivel. |
| Pinchos retráctiles — fase asomando | 0.4s | tiempoAsomando. Fase intermedia SIN daño (telegrafío de salida). |
| Pinchos retráctiles — fase desplegado | 1.5s | tiempoExtendido. Única fase con daño. |
| Pinchos retráctiles — dirección | Arriba (default) | Enum DireccionSalida: Arriba / Abajo / Izquierda / Derecha. Inspector en SpikeHazard.cs. |
| Pinchos retráctiles — modo de ciclo | Desfasado (default) | Enum ModoCiclo en SpikeGroup.cs: Sincronizado (todos juntos) / Desfasado (fase distribuida automáticamente) / Secuencial (con delay entre spikes). |
| Pinchos retráctiles — delay secuencial | 0.15s | delayEntreSpikes en SpikeGroup.cs. Solo aplica en modo Secuencial. |
| Pared reactiva — distancia de derribo | 1.5 u | distanciaDerribo en ReactiveWall.cs. Al superarla tirando con el gancho, cae. |
| Pared reactiva — caída | 90° / 0.4s | angulosCaida / duracionCaida. Al terminar pasa a RigidbodyType2D.Static (puente). |
| BreakableAnchor — delay de rotura | 0.6s | breakDelay desde que se engancha (OnHooked). |
| BreakableAnchor — duración rompiéndose | 0.4s | breakingAnimDuration. |
| BreakableAnchor — fases de advertencia | 3 | Igual que Plataforma Frágil: cambio de color progresivo. |
| BreakableAnchor — regeneración | 5s (default) | regenDelay configurable en Inspector. Sin efecto si permanentBreak = true. |

**15.4 Enemigos**

**Guerrero Espectral (`SpectralPatrollerAI.cs`)**

No persigue en línea recta: orbita orgánicamente y ataca con dash cuando el jugador se queda quieto.

| **Parámetro** | **Valor** | **Variable** | **Notas** |
| --- | --- | --- | --- |
| Velocidad — patrulla | 2 u/s | velocidadPatrulla | Entre waypoints A/B. También usada en Regreso. |
| Pausa en extremos | 0.6s | duracionIdle | En cada extremo de la ruta. |
| Radio de detección | 6 u | radioDeteccion | Chequeo cada frame. |
| Radio de abandono | 8.5 u | radioAbandonar | Al superarlo en órbita, regresa. |
| Radio de órbita | 3 u | orbitRadius | Distancia mínima que mantiene (± flotación). |
| Velocidad angular órbita | 1.5 rad/s | orbitVelocity | Componente tangencial. |
| Acercamiento radial | 0.6 | approachSpeed | Resorte que cierra distancia de a poco. |
| Firmeza radial | 1.5 | radialStiffness | Corrección hacia la distancia mínima. |
| Flotación radio | 0.4 u @ 1.2 | floatAmplitude / floatSpeed | Variación sinusoidal del radio. |
| Velocidad de dash | 18 u/s | dashSpeed | Línea recta hacia el jugador. |
| Distancia de dash | 8 u | dashDistance | Incluye overshoot. |
| Rango para iniciar dash | 3.5 u | dashTriggerRange | Debe estar a tiro para atacar. |
| Cooldown de dash | 2s | cooldown | Entre ataques. |
| Quietud para atacar | 1.5s (< 0.5 u/s) | playerStillTime / playerStillThreshold | El jugador debe quedarse quieto. |
| Windup (telegrafío) | 0.35s | (EnterWindup) | Parpadeo blanco antes del dash. |
| Recover | 0.4s | (EnterRecover) | Pausa tras el dash. |
| Traversal | Atraviesa todo salvo SpectralWall | spectralWallMask | Paredes doradas/rúnicas: señalización visible. Detiene el dash. |
| Eliminación — fuego | Muerte instantánea | (HazardFire) | Única trampa que lo elimina. Tag SpectralEnemy. |
| Eliminación — otras trampas | Sin efecto | — | Pinchos, roca, vacío no lo afectan. |
| Duración anim. muerte | 0.5s | deathAnimDuration | Antes de destruir el objeto. |

**Momia del Templo (`MummyAI.cs`)**

Patrullero físico por rebote (sin waypoints) con salto y detección por línea de visión.

| **Parámetro** | **Valor** | **Variable** | **Notas** |
| --- | --- | --- | --- |
| Velocidad — patrulla | 2 u/s | velocidadPatrulla | Por rebote. Respeta bordes y paredes. |
| Velocidad — persecución | 3.5 u/s | velocidadPersecucion | No respeta bordes (puede caer). |
| Pausa al girar | 0.4s | duracionIdle | Al rebotar en borde o pared. |
| Radio de detección | 5 u | radioDeteccion | Requiere además línea de visión. |
| Radio de abandono | 7 u | radioAbandonar | Valor absoluto (no ×1.5). Al superarlo vuelve a patrullar. |
| Línea de visión | Linecast | maskVisionBloqueo | No detecta a través de paredes/suelo. Si está vacío usa maskSuelo. |
| Fuerza de salto | 8 | fuerzaSalto | Salta paredes superables. |
| Cooldown de salto | 0.6s | jumpCooldown | Entre saltos consecutivos. |
| Altura de despeje (salto) | 1.0 u | jumpClearanceHeight | Si la pared sigue a esta altura, no la salta (gira). |
| Alcance raycast de pared | 0.4 u | wallCheckDistance | Detección horizontal de pared. |
| Salto en persecución | +1.2 u | alturaSaltoPersecucion | Salta si el jugador está más arriba. |
| Zona muerta horizontal | 0.3 u | zonaMuertaX | Evita flip-flop si el jugador está justo encima. |
| Detección de borde | 0.35 / 0.45 / 0.4 u | edgeCheckOffsetX/Y, edgeCheckDistance | Raycast hacia abajo adelante. |
| Traversal | Mismas limitaciones que el jugador | — | No atraviesa paredes. Cae si el suelo bajo sus pies desaparece (frágil colapsada o tracción arrastrada con gancho). |
| Duración anim. muerte | 1.2s | deathAnimDuration | Antes de destruir el objeto. |
| Eliminación — fuego | Muerte instantánea |  |  |
| Eliminación — pinchos | Muerte instantánea | Estáticos o retráctiles en estado activo. |
| Eliminación — roca cayente | Muerte instantánea | Si la roca impacta sobre ella. |
| Eliminación — vacío | Muerte instantánea | Si cae al void. |
| Eliminación — proyectil Lanzador | Muerte instantánea | Friendly fire ambiental. |
| Eliminación — plataforma frágil | Sin efecto (rompe la plataforma) | Su peso colapsa la plataforma pero no la mata. |

**Lanzador de Proyectiles**

| **Parámetro** | **Valor** | **Notas** |
| --- | --- | --- |
| Posición | Fija — no se mueve | Definida en el nivel. |
| Cadencia de disparo | 2.5s (default) | cadencia en LauncherAI.cs. Configurable por nivel. |
| Dirección de disparo | Fija — definida en nivel | direccionDisparo (default derecha). No apunta al jugador. |
| Velocidad del proyectil | 8 u/s | velocidad en Projectile.cs. Constante durante todo el vuelo. |
| Vida del proyectil | 10s | tiempoVida. Se destruye también al salir de pantalla. |
| Daño — jugador | Reinicio inmediato | Contacto = muerte. |
| Daño — Momia del Templo | Muerte instantánea | Friendly fire ambiental. |
| Daño — Guerrero Espectral | Sin efecto | El proyectil lo atraviesa. |

**Golem de Piedra — Jefe Final**

| **Parámetro** | **Descripción** |
| --- | --- |
| Tipo de combate | Guardián estático. Dos fases con patrones diferenciados. |
| Tamaño sprite | 48×64 px mínimo. |
| Fases | 2. Transición a Fase 2 por condición a definir por Game Design. |
| Cadencia Fase 1 | Lenta. El jugador tiene tiempo de leer los patrones. |
| Cadencia Fase 2 | Aumenta. Agrega ráfagas cortas. |
| Daño al jugador | Contacto con Golem o proyectiles = reinicio de la arena. |
| Condición de victoria | Alcanzar punto débil en ventanas entre ataques. A definir por Game Design. |

**15.8 Sistema de Llaves**

| **Parámetro** | **Valor** | **Notas** |
| --- | --- | --- |
| Relación llave-puerta | 1 a 1 | Configurable desde el Inspector de Unity. |
| Llaves activas por nivel | Sin límite fijo | Game Design define la cantidad. |
| Física de la llave | Activa | Cae con gravedad, rebota en superficies. |
| Recolección — proximidad | Al pasar sobre ella | Sin input adicional. |
| Recolección — gancho | Enganchar + S para retraer | La llave es un objeto grappleable. |
| Efecto en mano — gancho | Deshabilitado | Mientras el jugador sostiene la llave. |
| Efecto en mano — movimiento | Sin penalización | Corre y salta con normalidad. |
| Lanzamiento — sistema de carga | Igual que el gancho | Click izquierdo + cursor. Misma barra de carga. |
| Lanzamiento — alcance | Igual que el gancho | Proporcional al tiempo de carga. |
| Apertura de puerta | Por proximidad sosteniendo la llave vinculada | No requiere lanzar la llave a la cerradura. |
| Llave en void | Reaparece en posición original | Si cae al vacío. |
| Al reiniciar nivel | Reaparece en posición original | Puertas vuelven a cerrarse. |
| Llave dentro de enemigo | Cae al suelo con física al morir el enemigo | Tanto Espectral como Momia pueden portarlas. |

**15.5 Sistema de Daño y Reinicio**

| **Parámetro** | **Valor** | **Notas** |
| --- | --- | --- |
| Sistema de salud | Ninguno | No hay barra de salud ni vidas. |
| Consecuencia de daño | Reinicio inmediato del nivel | Cualquier contacto con enemigo, proyectil o trampa. |
| Duración fade-out de muerte | < 0.5s | Transición visual antes del reinicio. |
| Tiempo de vuelta al nivel | < 2s | Desde la muerte hasta estar jugando de nuevo. |
| Cristales al morir | Se conservan | Los ya recogidos no reaparecen; el contador se mantiene. |
| Cristales al reiniciar (R) / cambiar nivel | Reaparecen todos | El contador vuelve a cero. |
| Contador de muertes | Global de la partida (HUD + victoria) + por nivel (panel de fin de nivel) | El global es acumulativo; el por nivel se resetea al entrar a un nivel nuevo o Reintentar. |

**15.6 Audio — Prioridades**

| **Evento** | **Descripción del SFX** | **Prioridad** |
| --- | --- | --- |
| Carga del gancho | Tensión creciente mientras se mantiene click. Escala con la carga. | Alta |
| Lanzamiento de gancho | 'Whoosh' corto y seco. | Alta |
| Enganche exitoso | 'Clink' metálico. | Alta |
| Retracción — fallo | Cuerda que regresa al fallar. | Media |
| Salto | 'Pop' suave. | Alta |
| Aterrizaje — piedra | Impacto seco y grave. | Alta |
| Plataforma frágil — crujido | Crujido progresivo al pisar. | Alta |
| Plataforma frágil — rotura | Colapso de material. | Alta |
| Enemigo en alerta | Alarma simple. | Alta |
| Proyectil disparado | 'Pew' corto. | Alta |
| Recolectar cristal | 'Ding' brillante. | Media |
| Muerte del jugador | Sonido decreciente, corto. | Alta |
| Golem — ataque | Impacto de piedra masivo. | Alta |
| Música — Jungla | Loop ambiental. Sonidos naturales + percusión suave. | Media |
| Música — Templo | Drones oscuros + percusión lenta. Baja en alta tensión. | Media |
| Música — Jefe Final | Pieza diferenciada. Más rítmica, mayor intensidad. | Alta |

**15.7 Arte — Especificaciones**

**Animaciones del Personaje**

| **Estado** | **Frames** | **Descripción** |
| --- | --- | --- |
| Idle | 2–4 | Respiración sutil o parpadeo. Loop. |
| Run | 6–8 | Ciclo de carrera ágil. Loop. |
| Jump | 3–4 | Despegue → apex. |
| Fall | 2–3 | Caída, cuerpo inclinado hacia abajo. |
| Grapple charge | 2–3 | Brazo extendido, tensión visible. Loop mientras carga. |
| Grapple launch | 2–3 | Brazo extendido hacia el cursor al soltar. |
| Grapple swing | 4–6 | Cuerpo colgante en péndulo. Loop. |
| Climbing | — | Activo al subir o bajar la cuerda con W/S mientras está enganchado. Transición desde Grapple swing; vuelve a Grapple swing al soltar el input. IsClimbing = true en GrappleScript. |
| Death | — | Animación de muerte dramática. Se reproduce en UnscaledTime durante la pausa del juego. Duración configurable desde el Inspector (Death Anim Duration, default 1.5s). Sin transiciones de salida — la escena recarga al finalizar. Solo aplica a muerte por hazard; muerte por vacío no la activa. |

**Sprites — Enemigos y Jefe Final**

| **Sprite** | **Tamaño** | **Paleta** | **Animaciones clave** |
| --- | --- | --- | --- |
| Patrullero | 16×24 px | Grises de piedra, detalles dorado oxidado | Idle 2–4f · walk 6–8f · alert 2f · chase 6–8f |
| Ícono de alerta | 8×8 px | Amarillo | — |
| Lanzador | 16×16 px | Verdes llamativos y rojos | Carga 4–6f · disparo 2f |
| Proyectil (Lanzador) | 8×8 px | Distinguible del fondo | Loop 2–4f, rotación o pulso |
| Golem — idle | 48×64 px mín. | Roca oscura, musgo, runas doradas | 4–6f, respiración pesada |
| Golem — ataque | 48×64 px mín. | Punto débil destacado | 8–10f |
| Golem — Fase 2 | 48×64 px mín. | Crack visible, ojos brillantes | — |
| Golem — muerte | 48×64 px mín. | — | 10–12f, colapso de piedra |

*Demonic Arts Company  ·  Templo Utaki  ·  GDD v5.6  ·  Junio 2026  ·  Confidencial — Uso interno del equipo*