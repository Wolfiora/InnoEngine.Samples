using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Inno.Canvas;
using Inno.Rendering2D;
using InnoEngine.Audio;
using InnoEngine.Core;
using InnoEngine.Input;
using InnoEngine.Reflection;
using InnoEngine.Scene;
using InnoEngine.Serialization;
using InnoEngine.Storage;
using InnoEngine.UI;

namespace FlappyBird;

/// <summary>Runs the game from the scene's Canvas, bird, and pipe prefab.</summary>
[StableTypeId("000cb832-8caf-4b9d-a397-97f95e0fd7ac")]
public sealed class FlappyBirdController : GameBehavior
{
    private static readonly StorageKey S_BEST_SCORE_KEY = new("flappy-bird/best-score.txt");

    private const float C_GRAVITY = -17f;
    private const float C_FLAP_SPEED = 6.1f;
    private const float C_PIPE_SPEED = 2.35f;
    private const float C_PIPE_SPACING = 3.1f;
    private const float C_PIPE_FIRST_X = 2.7f;
    private const float C_PIPE_LAST_X = C_PIPE_FIRST_X + C_PIPE_SPACING * 3f;
    private const float C_READY_BIRD_X = -4.7f;
    private const float C_PLAY_BIRD_X = -1.15f;
    private const float C_BIRD_HALF_HEIGHT = 0.24f;
    private const float C_DEATH_PANEL_DURATION = 0.8f;
    private const float C_DEATH_PANEL_START = -220f;
    private const float C_DEATH_PANEL_CENTER = 159f;

    private readonly List<FlappyBirdPipe> m_pipes = [];
    private readonly Random m_random = new();
    private Canvas? m_canvas;
    private FlappyBirdBird? m_bird;
    private Camera2D? m_camera;
    private Light2D? m_ambient;
    private IDisposable? m_canvasEvents;
    private Phase m_phase;
    private float m_velocity;
    private float m_readyClock;
    private float m_spawnClock;
    private float m_deathPanelClock;
    private int m_score;
    private int m_best;
    private bool m_hudDirty = true;
    private bool m_night;
    private bool m_suppressPointerFlap;

    /// <summary>Gets or sets the scene bird controlled by this game.</summary>
    [SerializableProperty]
    public GameObject? bird { get; set; }

    /// <summary>Gets or sets the authored pipe pair used for each new obstacle.</summary>
    [SerializableProperty]
    public PrefabAsset? pipePrefab { get; set; }

    /// <summary>Gets or sets the scene hierarchy group that receives pipe instances.</summary>
    [SerializableProperty]
    public GameObject? pipeContainer { get; set; }

    /// <summary>Gets or sets the authored gameplay camera used for obstacle culling.</summary>
    [SerializableProperty]
    public GameObject? camera { get; set; }

    /// <summary>Gets or sets the daytime sky group.</summary>
    [SerializableProperty]
    public GameObject? daySky { get; set; }

    /// <summary>Gets or sets the nighttime sky group.</summary>
    [SerializableProperty]
    public GameObject? nightSky { get; set; }

    /// <summary>Gets or sets the authored global light used for daytime illumination.</summary>
    [SerializableProperty]
    public GameObject? ambientLight { get; set; }

    /// <summary>Gets or sets the authored lights aligned with the night sky stars.</summary>
    [SerializableProperty]
    public GameObject? starLights { get; set; }

    /// <summary>Gets or sets the sound played when the bird flaps.</summary>
    [SerializableProperty]
    public AudioClipAsset? wingSound { get; set; }

    /// <summary>Gets or sets the sound played when a run starts.</summary>
    [SerializableProperty]
    public AudioClipAsset? startSound { get; set; }

    /// <summary>Gets or sets the sound played when the score increases.</summary>
    [SerializableProperty]
    public AudioClipAsset? scoreSound { get; set; }

    /// <summary>Gets or sets the sound played on collision.</summary>
    [SerializableProperty]
    public AudioClipAsset? hitSound { get; set; }

    /// <summary>Gets or sets the sound played after a run ends.</summary>
    [SerializableProperty]
    public AudioClipAsset? dieSound { get; set; }

