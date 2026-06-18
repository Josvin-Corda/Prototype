using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ExclusiveAudioZoneController : MonoBehaviour
{
    [SerializeField] private Transform playerRoot;

    [SerializeField] private AudioMixerSnapshot listenerOutsideSnapshot;
    [SerializeField] private AudioMixerSnapshot listenerInsideSnapshot;

    [SerializeField] private AudioMixerGroup insideZoneGroup;
    [SerializeField] private AudioMixerGroup outsideZoneGroup;

    [SerializeField, Min(0f)] private float transitionTime = 0.05f;

    private readonly HashSet<Collider> playerCollidersInside = new HashSet<Collider>();
    private readonly Dictionary<Transform, int> emitterColliderCount = new Dictionary<Transform, int>();
    private readonly Dictionary<Transform, List<AudioSource>> emitterAudioSources = new Dictionary<Transform, List<AudioSource>>();

    private void Awake()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (listenerOutsideSnapshot != null)
        {
            listenerOutsideSnapshot.TransitionTo(0f);
        }
    }

    private void OnDisable()
    {
        foreach (var pair in emitterAudioSources)
        {
            List<AudioSource> sources = pair.Value;
            if (sources != null)
            {
                foreach (AudioSource source in sources)
                {
                    if (source != null)
                    {
                        source.outputAudioMixerGroup = outsideZoneGroup;
                    }
                }
            }
        }

        playerCollidersInside.Clear();
        emitterColliderCount.Clear();
        emitterAudioSources.Clear();

        if (listenerOutsideSnapshot != null)
        {
            listenerOutsideSnapshot.TransitionTo(0f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // Player Detection
        if (playerRoot != null && (other.transform == playerRoot || other.transform.IsChildOf(playerRoot)))
        {
            if (playerCollidersInside.Add(other))
            {
                if (playerCollidersInside.Count == 1)
                {
                    if (listenerInsideSnapshot != null)
                    {
                        listenerInsideSnapshot.TransitionTo(transitionTime);
                    }
                }
            }
            return;
        }

        // Emitter Detection
        List<AudioSource> eligibleSources;
        Transform owner = FindEmitterOwnerAndAudioSources(other.transform, out eligibleSources);
        if (owner != null && eligibleSources.Count > 0)
        {
            if (!emitterColliderCount.ContainsKey(owner))
            {
                emitterColliderCount[owner] = 1;
                emitterAudioSources[owner] = eligibleSources;
                foreach (AudioSource source in eligibleSources)
                {
                    if (source != null)
                    {
                        source.outputAudioMixerGroup = insideZoneGroup;
                    }
                }
            }
            else
            {
                emitterColliderCount[owner]++;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null) return;

        // Player Detection
        if (playerRoot != null && (other.transform == playerRoot || other.transform.IsChildOf(playerRoot)))
        {
            if (playerCollidersInside.Remove(other))
            {
                if (playerCollidersInside.Count == 0)
                {
                    if (listenerOutsideSnapshot != null)
                    {
                        listenerOutsideSnapshot.TransitionTo(transitionTime);
                    }
                }
            }
            return;
        }

        // Emitter Detection
        List<AudioSource> eligibleSources;
        Transform owner = FindEmitterOwnerAndAudioSources(other.transform, out eligibleSources);
        if (owner != null)
        {
            if (emitterColliderCount.ContainsKey(owner))
            {
                emitterColliderCount[owner]--;
                if (emitterColliderCount[owner] <= 0)
                {
                    if (emitterAudioSources.TryGetValue(owner, out List<AudioSource> sources) && sources != null)
                    {
                        foreach (AudioSource source in sources)
                        {
                            if (source != null)
                            {
                                source.outputAudioMixerGroup = outsideZoneGroup;
                            }
                        }
                    }
                    emitterColliderCount.Remove(owner);
                    emitterAudioSources.Remove(owner);
                }
            }
        }
    }

    private Transform FindEmitterOwnerAndAudioSources(Transform start, out List<AudioSource> eligibleSources)
    {
        eligibleSources = new List<AudioSource>();
        Transform current = start;
        while (current != null)
        {
            AudioSource[] sources = current.GetComponentsInChildren<AudioSource>(true);
            if (sources != null)
            {
                foreach (AudioSource source in sources)
                {
                    if (source != null &&
                        (source.outputAudioMixerGroup == insideZoneGroup || source.outputAudioMixerGroup == outsideZoneGroup))
                    {
                        eligibleSources.Add(source);
                    }
                }
            }
            if (eligibleSources.Count > 0)
            {
                return current;
            }
            current = current.parent;
        }
        return null;
    }
}
