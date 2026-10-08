using EMT;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour, ISingleton
{
    [Header("Música")]
    [SerializeField] AudioClip[] clipsMusic;

    [Header("Prefab que contiene un AudioSource")]
    [SerializeField] GameObject musicLayerPrefab;

    [Header("Pool de música")]
    [SerializeField, Min(1)] int poolSize = 2;

    [Header("Precarga de Audio")]
    [SerializeField] AudioClip[] precargarAudioData;

    public float bpm = 160f;

    double SecondsPerBeat => 60.0 / bpm;

    readonly Dictionary<AudioSource, double> startTimes = new();
    readonly Queue<AudioSource> pool = new();

    readonly List<AudioClip> clipsInMemory = new();
    readonly Dictionary<string, AudioClip> clipsNames = new();

    AudioSource currentMainSource;
    Coroutine crossfadeCoroutine;

    void Awake()
    {
        PrewarmPool();
    }

    void Start()
    {
        PreloadClips(precargarAudioData);
    }

    public void PreloadClips(AudioClip[] clips)
    {
        if (clips == null)return;

        foreach (AudioClip clip in clips)
        {
            if (clip != null) PreloadClip(clip);
        }
    }

    public void PreloadClip(AudioClip clip)
    {
        if (clip == null)
            return;

        // Mantener referencia fuerte.
        if (!clipsInMemory.Contains(clip))
            clipsInMemory.Add(clip);

        clipsNames[clip.name] = clip;

        // Solicitar carga de los datos.
        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();
    }

    private void PrewarmPool()
    {
        poolSize = Mathf.Max(1, poolSize);

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = CreatePooledInstance();
            pool.Enqueue(source);
        }
    }

    AudioSource CreatePooledInstance()
    {
        GameObject obj = Instantiate(musicLayerPrefab, transform);

        obj.name = "MusicAudioSource";

        obj.SetActive(false);

        
        if (!obj.TryGetComponent<AudioSource>(out var source))
        {
            Debug.LogError("MusicManager: El prefab de música no contiene un AudioSource.");

            Destroy(obj);
            return null;
        }

        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;

        return source;
    }

    AudioSource GetFromPool()
    {
        AudioSource source;

        if (pool.Count > 0)
        {
            source = pool.Dequeue();
        }
        else
        {
            // Seguridad: si hay más capas simultáneas de las previstas,
            // se crea una temporal.
            source = CreatePooledInstance();
        }

        if (source == null)
            return null;

        source.gameObject.SetActive(true);
        source.Stop();
        source.clip = null;
        source.volume = 0f;

        return source;
    }

    void ReturnToPool(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();
        source.clip = null;
        source.volume = 0f;
        source.pitch = 1f;

        startTimes.Remove(source);

        source.gameObject.SetActive(false);

        pool.Enqueue(source);
    }

    public AudioSource PlayFirstTrack(int indiceMusica, double delay = 0.1)
    {
        if (clipsMusic == null || indiceMusica < 0 || indiceMusica >= clipsMusic.Length)
        {
            Debug.LogWarning("Índice de música inválido.");
            return null;
        }

        AudioSource source = GetFromPool();

        if (source == null) return null;

        // Si había una pista anterior, detenerla.
        if (currentMainSource != null)
        {
            ReturnToPool(currentMainSource);
            currentMainSource = null;
        }

        source.volume = GetMusicVolume();
        source.clip = clipsMusic[indiceMusica];

        double startDsp = AudioSettings.dspTime + delay;

        source.PlayScheduled(startDsp);

        startTimes[source] = startDsp;

        currentMainSource = source;

        return source;
    }

    public double GetNextBeatDspTime(AudioSource referenceSource)
    {
        if (referenceSource == null || !startTimes.TryGetValue(referenceSource, out double dspStartTime)) return AudioSettings.dspTime;

        double currentDsp = AudioSettings.dspTime;
        double elapsed = currentDsp - dspStartTime;

        double beatsElapsed = elapsed / SecondsPerBeat;

        double nextBeatIndex = Math.Ceiling(beatsElapsed);

        if (beatsElapsed - Math.Floor(beatsElapsed) < 0.001) nextBeatIndex += 1;

        return dspStartTime + (nextBeatIndex * SecondsPerBeat);
    }

    public void CrossfadeToNextTrack(int siguienteIndiceMusica, float crossfadeDuration = 4f)
    {
        if (clipsMusic == null || siguienteIndiceMusica < 0 || siguienteIndiceMusica >= clipsMusic.Length)
        {
            Debug.LogWarning("Índice de música inválido.");
            return;
        }

        if (currentMainSource == null || !startTimes.ContainsKey(currentMainSource))
        {
            Debug.LogWarning("No hay una pista principal activa registrada.");
            return;
        }

        // Evita múltiples crossfades simultáneos.
        if (crossfadeCoroutine != null)
        {
            Debug.LogWarning("Ya existe un crossfade en progreso.");
            return;
        }

        AudioSource layerSource = GetFromPool();

        if (layerSource == null) return;

        double nextBeatDspTime = GetNextBeatDspTime(currentMainSource);

        layerSource.clip = clipsMusic[siguienteIndiceMusica];

        layerSource.volume = 0f;

        layerSource.PlayScheduled(nextBeatDspTime);

        startTimes[layerSource] = nextBeatDspTime;

        crossfadeCoroutine = StartCoroutine(CrossfadeRoutine(currentMainSource, layerSource, nextBeatDspTime, Mathf.Max(0.01f, crossfadeDuration)));
    }

    IEnumerator CrossfadeRoutine(AudioSource mainSource, AudioSource layerSource, double startDspTime, float crossfadeDuration)
    {
        float targetVolume = GetMusicVolume();

        // Esperar hasta el beat exacto.
        while (AudioSettings.dspTime < startDspTime) yield return null;

        float startMainVolume = mainSource.volume;

        float elapsed = 0f;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(elapsed / crossfadeDuration);

            mainSource.volume =
                Mathf.Lerp(startMainVolume, 0f, t);

            layerSource.volume =
                Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }

        mainSource.volume = 0f;
        layerSource.volume = targetVolume;

        ReturnToPool(mainSource);

        currentMainSource = layerSource;

        crossfadeCoroutine = null;
    }

    public void StopAllMusic()
    {
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
            crossfadeCoroutine = null;
        }

        List<AudioSource> sourcesToStop = new(startTimes.Keys);

        foreach (AudioSource source in sourcesToStop)
        {
            ReturnToPool(source);
        }

        currentMainSource = null;
    }

    float GetMusicVolume()
    {
        if (Singleton<DataManager>.singleton == null || Singleton<DataManager>.singleton.currentData == null) return 1f;
        return Singleton<DataManager>.singleton.currentData.soundSettings.musicPercent * 0.01f;
    }
}