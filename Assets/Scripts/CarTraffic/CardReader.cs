using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A reusable component for simulating a payment card reader.
/// Triggers when a PaymentCard is physically tapped near its collider and plays a feedback sound.
/// </summary>
public class CardReader : MonoBehaviour
{
    [Header("Reader Configuration")]
    [Tooltip("Unique ID to identify this reader.")]
    public string readerID = "EntranceReader";

    [Tooltip("Audio clip to play on successful read.")]
    public AudioClip beepSound;

    [Tooltip("AudioSource to play the beep sound from. If null, will try to get or add one.")]
    public AudioSource audioSource;

    [Tooltip("Trigger collider defining the detection zone. Exposed for manual adjustment.")]
    public Collider detectionTrigger;

    [Tooltip("Cooldown in seconds between successive readings.")]
    public float readCooldown = 1.5f;

    [Header("Events")]
    public UnityEvent<string> OnCardRead;

    private float cooldownTimer = 0f;

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (detectionTrigger == null)
        {
            detectionTrigger = GetComponent<Collider>();
        }

        if (detectionTrigger != null)
        {
            detectionTrigger.isTrigger = true;
        }
        else
        {
            Debug.LogWarning($"[CardReader] {gameObject.name} does not have a collider. Please add a BoxCollider trigger manually.");
        }
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (cooldownTimer > 0f) return;

        PaymentCard card = other.GetComponent<PaymentCard>();
        if (card == null) card = other.GetComponentInParent<PaymentCard>();

        if (card != null)
        {
            cooldownTimer = readCooldown;
            TriggerSuccessfulRead(card.cardNumber);
        }
    }

    public void TriggerSuccessfulRead(string cardNumber, bool playBeep = true)
    {
        if (playBeep)
        {
            PlayBeep();
        }
        Debug.Log($"[CardReader] Reader '{readerID}' read card: {cardNumber}");
        OnCardRead?.Invoke(cardNumber);
    }

    private void PlayBeep()
    {
        if (audioSource != null && beepSound != null)
        {
            audioSource.PlayOneShot(beepSound);
            return;
        }

        // Procedural synth beep
        AudioClip clip = CreateBeepClip();
        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }

    private AudioClip CreateBeepClip()
    {
        int samplerate = 44100;
        float frequency = 1200f; // 1.2 kHz crisp beep
        float duration = 0.12f;
        int sampleCount = Mathf.RoundToInt(samplerate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / samplerate;
            float fade = 1f;
            if (t > duration - 0.02f)
            {
                fade = (duration - t) / 0.02f;
            }
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.4f * fade;
        }

        AudioClip clip = AudioClip.Create("BeepProcedural", sampleCount, 1, samplerate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