    /// <inheritdoc />
    protected override void Start()
    {
        m_canvas = gameObject.GetComponent<Canvas>();
        m_bird = (bird ?? throw new InvalidOperationException("The bird scene reference is missing."))
            .GetComponent<FlappyBirdBird>();
        _ = pipePrefab ?? throw new InvalidOperationException("The pipe prefab is missing.");
        _ = pipeContainer ?? throw new InvalidOperationException("The pipe hierarchy group is missing.");
        m_camera = (camera ?? throw new InvalidOperationException("The gameplay camera reference is missing."))
            .GetComponent<Camera2D>();
        _ = daySky ?? throw new InvalidOperationException("The daytime sky group is missing.");
        _ = nightSky ?? throw new InvalidOperationException("The nighttime sky group is missing.");
        _ = starLights ?? throw new InvalidOperationException("The star lights group is missing.");
        m_ambient = (ambientLight ?? throw new InvalidOperationException("The ambient light reference is missing."))
            .GetComponent<Light2D>();
        _ = wingSound ?? throw new InvalidOperationException("The wing sound is missing.");
        _ = startSound ?? throw new InvalidOperationException("The start sound is missing.");
        _ = scoreSound ?? throw new InvalidOperationException("The score sound is missing.");
        _ = hitSound ?? throw new InvalidOperationException("The hit sound is missing.");
        _ = dieSound ?? throw new InvalidOperationException("The death sound is missing.");
        foreach (GameObject sceneObject in gameObject.scene.GetObjects())
        {
            if (sceneObject.TryGetComponent(out FlappyBirdPipe? pipe) && pipe is not null)
                m_pipes.Add(pipe);
        }
        m_canvasEvents = m_canvas.Listen(OnCanvasEvent);
        m_best = LoadBestScore();
        PreparePipes();
        m_bird.SetFlight(C_READY_BIRD_X, 0.5f, 0f);
        m_phase = Phase.Ready;
        m_hudDirty = true;
        SetNightMode(false);
    }

    /// <inheritdoc />
    protected override void Update()
    {
        float step = Math.Clamp(Time.deltaTime, 0f, 0.05f);
        UpdateHud();
        UpdateDeathPanel(step);
        bool pointerPressed = Input.WasMouseButtonPressed(MouseButton.Left);
        bool flap = Input.WasKeyPressed(KeyCode.Space)
            || Input.WasKeyPressed(KeyCode.UpArrow)
            || pointerPressed && !m_suppressPointerFlap;
        if (!Input.IsMouseButtonDown(MouseButton.Left) && !pointerPressed)
            m_suppressPointerFlap = false;

        if (Input.WasKeyPressed(KeyCode.N))
            SetNightMode(!m_night);

        if (m_phase == Phase.Ready)
        {
            m_readyClock += step;
            m_bird!.SetFlight(C_READY_BIRD_X,
                0.5f + MathF.Sin(m_readyClock * 2.6f) * 0.35f, 0f);
            if (Input.WasKeyPressed(KeyCode.Space) || Input.WasKeyPressed(KeyCode.UpArrow))
                StartRun();
            return;
        }

        if (m_phase == Phase.Falling)
        {
            m_velocity += C_GRAVITY * step;
            m_bird!.SetFlight(C_PLAY_BIRD_X, m_bird.y + m_velocity * step, m_velocity);
            float cameraBottom = m_camera!.transform.worldPosition.y - m_camera.orthographicSize;
            if (m_bird.y + C_BIRD_HALF_HEIGHT < cameraBottom)
            {
                m_phase = Phase.Dead;
                m_bird.SetGlowVisible(false);
            }
            return;
        }

        if (m_phase == Phase.Dead)
        {
            if (Input.WasKeyPressed(KeyCode.Space) || Input.WasKeyPressed(KeyCode.Enter))
                StartRun();
            return;
        }

        if (flap)
        {
            m_velocity = C_FLAP_SPEED;
            Audio.Play(wingSound!);
        }
        m_velocity += C_GRAVITY * step;
        float birdY = m_bird!.y + m_velocity * step;
        m_bird.SetFlight(C_PLAY_BIRD_X, birdY, m_velocity);

        bool collided = birdY < -3.65f || birdY > 5f;
        for (int index = m_pipes.Count - 1; index >= 0; index--)
        {
            FlappyBirdPipe pipe = m_pipes[index];
            pipe.Move(C_PIPE_SPEED * step);
            if (pipe.TryScore(m_bird.x))
            {
                m_score++;
                if (m_score > m_best)
                {
                    m_best = m_score;
                    SaveBestScore(m_best);
                }
                m_hudDirty = true;
                Audio.Play(scoreSound!);
            }
            collided |= pipe.Hits(m_bird.x, birdY);
            if (!pipe.IsFullyLeftOf(CameraLeft()))
                continue;
            m_pipes.RemoveAt(index);
            gameObject.scene.DestroyObject(pipe.gameObject);
        }
        if (collided)
        {
            Die();
            return;
        }

        m_spawnClock += step;
        if (m_spawnClock >= C_PIPE_SPACING / C_PIPE_SPEED)
        {
            m_spawnClock -= C_PIPE_SPACING / C_PIPE_SPEED;
            SpawnPipe(C_PIPE_LAST_X, NextGapY());
        }
    }

    /// <inheritdoc />
    protected override void OnDestroy() => m_canvasEvents?.Dispose();

    private void StartRun()
    {
        if (m_phase != Phase.Ready)
            PreparePipes();
        m_phase = Phase.Playing;
        m_score = 0;
        m_velocity = 0f;
        m_spawnClock = 0f;
        m_deathPanelClock = 0f;
        m_canvas!.SetAttribute("death-screen", "style", $"top:{C_DEATH_PANEL_START.ToString(CultureInfo.InvariantCulture)}dp;");
        m_bird!.SetFlight(C_PLAY_BIRD_X, 0.5f, m_velocity);
        m_bird.SetGlowVisible(true);
        m_hudDirty = true;
        Audio.Play(startSound!);
    }

