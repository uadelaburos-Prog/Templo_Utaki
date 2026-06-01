# Técnicas de Level Design

Recopilación de conceptos de cursada. Fuentes: Atención Indirecta · Celeste · Formas de Guiar · Rastreo Mecánico · Silksong · Desvelado · Filosofía Nintendo · Super Mario 3D World · BattleBlock Theater.

---

## 01 — Atención Indirecta

El objetivo es generar el loop *curiosidad → búsqueda → pérdida → descubrimiento* sin guiar al jugador de forma explícita. Las estrategias de atención indirecta dirigen la mirada de manera orgánica, sin señalización directa.

**Forma Extraña**
Un elemento que no repite ni comparte características formales con los que lo rodean. No necesita estar fuera de contexto narrativo — simplemente rompe el patrón visual del nivel.

**Aislamiento**
El espacio vacío alrededor de un elemento lo despega visualmente del conjunto. El espacio negativo es activo: su diseño es tan importante como el de los objetos que rodea.

**Diferencia por Altura**
Cuando la forma y el aislamiento no funcionan, la distinción vertical impone jerarquía. Elementos más altos o más bajos que el conjunto generan atención de manera natural.

**Dinámico vs. Estático**
En un escenario mayormente quieto, lo que se mueve se ve primero. En uno muy animado, lo que permanece quieto destaca. La misma lógica funciona en los dos sentidos.

**Contraste Visual**
Un elemento que altera la identidad visual dominante del nivel — en color, trazo, material o estilo — rompe el ritmo perceptivo y genera curiosidad antes de que el jugador lo procese conscientemente.

**Repetición y Diferencia**
La repetición de un asset es herramienta de camuflaje y de guía al mismo tiempo. Múltiples elementos similares obligan a prestar atención al detalle; la variación dentro del patrón llama la atención.

**Posición en Pantalla**
El centro, los ejes y las esquinas imponen jerarquía visual por defecto. La composición del nivel puede generar nuevas jerarquías construidas desde vacíos, tamaño y límites del escenario.

**Nivel de Detalle**
Un objeto con mayor resolución visual o complejidad que su entorno genera jerarquía perceptiva. El ojo va naturalmente a donde hay más información.

**Atención Dispersa**
Múltiples llamados de atención simultáneos crean confusión productiva. El jugador no sabe adónde ir primero, lo que fomenta la exploración activa en lugar del seguimiento de un camino único.

---

## 02 — Formas de Guiar al Jugador

Herramientas visuales específicas del plataformero (estudiadas en Celeste) para orientar al jugador sin texto. Cada elemento del nivel puede cumplir una función de guía además de su función de gameplay.

**Estado de los Assets**
El estado visible de un elemento del escenario comunica información. Al comparar un farol destruido con uno iluminado, el juego motiva a ir hacia el iluminado — permite imaginar un camino posible sin decirlo explícitamente.

**Diseño de Límites**
Paredes, techos y pisos no solo funcionan como obstáculos — también pueden usarse para guiar al jugador hacia determinados puntos. La forma del límite dirige el movimiento antes de que el jugador lo decida conscientemente.

**Trabajo de Background**
El diseño del background agrega profundidad y una capa de contraste que permite reconocer formas y posiciones. Puede subrayar la posición de un objetivo o ayudar a entender la posición final del jugador después de ejecutar una acción.

**Contraste de Colores y Materiales**
Cada juego define una paleta y estética que se constituyen y asocian a significados. El contraste dentro de esa paleta construye jerarquía — destaca la posición inicial, el desafío y la posición final dentro de una misma pantalla.

**Acción Sugerida vs. Camino Alternativo**
Celeste combina recursos para sugerir una acción o camino sin hacerlo obligatorio: posición de la mecánica en pantalla, ubicación de elementos, diseño del espacio, grandes vacíos y peligros evidentes. El camino sugerido requiere menos habilidad y peligro — el alternativo exige más.

**Rutas Obviamente Imposibles**
Existen momentos donde se necesita desalentar determinadas acciones para evitar intentos frustrados. Hacer un camino visualmente imposible disuade sin necesidad de pared invisible ni texto de advertencia.

---

## 03 — Espacio y Composición

El diseño del espacio — tanto de lo que está como de lo que no está — es la herramienta compositiva más fundamental. Afecta el ritmo, la legibilidad del nivel y cómo el jugador construye su plan antes de ejecutarlo.

**Espacio Negativo**
El vacío entre objetos es tan activo como los objetos mismos. A escala micro, aísla elementos y los hace legibles. A escala macro, construye líneas guía invisibles y regula el ritmo de lectura del nivel.

La inversión también es una técnica: la sobrecarga de información o el exceso de vacío pueden usarse como camuflaje deliberado para escalar la dificultad.

