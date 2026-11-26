using UnityEngine;

public class ResumenNocheTrigger : MonoBehaviour
{
   
    public string idDialogo = "resumen_dia_1";
    public float retrasoAntesDeEmpezar = 0.8f;

   
    public bool forzarResumenSiempre = false;

    private void Start()
    {
        //se tiene que agregar un ntriger para que funcione, en este caso le puse el de la noche trigger
        Invoke(nameof(MostrarResumen), retrasoAntesDeEmpezar);
    }

    private void MostrarResumen()
    {
       
        if (forzarResumenSiempre)
        {
        
            DialogoManager.Instance?.MostrarDialogo(idDialogo);
            return;
        }

  
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance es null");
            return;
        }

        var data = GameManager.Instance.gameData;

        if (data.resumenNocheMostrado)
        {
         
            return;
        }

        
        data.resumenNocheMostrado = true;
        

       
        if (DialogoManager.Instance != null)
        {
            DialogoManager.Instance.OnDialogoTerminado += AlTerminarResumen;
            DialogoManager.Instance.MostrarDialogo(idDialogo);
        }
        else
        {
            Debug.LogError("DialogoManager.Instance es null");
        }
    }

    private void AlTerminarResumen(string id)
    {
        if (id != idDialogo) return;

        DialogoManager.Instance.OnDialogoTerminado -= AlTerminarResumen;

        if (GameManager.Instance != null)
            DialogoManager.Instance.MostrarDialogoDirecto("cartas_noche", Economia.LineasDeLaNoche(GameManager.Instance.GameData));
    }

    private void OnDestroy()
    {
        if (DialogoManager.Instance != null)
            DialogoManager.Instance.OnDialogoTerminado -= AlTerminarResumen;
    }

    [ContextMenu("Resetear Flag (Debug)")]
    public void ResetearFlag()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.gameData.resumenNocheMostrado = false;
            Debug.Log("Flag de resumen reseteado");
        }
    }
}