using EMT;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SonidoManager : MonoBehaviour, ISingleton
{
    [Header("Prefab")]
    [SerializeField] private GameObject customASPrefab;

    [Header("Pool de efectos")]
    [SerializeField, Min(1)]
    private int maxAudioSources = 20;

    private readonly Queue<AudioSource> pool = new();

    private readonly HashSet<AudioSource> sourcesEnUso = new();

    private void Start()
    {
        PrewarmPool();
    }

    private void PrewarmPool()
    {
        maxAudioSources = Mathf.Max(1, maxAudioSources);

        for (int i = 0; i < maxAudioSources; i++)
        {
            AudioSource source = CreateAudioSource();

            if (source != null)
                pool.Enqueue(source);
        }
    }

    private AudioSource CreateAudioSource()
    {
        AudioSource newAudioSouce;

        if (customASPrefab == null)
        {
            newAudioSouce = new GameObject().AddComponent<AudioSource>();
        }
        else
        {     
            newAudioSouce = Instantiate(customASPrefab, transform).GetComponent<AudioSource>();
        }

        if (newAudioSouce == null)
        {
            newAudioSouce.name = "SFXAudioSource";
            newAudioSouce.gameObject.SetActive(false);

            Debug.LogError("SonidoManager: sonidoPrefab no contiene AudioSource.");

            Destroy(newAudioSouce);

            return null;
        }

        newAudioSouce.playOnAwake = false;
        newAudioSouce.loop = false;

        return newAudioSouce;
    }

    private AudioSource ObtenerDelPool()
    {
        if (pool.Count == 0)
        {
            // No crear más de maxAudioSources.
            // Si todos están ocupados, se ignora este sonido.
            return null;
        }

        AudioSource source = pool.Dequeue();

        source.gameObject.SetActive(true);

        source.Stop();

        source.clip = null;
        source.pitch = 1f;
        source.volume = 0f;
        source.loop = false;

        sourcesEnUso.Add(source);

        return source;
    }

    private void DevolverAlPool(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();

        source.clip = null;
        source.pitch = 1f;
        source.volume = 0f;
        source.loop = false;

        source.transform.position =
            transform.position;

        source.gameObject.SetActive(false);

        sourcesEnUso.Remove(source);

        pool.Enqueue(source);
    }

    public void ReproducirSonido(
        AudioClip clip,
        Vector3 position,
        float pitch = 1f,
        float volumen = 1f
    )
    {
        ReproducirSonidoInterno(
            clip,
            position,
            pitch,
            volumen
        );
    }

    public void ReproducirSonido(AudioClip clip, float pitch = 1f, float volumen = 1f)
    {
        Vector3 position = Camera.main != null? Camera.main.transform.position : transform.position;

        ReproducirSonidoInterno(
            clip,
            position,
            pitch,
            volumen
        );
    }

    private void ReproducirSonidoInterno(
        AudioClip clip,
        Vector3 position,
        float pitch,
        float volumen
    )
    {
        if (clip == null)
            return;

        AudioSource source = ObtenerDelPool();

        if (source == null)
        {
            // Los 20 AudioSources están ocupados.
            // No generamos otro GameObject.
            return;
        }

        source.transform.position = position;

        source.clip = clip;
        source.pitch = Mathf.Max(0.01f, pitch);
        source.volume =
            volumen * GetSFXVolume();

        source.loop = false;

        source.Play();

        StartCoroutine(
            LiberarCuandoTermine(source)
        );
    }

    // ---------------------------------------------------------
    // LIBERACIÓN AUTOMÁTICA
    // ---------------------------------------------------------

    private IEnumerator LiberarCuandoTermine(
        AudioSource source
    )
    {
        // Esperar hasta que el AudioSource termine.
        while (source != null &&
               source.isPlaying)
        {
            yield return null;
        }

        if (source != null &&
            sourcesEnUso.Contains(source))
        {
            DevolverAlPool(source);
        }
    }

    // ---------------------------------------------------------
    // SONIDOS LOOPEABLES
    // ---------------------------------------------------------

    // IMPORTANTE:
    // Estos NO utilizan el pool.
    // Se comportan como antes.

    public AudioSource ReproducirSonidoLoopeable(
        AudioClip clip,
        float pitch = 1f,
        float volumen = 1f
    )
    {
        if (clip == null)
            return null;

        Vector3 position =
            Camera.main != null
                ? Camera.main.transform.position
                : transform.position;

        GameObject instance =
            Instantiate(
                customASPrefab,
                position,
                Quaternion.identity
            );

        AudioSource source =
            instance.GetComponent<AudioSource>();

        if (source == null)
        {
            Destroy(instance);
            return null;
        }

        source.loop = true;
        source.clip = clip;
        source.pitch = pitch;
        source.volume =
            volumen * GetSFXVolume();

        source.Play();

        return source;
    }

    // ---------------------------------------------------------
    // VOLUMEN
    // ---------------------------------------------------------

    private float GetSFXVolume()
    {
        if (Singleton<DataManager>.singleton == null ||
            Singleton<DataManager>.singleton.currentData == null)
        {
            return 1f;
        }

        return Singleton<DataManager>.singleton.currentData.soundSettings.SFXPercent * 0.01f;
    }

    // ---------------------------------------------------------
    // LIMPIEZA
    // ---------------------------------------------------------

    public void DetenerTodosLosSonidos()
    {
        foreach (AudioSource source in
                 new List<AudioSource>(sourcesEnUso))
        {
            DevolverAlPool(source);
        }
    }
}