Diagnostica y corrige el siguiente problema en Templo Utaki: $ARGUMENTS

## Flujo

**Paso 1 — Reproducir el problema**
Describe exactamente cuál es el comportamiento actual vs el esperado según el GDD (`CLAUDE1.md`). Si el problema involucra física, timing o estados, identifica en qué método ocurre (`Update`, `FixedUpdate`, `OnTriggerEnter2D`, etc.).

**Paso 2 — Leer el código relevante**
Lee los scripts involucrados completos — nunca diagnostiques desde memoria. Para bugs de interacción entre sistemas, lee todos los scripts que participan.

**Paso 3 — Hipótesis**
Lista 2–3 causas posibles ordenadas por probabilidad. Para cada una, explica por qué explicaría el síntoma observado.

**Paso 4 — Diagnóstico**
Identifica la causa raíz. Busca específicamente:
- `GetComponent` en `Update/FixedUpdate` (performance y crash)
- Física en `Update` en lugar de `FixedUpdate`
- Order of execution issues (usa Script Execution Order si aplica)
- Referencias null no verificadas
- Corrutinas que no se detienen al destruirse el objeto
- Colisiones en layers incorrectos
- `Transform.Translate` en lugar de `Rigidbody2D.MovePosition`

**Paso 5 — Fix**
Aplica la corrección mínima necesaria. No refactorices código que no está roto. Explica en una oración por qué este fix resuelve la causa raíz.

**Paso 6 — Verificación**
Indica cómo probar que el fix funciona (qué hacer en el Editor de Unity para confirmar que el bug ya no ocurre).
