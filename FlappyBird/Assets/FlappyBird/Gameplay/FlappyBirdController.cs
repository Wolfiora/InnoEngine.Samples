using System;
using System.Collections.Generic;
using Inno.Canvas;
using InnoEngine.Audio;
using InnoEngine.Core;
using InnoEngine.Input;
using InnoEngine.Reflection;
using InnoEngine.Scene;
using InnoEngine.Serialization;
using InnoEngine.UI;

namespace FlappyBird;

/// <summary>Runs the game from the scene's Canvas, bird, and pipe prefab.</summary>
[StableTypeId("000cb832-8caf-4b9d-a397-97f95e0fd7ac")]
public sealed class FlappyBirdController : GameBehavior
{
    private const float C_GRAVITY = -17f;
    private const float C_FLAP_SPEED = 6.1f;
    private const float C_PIPE_SPEED = 2.35f;
    private const float C_PIPE_SPACING = 3.1f;
    private const float C_PIPE_FIRST_X = 3.8f;
    private const float C_PIPE_LAST_X = C_PIPE_FIRST_X + C_PIPE_SPACING * 3f;

    private readonly List<FlappyBirdPipe> m_pipes = [];
    private readonly Random m_random = new();
    private Canvas? m_canvas;
    private FlappyBirdBird? m_bird;
    private IDisposable? m_canvasEvents;
    private Phase m_phase;
    private float m_velocity;
    private float m_readyClock;
    private float m_spawnClock;
    private int m_score;
    private int m_best;
    private bool m_hudDirty = true;

    /// <summary>Gets or sets the scene bird controlled by this game.</summary>
    [SerializableProperty]
    public GameObject? bird { get; set; }

    /// <summary>Gets or sets the authored pipe pair used for each new obstacle.</summary>
    [SerializableProperty]
    public PrefabAsset? pipePrefab { get; set; }

    /// <summary>Gets or sets the scene hierarchy group that receives pipe instances.</summary>
    [SerializableProperty]
    public GameObject? pipeContainer { get; set; }

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
        m_bird.SetFlight(0.5f, 0f);
        m_phase = Phase.Ready;
        m_hudDirty = true;
    }

    /// <inheritdoc />
    protected override void Update()
    {
        UpdateHud();
        float step = Math.Clamp(Time.deltaTime, 0f, 0.05f);
        bool flap = Input.WasKeyPressed(KeyCode.Space)
            || Input.WasKeyPressed(KeyCode.UpArrow)
            || Input.WasMouseButtonPressed(MouseButton.Left);

        if (m_phase == Phase.Ready)
        {
            m_readyClock += step;
            m_bird!.SetFlight(0.5f + MathF.Sin(m_readyClock * 3f) * 0.12f, 0f);
            if (Input.WasKeyPressed(KeyCode.Space) || Input.WasKeyPressed(KeyCode.UpArrow))
                StartRun();
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
        m_bird.SetFlight(birdY, m_velocity);

        bool collided = birdY < -3.65f || birdY > 5f;
        for (int index = m_pipes.Count - 1; index >= 0; index--)
        {
            FlappyBirdPipe pipe = m_pipes[index];
            pipe.Move(C_PIPE_SPEED * step);
            if (pipe.TryScore(m_bird.x))
            {
                m_score++;
                m_best = Math.Max(m_best, m_score);
                m_hudDirty = true;
                Audio.Play(scoreSound!);
            }
            collided |= pipe.Hits(m_bird.x, birdY);
            if (pipe.x >= -4f)
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
        foreach (FlappyBirdPipe pipe in m_pipes)
        {
            if (pipe.gameObject.isRuntimeValid)
                gameObject.scene.DestroyObject(pipe.gameObject);
        }
        m_pipes.Clear();
        for (int index = 0; index < 4; index++)
            SpawnPipe(C_PIPE_FIRST_X + index * C_PIPE_SPACING,
                index == 0 ? 0.5f : NextGapY());
        m_phase = Phase.Playing;
        m_score = 0;
        m_velocity = C_FLAP_SPEED;
        m_spawnClock = 0f;
        m_bird!.SetFlight(0.5f, m_velocity);
        m_hudDirty = true;
        Audio.Play(startSound!);
        Audio.Play(wingSound!);
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

    private void Die()
    {
        m_phase = Phase.Dead;
        m_velocity = 0f;
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
        m_canvas.SetClass("death-screen", "hidden", m_phase != Phase.Dead);
        m_canvas.SetText("score", m_score.ToString());
        m_canvas.SetText("final-score", m_score.ToString());
        m_canvas.SetText("best-score", m_best.ToString());
        m_hudDirty = false;
    }

    private void OnCanvasEvent(UiEvent uiEvent)
    {
        if (uiEvent.type != UiEventType.Click)
            return;
        if (uiEvent.targetId == "start-button" && m_phase == Phase.Ready
            || uiEvent.targetId == "restart-button" && m_phase == Phase.Dead)
            StartRun();
    }

    private enum Phase
    {
        Ready,
        Playing,
        Dead
    }
}
