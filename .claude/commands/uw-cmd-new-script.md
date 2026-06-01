Crea un nuevo script de Unity para Templo Utaki: $ARGUMENTS

## Flujo

**Paso 1 — Spec**
Antes de escribir código, confirma:
- Nombre del script (PascalCase, sin espacios)
- Responsabilidad única (una oración)
- En qué GameObject vive y qué componentes necesita
- Si ya existe algo similar en `Assets/Scripts/` que debería extenderse en lugar de crear uno nuevo

**Paso 2 — Ubicación**
- Scripts de gameplay → `Templo_Utaki_Unity/Assets/Scripts/`
- Scripts de enemigos → `Templo_Utaki_Unity/Assets/Scripts/Enemies/`
- Scripts de menú/UI → `Templo_Utaki_Unity/Assets/Scripts/MenuScripts/`
- Verifica que el `.asmdef` correspondiente incluya este script

**Paso 3 — Estructura del archivo**
Sigue el orden exacto del estándar del proyecto:
```
using ...

namespace TemploUtaki  // solo si aplica
{
    public class NombreScript : MonoBehaviour
    {
        // 1. Constantes
        // 2. Campos static
        // 3. [SerializeField] private (con [Header] agrupados)
        // 4. Campos privados (referencias cacheadas)
        // 5. Propiedades públicas (si hacen falta)
        // 6. Lifecycle: Awake → Start → Update → FixedUpdate → LateUpdate
        // 7. Métodos públicos
        // 8. Métodos privados
        // 9. Event handlers / callbacks
    }
}
```

**Reglas no negociables:**
- `[SerializeField] private` — nunca `public` para el Inspector
- Toda referencia a componentes: cacheada en `Awake()`
- `GameDebug.Log()` — nunca `Debug.Log()`
- Input legacy (`Input.GetKey/GetAxis`) si el script maneja input
- Física solo en `FixedUpdate`
- Comentarios en español, naming en inglés

**Paso 4 — Crear el archivo**
Escribe el archivo en la ruta correcta. NO crear ni modificar `.meta` files.

**Paso 5 — Siguiente paso**
Indica qué falta configurar en el Editor de Unity (asignar referencias en Inspector, añadir a prefab, configurar layers, etc.).
