[System.Serializable]
public class SimulacionCreateDTO
{
    public string ModuloSlug; // nombre de la escena actual
    public bool Cobertura; // true si aprobado (>=75% en cada terreno) 
    public int UsuarioId; // id del usuario
}