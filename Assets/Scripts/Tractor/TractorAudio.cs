using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TractorAudio : MonoBehaviour
{
    public TractorControler tractor;       // Referencia al script del tractor
    public AudioClip engineStartClip;      // Clip de arranque
    public AudioClip engineIdleClip;       // Clip de marcha
    public float minPitch = 0.8f;
    public float maxPitch = 2.0f;

    private AudioSource audioSource;
    private bool motorEncendido = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    void Update()
    {
        float velocidad = tractor.rb.velocity.magnitude; // velocidad real del tractor

        // Arranque del motor
        if (!motorEncendido && (Mathf.Abs(Input.GetAxis("Vertical")) > 0.01f))
        {
            motorEncendido = true;
            audioSource.clip = engineStartClip;
            audioSource.loop = false;
            audioSource.Play();
            Invoke("StartIdle", engineStartClip.length); // pasa a marcha después del arranque
        }

        // Ajustar pitch dinámico cuando está en marcha
        if (motorEncendido && audioSource.clip == engineIdleClip)
        {
            float pitch = Mathf.Lerp(minPitch, maxPitch, velocidad / 20f); // ajusta 20 según tu velocidad máxima
            audioSource.pitch = pitch;
            audioSource.volume = 0.5f + Mathf.Clamp01(velocidad / 50f); // aumenta un poco el volumen con velocidad
        }
    }

    void StartIdle()
    {
        audioSource.clip = engineIdleClip;
        audioSource.loop = true;
        audioSource.Play();
    }
}
