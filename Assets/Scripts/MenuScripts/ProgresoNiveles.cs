using UnityEngine;

// Persistencia del progreso de niveles vía PlayerPrefs. Fuente única de verdad del
// desbloqueo: guarda el mayor build index alcanzado. Lo escribe GameLoopManager al
// completar un nivel y lo lee SelectorNiveles para habilitar/bloquear botones.
// Convención de escenas: build index 0 = menú · 1..N = niveles jugables.
public static class ProgresoNiveles
{
    private const string KEY          = "NivelMaxDesbloqueado";
    private const int    PRIMER_NIVEL = 1; // El primer nivel jugable siempre está disponible.

    // Mayor build index desbloqueado (nunca por debajo del primer nivel).
    public static int MaxDesbloqueado => Mathf.Max(PRIMER_NIVEL, PlayerPrefs.GetInt(KEY, PRIMER_NIVEL));

    public static bool EstaDesbloqueado(int buildIndex) => buildIndex <= MaxDesbloqueado;

    // Marca un nivel como desbloqueado (y todos los anteriores). Solo sube, nunca baja.
    public static void DesbloquearHasta(int buildIndex)
    {
        if (buildIndex > MaxDesbloqueado)
        {
            PlayerPrefs.SetInt(KEY, buildIndex);
            PlayerPrefs.Save();
        }
    }

    // Borra el progreso (vuelve a dejar solo el primer nivel). Útil para testing/opciones.
    public static void Reiniciar()
    {
        PlayerPrefs.DeleteKey(KEY);
        PlayerPrefs.Save();
    }
}