**Líneas Guía Tácitas**
Los assets del entorno construyen líneas físicas o tácitas que dirigen la mirada y anticipan el recorrido. Pueden ser filas de objetos, bordes de plataformas, caminos de espacio vacío, o la dirección implícita de la geometría del nivel.

**Jerarquía de Atención**
Al establecer jerarquías visuales claras, le damos al jugador referencias para iniciar su recorrido y, en caso de perderse, reorganizar su mirada y planificar su siguiente acción. Es el punto de arranque de cada nuevo espacio.

**Distribución Equilibrada**
Los puntos de interés distribuidos de forma pareja en el espacio maximizan el área jugable, evitan zonas muertas y nutren el loop de sensaciones con una frecuencia sostenida. Una distribución desequilibrada genera pérdidas extensas de ritmo.

---

## 04 — Progresión Mecánica

Cómo se introduce, desarrolla y escala una mecánica a lo largo de un área. Estudiado a través del rastreo de trampolines y plataformas móviles en Celeste, y de la progresión de enemigos en Silksong. El patrón es siempre el mismo — lo que cambia es la mecánica.

| Etapa | Qué hace |
|---|---|
| 1ª Introducción | No obligatoria para avanzar. Enseña el comportamiento básico con feedback visual y sonoro. Sin peligro de morir. La mecánica se presenta en un espacio seguro y amplio. |
| 2ª Introducción | Refuerza el aprendizaje con una variación leve. Puede seguir siendo opcional. Se incorpora una dimensión nueva (vertical si la primera fue horizontal, o movimiento si la primera fue estática). |
| 1er Desarrollo | Expande posibilidades: combinación de 2 instancias de la mecánica, mayor habilidad requerida, peligro de morir empieza a aparecer. Requiere movimiento en el aire o coordinación más precisa. |
| 2do Desarrollo | La mecánica ya no ayuda — puede convertirse en obstáculo. Se reduce el espacio seguro. La combinación con otras mecánicas es necesaria. Penalización más presente. |
| Descanso | Valle deliberado de dificultad. La mecánica vuelve a ser opcional o se presenta en versión más accesible. Prepara al jugador para el desafío final. No es una regresión — es pausa narrativa. |
| Skill Gate | Por primera vez la mecánica es obligatoria en el camino principal. Concentra todo lo aprendido. Puede ir acompañado de caminos secundarios para reducir la frustración. |

**Patrón de diseño:** situaciones similares con leves diferencias para generar variedad jugable y evitar la ansiedad de agregar cada vez más mecánicas nuevas. Comparar rooms contiguas con los mismos elementos en pequeñas variaciones es un ejercicio de diseño válido — no es pereza creativa, es refuerzo de aprendizaje.

**Caminos secundarios en los desarrollos:** en los momentos de mayor dificultad, siempre hay opciones secundarias para reducir la sensación de frustración. Las conclusiones del funcionamiento mecánico son propias de cada jugador — cada uno aprende a su ritmo.

---

## 05 — Kishōtenketsu — Filosofía Nintendo

Nintendo reinterpretó la estructura de poemas chinos de 4 versos para diseñar niveles sin tutoriales de texto. Cada parte es autoconclusiva pero depende de las demás para funcionar. El entorno enseña — el texto no.

**1 — Ki: Enseño la Mecánica** *(Espacio seguro · penalización compensada · casi sin riesgo)*
El jugador experimenta la mecánica en un ambiente controlado. La penalización se compensa: elementos que impiden la muerte o recuperan posición rápidamente, evitando la frustración mientras el jugador aprende a su tiempo las posibilidades y limitaciones de la mecánica.

**2 — Shō: Desarrollo** *(Mayor peligro · precisión requerida · internalización)*
Se generan situaciones de mayor peligro. El jugador debe internalizar la mecánica y actuar con mayor precisión. Se elimina el suelo seguro, aumentan los enemigos o se reduce el margen de error.

**3 — Ten: Vuelta de Rosca** *(Misma mecánica · contexto invertido o fragmentado)*
No cambia la mecánica — funciona exactamente igual. Lo que cambia es el contexto: la disposición se fragmenta, se organiza de manera distinta, se invierte el desafío. Obliga a pensar nuevas formas de resolver algo ya conocido.

**4 — Ketsu: Conclusión** *(Síntesis · fusión de situaciones ya superadas)*
Se combinan todas las situaciones ya superadas. No amplía los límites de la mecánica — fusiona varias situaciones conocidas en una secuencia. La Etapa X de SM3D World combina Mecánica A + B en una sola sección.

**Superposición de Estructuras + Mecánica B + Transiciones**
En la mayoría de los niveles de Nintendo existe una superposición de varias estructuras de 4 pasos, cada una asociada a una mecánica diferente. La Mecánica B interrumpe la principal y funciona tanto como descanso como para incorporar variedad.

