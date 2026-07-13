using System;
using UnityEngine;

// Persistencia del progreso de niveles vía PlayerPrefs. Fuente única de verdad del
// ORDEN de la campaña y del desbloqueo, todo por NOMBRE de escena (no por build index,
// así reordenar Build Settings no rompe nada). Lo escribe GameLoopManager al completar
// un nivel y lo lee SelectorNiveles para habilitar/bloquear botones.
public static class ProgresoNiveles
{
    // Orden canónico de los niveles jugables (nombres EXACTOS de las escenas en Build
    // Settings). Esta lista define la progresión: "siguiente nivel", "último nivel", etc.
    public static readonly string[] Orden =
    {
        "Nivel 1", "Nivel 2", "Nivel 3", "Nivel 4", "Nivel 5", "Nivel 6"
    };

    // Guarda el índice ordinal (dentro de 'Orden') del mayor nivel desbloqueado.
    // Sufijo V2: la clave vieja guardaba build index; se invalida para empezar limpio.
    private const string KEY = "NivelMaxDesbloqueadoV2";

    // Índice ordinal del mayor nivel desbloqueado (0 = solo el primero disponible).
    public static int MaxDesbloqueado => Mathf.Max(0, PlayerPrefs.GetInt(KEY, 0));

    // Posición de un nivel dentro del orden canónico (-1 si no es un nivel jugable).
    public static int IndiceDe(string nombreEscena) => Array.IndexOf(Orden, nombreEscena);

    // True si la escena es uno de los niveles jugables de la campaña.
    public static bool EsNivelJugable(string nombreEscena) => IndiceDe(nombreEscena) >= 0;

    public static bool EstaDesbloqueado(string nombreEscena)
    {
        int i = IndiceDe(nombreEscena);
        return i >= 0 && i <= MaxDesbloqueado;
    }

    // Marca un nivel como desbloqueado (y todos los anteriores). Solo sube, nunca baja.
    public static void DesbloquearHasta(string nombreEscena)
    {
        int i = IndiceDe(nombreEscena);
        if (i > MaxDesbloqueado)
        {
            PlayerPrefs.SetInt(KEY, i);
            PlayerPrefs.Save();
        }
    }

    // Nombre del nivel siguiente en el orden, o null si 'nombreActual' es el último
    // (o no es un nivel jugable).
    public static string SiguienteNivel(string nombreActual)
    {
        int i = IndiceDe(nombreActual);
        if (i < 0 || i + 1 >= Orden.Length) return null;
        return Orden[i + 1];
    }

    // True si 'nombreEscena' es el último nivel de la campaña.
    public static bool EsUltimoNivel(string nombreEscena)
    {
        int i = IndiceDe(nombreEscena);
        return i >= 0 && i == Orden.Length - 1;
    }

    // Borra el progreso (vuelve a dejar solo el primer nivel). Útil para testing/opciones.
    public static void Reiniciar()
    {
        PlayerPrefs.DeleteKey(KEY);
        PlayerPrefs.Save();
    }
}
