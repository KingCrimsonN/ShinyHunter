using UnityEngine;

public class LifeClock : MonoBehaviour
{
    private AudioSource audioSource;
    private bool timeRunningOut;
    [SerializeField] private float startVolume = 0.1f;
    [SerializeField] private float endVolume = 1f;
    [Tooltip("Life seconds left for the audio to start")]
    [SerializeField] private float startThreshold = 30f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        var health = PlayerHealth.Instance;
        if (health == null) return;

        // Only while a run is going - and once it has ended (time out) it stays at the end look.
        if (health != null && (health.IsRunning || health.currentHealth <= 0f) && !SceneNames.IsHub(gameObject.scene.name))
        {
            float left = Mathf.Max(0f, health.currentHealth);
            UpdateVolume(left);
            if (left <= startThreshold && !timeRunningOut && left > 0f)
            {
                StartTicking(left);
            }
            else if (left > startThreshold && timeRunningOut || left <= 0f)
            {
                StopTicking();
            }
        }
    }

    void StartTicking(float left)
    {
        timeRunningOut = true;
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void UpdateVolume(float left)
    {
        float volume = Mathf.Lerp(endVolume, startVolume, left / startThreshold);
        audioSource.volume = volume;
    }

    void StopTicking()
    {
        timeRunningOut = false;
        audioSource.Stop();
    }
}
