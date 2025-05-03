using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Needed for list manipulation
using UnityEngine.SceneManagement; // Required for scene management

[RequireComponent(typeof(AudioSource))] // Ensure an AudioSource component exists
public class GameplayMusicPlayer : MonoBehaviour
{
    public static GameplayMusicPlayer Instance { get; private set; }

    [Tooltip("Assign your gameplay music tracks here.")]
    public AudioClip[] gameplayMusicTracks;

    [Tooltip("Volume for the gameplay music tracks.")]
    [Range(0f, 1f)]
    public float musicVolume = 0.04f; // Reduced default volume by 50% (was 0.08)

    [Tooltip("Duration of the fade between tracks in seconds.")]
    public float fadeDuration = 1.0f; 
    
    [Tooltip("Name of the main gameplay scene.")]
    public string gameplaySceneName = "MainScene"; // ** IMPORTANT: Set this in Inspector if your scene name is different **

    private AudioSource audioSource;
    private List<int> availableTrackIndices;
    private int currentTrackIndex = -1;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        // --- Singleton Implementation ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
            Debug.Log("GameplayMusicPlayer Singleton created and marked DontDestroyOnLoad.");
        }
        else if (Instance != this) // If another instance exists
        {
             Debug.Log("Duplicate GameplayMusicPlayer found, destroying self.");
            Destroy(gameObject); // Destroy duplicate
            return;
        }
        // ------------------------------

        audioSource = GetComponent<AudioSource>();
        // Configure AudioSource settings
        audioSource.loop = false; // We handle looping/transitions manually
        audioSource.playOnAwake = false;
        audioSource.volume = 0; // Start silent, fade in first track

        InitializeAvailableTracks();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded; // Subscribe to scene load event
        Debug.Log("GameplayMusicPlayer subscribed to sceneLoaded.");
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe
         Debug.Log("GameplayMusicPlayer unsubscribed from sceneLoaded.");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene Loaded: {scene.name}");
        // Check if the loaded scene is the designated gameplay scene
        if (scene.name == gameplaySceneName)
        {
             Debug.Log("Gameplay Scene loaded.");
            // If music isn't already playing (e.g., first load or after returning from menu)
            if (!audioSource.isPlaying)
            {
                 Debug.Log("AudioSource not playing, starting random track with fade-in.");
                PlayRandomTrack(true); // Start music with fade-in
            }
             else {
                 Debug.Log("AudioSource already playing, continuing current track.");
             }
        }
        else // If any other scene is loaded (like MainMenu)
        {
            Debug.Log($"Non-gameplay scene ({scene.name}) loaded. Stopping music and destroying self.");
            // Stop music immediately
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            // Destroy this persistent object as we are leaving the gameplay context
            Destroy(gameObject); 
        }
    }

    void InitializeAvailableTracks()
    {
        availableTrackIndices = new List<int>();
        if (gameplayMusicTracks != null)
        {
            for (int i = 0; i < gameplayMusicTracks.Length; i++)
            {
                availableTrackIndices.Add(i);
            }
        }
    }

    void PlayRandomTrack(bool fadeIn = false)
    {
        if (availableTrackIndices == null || gameplayMusicTracks == null || gameplayMusicTracks.Length == 0)
        {
            InitializeAvailableTracks(); 
            if (gameplayMusicTracks == null || gameplayMusicTracks.Length == 0) return; 
        }

        // Refill list if empty
        if (availableTrackIndices.Count == 0)
        {
            InitializeAvailableTracks();
            if (currentTrackIndex != -1 && availableTrackIndices.Count > 1) 
            {
                availableTrackIndices.Remove(currentTrackIndex);
            }
             Debug.Log("Refilled gameplay music track list.");
        }

        if (availableTrackIndices.Count == 0)
        {
             Debug.LogWarning("GameplayMusicPlayer: Cannot play random track, list empty/only one track.", this);
             if(gameplayMusicTracks.Length > 0)
             {
                currentTrackIndex = 0;
                availableTrackIndices.Add(0); 
             } else {
                 return; 
             }
        }

        // Select random index
        int randomIndexInList = Random.Range(0, availableTrackIndices.Count);
        currentTrackIndex = availableTrackIndices[randomIndexInList];
        availableTrackIndices.RemoveAt(randomIndexInList);

        // Assign and play
        if (currentTrackIndex >= 0 && currentTrackIndex < gameplayMusicTracks.Length)
        {
            audioSource.clip = gameplayMusicTracks[currentTrackIndex];
            audioSource.volume = fadeIn ? 0f : musicVolume; // Start at 0 if fading in
            audioSource.Play();
            Debug.Log($"Playing gameplay track: {audioSource.clip.name}");

            if (fadeIn)
            {
                StartCoroutine(FadeIn());
            }
        } else {
            Debug.LogError($"GameplayMusicPlayer: Invalid track index selected: {currentTrackIndex}", this);
        }
    }

    // --- Fade Implementation ---

    IEnumerator FadeAndPlayNextTrack()
    {
        Debug.Log("Starting fade out...");
        yield return StartCoroutine(FadeOut());
        Debug.Log("Fade out complete. Playing next track...");
        PlayRandomTrack(true); // Play next track with fade in
        fadeCoroutine = null; // Reset coroutine flag
    }

    IEnumerator FadeOut()
    {
        float startVolume = audioSource.volume; // Use current volume
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
            yield return null;
        }
        audioSource.volume = 0f;
        audioSource.Stop(); // Stop playback after fading out
    }

    IEnumerator FadeIn()
    {
        float timer = 0f;
        // Ensure playback is started before fading in volume
        if (!audioSource.isPlaying)
        {
            Debug.LogWarning("FadeIn called but AudioSource wasn't playing. Starting playback.");
            audioSource.Play();
        }
        Debug.Log("Starting fade in...");

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, musicVolume, timer / fadeDuration);
            yield return null;
        }
        audioSource.volume = musicVolume;
         Debug.Log("Fade in complete.");
    }
} 