Las transiciones entre etapas funcionan como signos de puntuación: articulan dos fragmentos, construyen el ritmo y camuflan la progresión para que el nivel no se sienta formulaico.

> Conceptos complementarios: Repetición de patrones con variación de forma · Ritmo intercalado · Penalización compensada · Refuerzo de aprendizaje · Dificultad no ascendente (tiene picos y valles deliberados).

---

## 06 — Conceptos Clave de Celeste

Celeste es una historia hecha de pequeñas historias: cada área tiene su propio arco narrativo, estética y ritmo de juego. Cada pantalla plantea una situación particular. Al pensar cada área y cada nivel como fragmentos autoconclusivos, el diseño se vuelve más manejable y más preciso.

**Bifurcación de Áreas**
Más allá de que el final de cada área sea único, hay varios momentos donde el nivel se bifurca y el jugador puede elegir un camino u otro. Algunas bifurcaciones llevan a coleccionables; otras son puramente exploratorias. Fomentan la exploración voluntaria.

**Recorrido Mixto**
En casi ningún momento el recorrido del personaje es puramente horizontal o puramente vertical. El escenario se atraviesa siempre en ambos ejes, aprovechando al máximo sus dimensiones. Se crean situaciones que obligan a mirar y atravesar todo el espacio.

**Posición de Entradas y Salidas**
Las entradas y salidas del nivel se posicionan en lados opuestos y varían de nivel en nivel para forzar el aprovechamiento de todo el espacio. Hay casos donde el nivel tiene múltiples salidas distribuidas por toda la pantalla.

**Secuencias Flexibles**
Algunos niveles obligan a realizar una secuencia de movimientos específica pero flexible: hay un orden posible, pero no uno único. Esta mezcla entre flexibilidad y especificidad otorga libertad y permite un grado alto de creatividad en el gameplay.

**Múltiples Soluciones**
Los escenarios aceptan distintas aproximaciones para resolver un problema, teniendo en cuenta todas las mecánicas disponibles. El diseño es permisivo con la expresividad del jugador — la secuencia de uso de mecánicas puede variar.

**Fragmentación en Pequeños Niveles**
Las áreas se dividen en múltiples pequeños fragmentos que posibilitan construir retos específicos y progresivos. La frustración se reduce y la pérdida es casi nula — morir en un fragmento no manda al inicio del área.

**Seguridad Determina el Ritmo**
En la mayoría de los niveles hay espacios seguros que determinan el ritmo de juego. A veces queda a criterio del jugador usarlos; en otros momentos se alternan deliberadamente con situaciones de gran dificultad para generar pequeñas pausas. Las paredes son espacios seguros intermedios; los pisos son el espacio más seguro del juego.

**Enseñanza No Textual**
Las mecánicas se enseñan a través del diseño de niveles y en ocasiones con carteles o dibujos en el escenario. El texto se usa solo en el prólogo. Los carteles en el escenario dan información del entorno o sugieren funcionamientos del mundo.

**Soluciones para Expertos**
Aun cuando la lógica de diseño general organiza el nivel para que el jugador pueda experimentar la narrativa pensada, existen mecánicas ocultas y espacios extremadamente complejos que apoyan a quienes dedican mucho tiempo al juego. El expert path existe en paralelo al camino principal.

---

## 07 — Objetivos y Dificultad

Los objetivos son esenciales para mantener la motivación y guiar las acciones del jugador. Su disposición y variedad tienen un alto impacto en el ritmo y las emociones que genera el nivel.

| Plazo | Características |
|---|---|
| Corto | Claros, visibles, dificultad baja, recompensa baja. Acción sencilla integrada al núcleo del gameplay. Duran segundos. Sin ellos la experiencia carece de hilo conductor. |
| Mediano | Requieren habilidad o manejo de recursos. Recompensa que conecta con sistemas mayores (nuevas zonas, lore, acceso a niveles especiales). Frecuencia más separada que el corto plazo. |
| Largo | Estructura general del juego. Impactan el progreso global del jugador. Dan sentido a la progresión macro. |

**Dificultad Autoservicio**
Nintendo incorpora elementos que funcionan como selectores de dificultad in-game. El jugador elige la ruta que se adecúa a sus deseos sin que el sistema lo obligue a declarar una dificultad explícita. Caminos con más enemigos, alternativos, sin violencia — todos conducen al mismo objetivo final.

**Límites Flexibles**
Sistema que no bloquea de inmediato. Nintendo pone siempre más estrellas de las necesarias para avanzar, evitando bloqueos y dándole al jugador libertad para elegir cómo completar el nivel.

