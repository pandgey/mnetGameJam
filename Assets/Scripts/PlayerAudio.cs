using UnityEngine;

// Shared by the player scripts so jump and death sounds play through one AudioSource.
public static class PlayerAudio
{
    public static AudioSource GetOrAddSource(GameObject owner)
    {
        var source = owner.GetComponent<AudioSource>();
        if (source == null)
        {
            source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        return source;
    }

    public static void Play(AudioSource source, AudioClip clip, float volume)
    {
        if (source != null && clip != null)
            source.PlayOneShot(clip, volume);
    }
}
