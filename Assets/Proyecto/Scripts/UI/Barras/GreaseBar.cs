using UnityEngine;
using UnityEngine.UI;

public class GreaseBar : MonoBehaviour
{
    [SerializeField] private GameData data;
    [SerializeField] private Image fillImage;
    [SerializeField] private float maxGrease = 100f;

    private void OnEnable()
    {
        if (data != null)
            data.OnDataChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (data != null)
            data.OnDataChanged -= Refresh;
    }

    private void Refresh()
    {
        if (fillImage == null || data == null) return;
        fillImage.fillAmount = Mathf.Clamp01(data.contadorGrasa / maxGrease);
    }
}