Implementa la siguiente feature para Templo Utaki: $ARGUMENTS

## Flujo obligatorio

**Paso 1 — Contexto**
Lee `SESION.md` (tareas actuales y estado de sesión) y las secciones relevantes de `CLAUDE1.md` (GDD autoritativo). Si la feature involucra un script existente, léelo completo antes de escribir una sola línea.

**Paso 2 — Plan**
Antes de tocar código, describe en 3–5 puntos qué vas a hacer y por qué. Señala cualquier discrepancia entre la solicitud y el GDD. Espera confirmación si el cambio es mayor o destructivo.

**Paso 3 — Implementación**
Escribe o modifica el código siguiendo sin excepción:
- `[SerializeField] private` — nunca `public` para el Inspector
- Cachear referencias en `Awake()` o `Start()` — nunca en `Update/FixedUpdate/LateUpdate`
- `GameDebug.Log()` — nunca `Debug.Log()` directo
- Input: legacy `Input.GetKey/GetAxis` (override activo en ProjectConfig: `allow_legacy_input_for_existing`)
- Física en `FixedUpdate`, input en `Update` — nunca mezclar
- Corrutinas para fades y lógica de nivel (override activo: `allow_coroutines_for_logic`)
- Comentarios en español, naming en inglés estándar Unity
- Orden de clase: constantes → campos static → [SerializeField] → campos privados → propiedades → lifecycle → métodos públicos → métodos privados → event handlers

**Paso 4 — Verificación GDD**
Compara el resultado contra la especificación en `CLAUDE1.md`. Lista explícitamente: ✅ cumple / ⚠️ desviación justificada / ❌ pendiente.

**Paso 5 — Cierre**
Indica qué archivos fueron modificados y si hay algo que Bonio debe revisar o aprobar antes de commitear.
