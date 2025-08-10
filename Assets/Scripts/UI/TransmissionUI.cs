using UnityEngine;
using TMPro;

public class TransmissionUI : MonoBehaviour
{
    [SerializeField] private Transmision transmission;
    [SerializeField] private TextMeshProUGUI gearText;

    private void Update()
    {
        if (transmission == null || gearText == null) return;

        string mode = transmission.isAutomatic ? "A" : "M";
        string gear = transmission.GetCurrentGear().ToString();

        gearText.text = $"{mode} | {gear}";
    }
}
