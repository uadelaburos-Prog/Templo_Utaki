// Interfaz para objetos que el gancho puede enganchar en modo Pull (AttachType.Pull).
// Implementada por ReactiveWall, Lever, y cualquier futuro objeto hookeable con lógica propia.
public interface IHookable
{
    void OnHooked();
    void OnReleased();
}
