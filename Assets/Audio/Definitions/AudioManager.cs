using System;
using System.Collections.Generic;
using FMODUnity;
using FMOD.Studio;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace UnholyTrinity.Audio
{
[CreateAssetMenu(fileName = "AudioManager",
    menuName = "Unholy Trinity/Audio/Audio Manager")]
[Serializable]
public class AudioManager : SoSingleton<AudioManager>
{
    [NonSerialized]
    private List<EventInstance> _eventInstances = new();

    [NonSerialized]
    private List<StudioEventEmitter> _eventEmitters
        = new();

    protected override void OnEnable()
    {
        base.OnEnable();
        _eventInstances.Clear();
        _eventEmitters.Clear();
    }

    protected override void OnDisable()
    {
        CleanUp();
        base.OnDisable();
    }

    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    public EventInstance CreateInstance(EventReference eventReference)
    {
        EventInstance eventInstance
            = RuntimeManager.CreateInstance(eventReference);
        _eventInstances.Add(eventInstance);
        return eventInstance;
    }

    public StudioEventEmitter CreateEmitter(
        EventReference eventReference,
        GameObject emitterObject)
    {
        StudioEventEmitter emitter
            = emitterObject.GetComponent<StudioEventEmitter>();
        if (emitter == null)
        {
            throw new ArgumentException(
                $"{emitterObject.name} does not have an emitter");
        }

        emitter.EventReference = eventReference;
        _eventEmitters.Add(emitter);
        return emitter;
    }

    private void CleanUp()
    {
        foreach (EventInstance eventInstance in _eventInstances)
        {
            eventInstance.stop(STOP_MODE.IMMEDIATE);
            eventInstance.release();
        }

        foreach (StudioEventEmitter emitter in _eventEmitters)
        {
            emitter.Stop();
        }

        _eventInstances.Clear();
        _eventEmitters.Clear();
    }
}
}
