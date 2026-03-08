using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Audio Library")]
public class AudioLibrary : ScriptableObject
{
    [System.Serializable]
    public class AudioEntry
    {
        [Tooltip("ID, e.g. 'Pistol/Shoot', 'Zombie/Hit', 'UI/Click'")]
        public string id;

        [Tooltip("Single clip for this id. Use the same id on multiple rows for variation.")]
        public AudioClip clip;
    }

    [Tooltip("One row per clip. Use the SAME id across multiple rows to randomize between them.")]
    public AudioEntry[] entries;

    // id -> array of clips
    private Dictionary<string, AudioClip[]> lookup;

    void OnEnable()
    {
        BuildLookup();
    }

    void BuildLookup()
    {
        lookup = new Dictionary<string, AudioClip[]>();

        if (entries == null || entries.Length == 0)
            return;

        // temp: id -> list
        Dictionary<string, List<AudioClip>> temp = new Dictionary<string, List<AudioClip>>();

        foreach (var e in entries)
        {
            if (e == null) continue;
            if (string.IsNullOrEmpty(e.id)) continue;
            if (e.clip == null) continue;

            if (!temp.TryGetValue(e.id, out var list))
            {
                list = new List<AudioClip>();
                temp.Add(e.id, list);
            }

            list.Add(e.clip);
        }

        // convert lists to arrays
        foreach (var kvp in temp)
        {
            lookup[kvp.Key] = kvp.Value.ToArray();
        }
    }

    public AudioClip GetRandomClip(string id)
    {
        if (lookup == null || lookup.Count == 0)
            BuildLookup();

        if (string.IsNullOrEmpty(id))
            return null;

        if (!lookup.TryGetValue(id, out var clips) || clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"[AudioLibrary] No clips found for id '{id}'", this);
            return null;
        }

        int index = Random.Range(0, clips.Length);
        return clips[index];
    }
}
