using UnityEngine;

public class TransmissionSelector : MonoBehaviour
{
    [SerializeField] private Transmision transmission;

    public void SetAutomatic()
    {
        transmission.isAutomatic = true;
    }

    public void SetManual()
    {
        transmission.isAutomatic = false;
    }
}
