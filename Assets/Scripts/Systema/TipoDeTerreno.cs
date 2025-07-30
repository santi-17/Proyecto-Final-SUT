using UnityEngine;

public class TipoDeTerreno : MonoBehaviour
{
    public enum TipoTerreno
    {
        A, B, C, D
    }

    public TipoTerreno tipo;

    public TipoTerreno GetTipo()
    {
        return tipo;
    }
}
