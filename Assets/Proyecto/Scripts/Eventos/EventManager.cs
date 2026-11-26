using UnityEngine;

// Interruptor general de los eventos del día.
// Los eventos los programa y lanza el DirectorEventos (a través del EventoEstadoManager);
// este componente solo dice si están permitidos. El tutorial los apaga mientras dura.
public class EventManager : MonoBehaviour
{
    [Tooltip("Desmarcado = el DirectorEventos no programa ni lanza eventos")]
    public bool eventosActivos = true;
}