**Caminos Interconectados**
Todas las opciones de ruta se combinan en el mismo escenario y el jugador puede alternar entre ellas en casi todo momento. Evita la percepción de pasillos cerrados y genera múltiples experiencias que se adaptan al tipo de jugador.

**Skill Gate Low Cost**
Zonas opcionales que requieren una habilidad específica para acceder, pero cuya recompensa es opcional. No bloquean el avance principal. Son un desafío auto-seleccionado para jugadores más exigentes — invisibles para quien no los busca.

---

## 08 — Errores de Diseño a Evitar

Extraídos del análisis de Desvelado como caso de estudio — un plataformero con fortalezas reales pero con debilidades identificables que sirven como guía de lo que no hacer.

**Secuencia Específica Rígida**
Los diseños con soluciones rígidas obligan al jugador a realizar las acciones exactamente como las previó el diseñador. Reduce la apropiación mecánica del jugador y la expresividad a través del gameplay, generando bloqueos ante quien no sigue la secuencia exacta.

**Soft Block Involuntario**
Momentos donde el jugador queda sin los recursos o habilidades necesarias para salir del espacio y avanzar, porque nunca se pensó que llegaría a esa situación. Castiga la experimentación, obliga a reiniciar el nivel y atenta contra el ritmo de la aventura.

**Recorridos Ida y Vuelta**
La repetición de caminos dentro de una room por falta de caminos alternativos obliga al jugador a volver a pasar por el mismo lugar más de una vez. Eleva los niveles de repetición y puede convertirse en tedio pasadas unas horas de juego.

**Repetición Visual Sin Control**
La repetición visual convierte a todos los niveles en el mismo nivel, perdiendo sorpresa y reduciendo los puntos de referencia del jugador. Afecta la legibilidad y la capacidad de orientación en el espacio.

**Background Minimalista Sin Función**
Un background minimalista priva al nivel de sugerencias, caminos secretos, guías indirectas, profundidad y movimiento. El fondo puede hacer trabajo de diseño — cuando no lo hace, es una oportunidad perdida.

> **Fortalezas de Desvelado a replicar:** ritmo frenético con rooms de 1-3 segundos que eliminan la sensación de bloqueo · introducción pausada de mecánicas con skill gate como examen inmediato · fragmentación de áreas con navegación ágil entre fragmentos · predominancia de refuerzo de aprendizaje entre rooms contiguas.

---

## 09 — BattleBlock Theater — Proceso de Creación

Del blog de The Behemoth (Ryan Horn). Proceso de 3 etapas para construir niveles de plataformero + técnicas para salir del bloqueo creativo.

> "Ningún nivel existe en el vacío. Cuando el jugador lo juegue va a estar entre otros niveles — siempre diseñá considerando la progresión."
> — Ryan Horn · The Behemoth · BattleBlock Theater devblog

**Etapa 1 — Lay the Bones**
Empezar grande e ir achicando. Thumbnails y garabatos del recorrido del jugador — squiggles. Importar el esquema al editor y dejar que las formas y espacios emergentes definan la estructura. El esqueleto no es el nivel final: es la silueta del recorrido.

**Etapa 2 — Muscle the Skeleton**
Músculo = secuencias de gameplay. Guiarse por las formas que emergieron. Ver las relaciones entre secciones del nivel para que interactúen de forma interesante. El objetivo central es mantener al jugador en movimiento constante.

**Etapa 3 — Flesh the Muscle**
Pulido + playtesting repetido. Es la etapa más larga. Los ajustes de timing, la dificultad fina y la coherencia del nivel se resuelven acá. Sin playtesting esta etapa no puede completarse correctamente.

### Técnicas para evitar el Level Design Block

**Rotar el nivel** — Girar el layout 90° o 180° para ver si la estructura funciona en otras orientaciones. Revela simetrías no intencionales y oportunidades perdidas.

**Cambiar el tema visual** — A veces el bloqueo es estético, no de gameplay. Cambiar el skin del nivel puede desbloquear ideas de mecánica que el tema original no sugería.

**Empezar desde el final** — Diseñar el clímax primero y construir hacia atrás. Define el pico emocional y facilita construir la curva de escalada de forma coherente.

**Restricción deliberada** — Imponerse un límite arbitrario: solo X tipos de plataforma, sin scroll horizontal. Las restricciones fuerzan soluciones creativas e impiden la sobre-complicación.

**Copiar y romper** — Tomar una sección que ya funciona, copiarla y modificarla hasta convertirla en algo diferente. Acelera la producción y garantiza una base probada.

**Playtesting temprano** — Testear el nivel lo antes posible, incluso en la Etapa 1. Ver a alguien jugarlo con ojos frescos revela problemas de legibilidad invisibles desde adentro.