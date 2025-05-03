using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Needed for list manipulation

[RequireComponent(typeof(AudioSource))] // Ensure an AudioSource component exists
public class MenuMusicPlayer : MonoBehaviour
{
    [Tooltip("Assign your main menu music tracks here.")]
    public AudioClip[] menuMusicTracks;

    [Tooltip("Volume for the music tracks.")]
    [Range(0f, 1f)]
    public float musicVolume = 0.15f;

    private AudioSource audioSource;
    private List<int> availableTrackIndices;
    private int currentTrackIndex = -1;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        // Configure AudioSource settings
        audioSource.loop = false; // We handle looping/transitions manually
        audioSource.playOnAwake = false;
        audioSource.volume = musicVolume;

        InitializeAvailableTracks();
    }

    void Start()
    {
        if (menuMusicTracks == null || menuMusicTracks.Length == 0)
        {
            Debug.LogWarning("MenuMusicPlayer: No music tracks assigned!", this);
            return;
        }
        PlayRandomTrack();
    }

    void Update()
    {
        // Check if the AudioSource is initialized and a track has finished playing
        if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
        {
            // Simple transition: just play the next random track immediately
            PlayRandomTrack();
        }

        // Update volume if changed in inspector during runtime (optional)
        if (audioSource != null && audioSource.volume != musicVolume)
        {
             audioSource.volume = musicVolume;
        }
    }

    void InitializeAvailableTracks()
    {
        availableTrackIndices = new List<int>();
        if (menuMusicTracks != null)
        {
            for (int i = 0; i < menuMusicTracks.Length; i++)
            {
                availableTrackIndices.Add(i);
            }
        }
    }

    void PlayRandomTrack()
    {
        if (availableTrackIndices == null || menuMusicTracks == null || menuMusicTracks.Length == 0)
        {
            InitializeAvailableTracks(); // Try to re-initialize if null
            if (menuMusicTracks == null || menuMusicTracks.Length == 0) return; // Still no tracks
        }

        // If the list of available tracks is empty, refill it
        if (availableTrackIndices.Count == 0)
        {
            InitializeAvailableTracks();
            // Optional: Prevent the immediately previously played track from playing again
            if (currentTrackIndex != -1 && availableTrackIndices.Count > 1) // Ensure there's more than one track
            {
                availableTrackIndices.Remove(currentTrackIndex);
            }
             Debug.Log("Refilled music track list.");
        }

        if (availableTrackIndices.Count == 0)
        {
             Debug.LogWarning("Cannot play random track, only one track available or list is empty.", this);
             if(menuMusicTracks.Length > 0)
             {
                // If only one track total, just replay it
                currentTrackIndex = 0;
                availableTrackIndices.Add(0); // Add it back so it can be picked
             } else {
                 return; // No tracks at all
             }
        }


        // Select a random index from the available tracks
        int randomIndexInList = Random.Range(0, availableTrackIndices.Count);
        currentTrackIndex = availableTrackIndices[randomIndexInList];

        // Remove the selected track index so it's not immediately repeated
        availableTrackIndices.RemoveAt(randomIndexInList);

        // Assign and play the chosen clip
        if (currentTrackIndex >= 0 && currentTrackIndex < menuMusicTracks.Length)
        {
            audioSource.clip = menuMusicTracks[currentTrackIndex];
            audioSource.Play();
            Debug.Log($"Playing menu track: {audioSource.clip.name}");
        } else {
            Debug.LogError($"Invalid track index selected: {currentTrackIndex}", this);
        }
    }

    // --- Optional Fade Implementation (Example) ---
    // You can uncomment and integrate this if you want smooth fades

    /*
    private Coroutine fadeCoroutine;

    void Update()
    {
        if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying && fadeCoroutine == null)
        {
            fadeCoroutine = StartCoroutine(FadeAndPlayNextTrack(0.5f)); // Fade over 0.5 seconds
        }
         if (audioSource != null && audioSource.volume != musicVolume)
        {
             audioSource.volume = musicVolume;
        }
    }

    IEnumerator FadeAndPlayNextTrack(float fadeDuration)
    {
        yield return StartCoroutine(FadeOut(fadeDuration));
        PlayRandomTrack(); // This will start the new track at full volume set by musicVolume
        // Optional: Fade in if you want a slower start
        // yield return StartCoroutine(FadeIn(fadeDuration));
        fadeCoroutine = null; // Allow the check in Update again
    }

    IEnumerator FadeOut(float duration)
    {
        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }
        audioSource.volume = 0f;
        audioSource.Stop(); // Stop playback after fading out
    }

    IEnumerator FadeIn(float duration)
    {
        float timer = 0f;
         // Ensure playback is started before fading in volume
        if (!audioSource.isPlaying) audioSource.Play();

        while (timer < duration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, musicVolume, timer / duration);
            yield return null;
        }
        audioSource.volume = musicVolume;
    }
    */
} 