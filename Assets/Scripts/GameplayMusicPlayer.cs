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
    private bool isFading = false;

    // --- Added: Coroutine References ---
    private Coroutine fadeInCoroutine = null;
    private Coroutine fadeOutCoroutine = null;
    private Coroutine fadeAndPlayNextCoroutine = null;
    // --- End Added ---

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

        // --- Added: Stop existing fades on scene load ---
        if (fadeInCoroutine != null)
        {
            StopCoroutine(fadeInCoroutine);
            fadeInCoroutine = null;
            Debug.Log("Stopped existing FadeIn coroutine on scene load.");
        }
        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
            fadeOutCoroutine = null;
            Debug.Log("Stopped existing FadeOut coroutine on scene load.");
        }
        if (fadeAndPlayNextCoroutine != null)
        {
            StopCoroutine(fadeAndPlayNextCoroutine);
            fadeAndPlayNextCoroutine = null;
            Debug.Log("Stopped existing FadeAndPlayNextTrack coroutine on scene load.");
        }
        isFading = false; // Ensure fading flag is reset
        // --- End Added ---

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

    void Update() // Need Update to check if track finished when not looping
    {
        if (audioSource == null || audioSource.clip == null || isFading || fadeAndPlayNextCoroutine != null)
        {
            // Don't check for track end if source/clip is invalid, currently fading, 
            // or already in the process of starting the next track.
            return; 
        }

        // Check if the track has finished playing (or is very close to finishing)
        // Use !isPlaying as the primary check, but add a time check as a fallback for edge cases.
        bool nearEndOfClip = audioSource.time >= audioSource.clip.length - 0.1f;
        if (!audioSource.isPlaying || nearEndOfClip) 
        {
            // Ensure we don't trigger this *just* after starting playback
            // Check if time is significant enough OR if near end explicitly
            if (audioSource.time > 0.1f || nearEndOfClip) 
            {
                Debug.Log($"Track {audioSource.clip.name} finished or ending. isPlaying={audioSource.isPlaying}, time={audioSource.time}, length={audioSource.clip.length}. Starting next track sequence.");
                fadeAndPlayNextCoroutine = StartCoroutine(FadeAndPlayNextTrack());
            }
        }
    }

    void PlayRandomTrack(bool fadeIn = false)
    {
        if (isFading) // Don't start a new track if currently fading
        {
            Debug.LogWarning("PlayRandomTrack called while fading, ignoring.");
            return;
        }
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
                // Stop existing fade just in case, before starting new one
                if (fadeInCoroutine != null) StopCoroutine(fadeInCoroutine);
                fadeInCoroutine = StartCoroutine(FadeIn());
            }
        } else {
            Debug.LogError($"GameplayMusicPlayer: Invalid track index selected: {currentTrackIndex}", this);
        }
    }

    // --- Fade Implementation ---

    IEnumerator FadeAndPlayNextTrack()
    {
        if (isFading) yield break; // Exit if already fading
        isFading = true; // Set fading flag
        
        Debug.Log("Starting fade out...");
        // Stop existing fade out just in case
        if (fadeOutCoroutine != null) StopCoroutine(fadeOutCoroutine);
        fadeOutCoroutine = StartCoroutine(FadeOut());
        yield return fadeOutCoroutine; // Wait for fade out to complete
        fadeOutCoroutine = null; // Clear reference
        Debug.Log("Fade out complete. Playing next track...");
        
        // Reset fading flag *before* starting the next track which might fade in
        isFading = false; // <<<< RESET FLAG HERE

        PlayRandomTrack(true); // Play next track with fade in (this will start FadeIn coroutine)
        
        // isFading will be set to true again by FadeIn coroutine
        fadeAndPlayNextCoroutine = null; // Allow this coroutine to run again later
    }

    IEnumerator FadeOut()
    {
        // isFading = true; // Flag is set by caller (FadeAndPlayNextTrack)
        
        float startVolume = audioSource.volume; 
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
            yield return null;
        }
        audioSource.volume = 0f;
        audioSource.Stop(); // Stop playback after fading out
        
        // isFading = false; // Let the caller handle overall state
        // fadeOutCoroutine reference is cleared by caller after yield return
    }

    IEnumerator FadeIn()
    {
        // if (isFading) yield break; // Redundant check, handled by PlayRandomTrack
        isFading = true; // Ensure flag is set during fade in
        
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
        
        isFading = false; // Reset fading flag *after* fade in is complete
        fadeInCoroutine = null; // Clear reference
    }
} 