    private void PreparePipes()
    {
        foreach (FlappyBirdPipe pipe in m_pipes)
        {
            if (pipe.gameObject.isRuntimeValid)
                gameObject.scene.DestroyObject(pipe.gameObject);
        }
        m_pipes.Clear();
        for (int index = 0; index < 4; index++)
            SpawnPipe(C_PIPE_FIRST_X + index * C_PIPE_SPACING,
                index == 0 ? 0.5f : NextGapY());
    }

    private void SpawnPipe(float x, float gapY)
    {
        GameObject instance = gameObject.scene.InstantiatePrefab(
            pipePrefab!, pipeContainer!.transform);
        FlappyBirdPipe pipe = instance.GetComponent<FlappyBirdPipe>();
        pipe.Place(x, gapY);
        m_pipes.Add(pipe);
    }

    private float NextGapY() => (float)(m_random.NextDouble() * 2.6 - 0.6);

    private static int LoadBestScore()
    {
        byte[]? bytes = Storage.ReadAsync(S_BEST_SCORE_KEY).GetAwaiter().GetResult();
        if (bytes is null)
            return 0;
        if (int.TryParse(Encoding.UTF8.GetString(bytes), NumberStyles.None,
                CultureInfo.InvariantCulture, out int bestScore))
            return bestScore;
        throw new InvalidDataException("The saved Flappy Bird best score is invalid.");
    }

    private static void SaveBestScore(int bestScore)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(bestScore.ToString(CultureInfo.InvariantCulture));
        Storage.WriteAsync(S_BEST_SCORE_KEY, bytes).GetAwaiter().GetResult();
    }

    private void Die()
    {
        m_phase = Phase.Falling;
        m_velocity = 0f;
        m_deathPanelClock = 0f;
        m_hudDirty = true;
        Audio.Play(hitSound!);
        Audio.Play(dieSound!);
    }

    private void UpdateHud()
    {
        if (!m_hudDirty || m_canvas is not { isReady: true })
            return;
        m_canvas.SetClass("start-screen", "hidden", m_phase != Phase.Ready);
        m_canvas.SetClass("score-hud", "hidden", m_phase != Phase.Playing);
        m_canvas.SetClass("death-screen", "hidden", m_phase is not (Phase.Falling or Phase.Dead));
        m_canvas.SetText("score", m_score.ToString());
        m_canvas.SetText("final-score", m_score.ToString());
        m_canvas.SetText("best-score", m_best.ToString());
        m_canvas.SetClass("moon-icon", "hidden", m_night);
        m_canvas.SetClass("sun-icon", "hidden", !m_night);
        m_hudDirty = false;
    }

    private float CameraLeft()
    {
        Camera2D view = m_camera!;
        return view.transform.worldPosition.x - view.orthographicSize
            * view.referenceResolution.x / view.referenceResolution.y;
    }

    private void UpdateDeathPanel(float step)
    {
        if (m_phase is not (Phase.Falling or Phase.Dead)
            || m_deathPanelClock >= C_DEATH_PANEL_DURATION
            || m_canvas is not { isReady: true })
            return;

        m_deathPanelClock = MathF.Min(C_DEATH_PANEL_DURATION, m_deathPanelClock + step);
        float progress = m_deathPanelClock / C_DEATH_PANEL_DURATION;
        float eased = 1f - (1f - progress) * (1f - progress) * (1f - progress);
        float top = C_DEATH_PANEL_START + (C_DEATH_PANEL_CENTER - C_DEATH_PANEL_START) * eased;
        m_canvas.SetAttribute("death-screen", "style", $"top:{top.ToString("F2", CultureInfo.InvariantCulture)}dp;");
    }

    private void SetNightMode(bool night)
    {
        m_night = night;
        daySky!.SetActive(!night);
        nightSky!.SetActive(night);
        starLights!.SetActive(night);
        m_ambient!.intensity = night ? 0.018f : 1.15f;
        m_bird!.SetNightMode(night);
        m_hudDirty = true;
    }

    private void OnCanvasEvent(UiEvent uiEvent)
    {
        if (uiEvent.type != UiEventType.Click)
            return;
        if (uiEvent.targetId == "mode-button"
            || uiEvent.targetId.StartsWith("moon-", StringComparison.Ordinal)
            || uiEvent.targetId.StartsWith("sun-", StringComparison.Ordinal))
        {
            m_suppressPointerFlap = true;
            SetNightMode(!m_night);
            return;
        }
        if ((uiEvent.targetId is "start-button" or "start-label") && m_phase == Phase.Ready
            || (uiEvent.targetId is "restart-button" or "restart-label") && m_phase == Phase.Dead)
        {
            m_suppressPointerFlap = true;
            StartRun();
        }
    }

    private enum Phase
    {
        Ready,
        Playing,
        Falling,
        Dead
    }
}
