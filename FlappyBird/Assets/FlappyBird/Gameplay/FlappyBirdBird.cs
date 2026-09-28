using System;
using Inno.Rendering2D;
using InnoEngine.Core;
using InnoEngine.Mathematics;
using InnoEngine.Reflection;
using InnoEngine.Scene;
using InnoEngine.Serialization;

namespace FlappyBird;

/// <summary>Animates the serialized bird sprites and follows its scene light.</summary>
[StableTypeId("ebd5a563-5fd2-421c-99b9-517fa1ee3781")]
public sealed class FlappyBirdBird : GameBehavior
{
    private float m_animationClock;
    private SpriteRenderer2D? m_renderer;

    /// <summary>Gets or sets the imported sprite frames in flap order.</summary>
    [SerializableProperty]
    public SpriteReference2D[] frames { get; set; } = [];

    /// <summary>Gets or sets the scene light that follows the bird.</summary>
    [SerializableProperty]
    public GameObject? glow { get; set; }

    /// <summary>Gets the bird's horizontal world position.</summary>
    public float x => gameObject.transform.worldPosition.x;

    /// <summary>Gets the bird's vertical world position.</summary>
    public float y => gameObject.transform.worldPosition.y;

    /// <summary>Places the bird and its light while showing the current flight direction.</summary>
    /// <param name="horizontalPosition">The horizontal world coordinate.</param>
    /// <param name="height">The vertical world coordinate.</param>
    /// <param name="velocity">The current vertical velocity used to tilt the bird.</param>
    public void SetFlight(float horizontalPosition, float height, float velocity)
    {
        gameObject.transform.localPosition = new Vector3(horizontalPosition, height, 0.2f);
        (glow ?? throw new InvalidOperationException("The bird glow scene reference is missing."))
            .transform.localPosition = new Vector3(horizontalPosition, height, 0.5f);
        float tilt = Math.Clamp(velocity * 5f, -75f, 25f);
        gameObject.transform.localRotation = Quaternion.FromEulerAnglesXYZDegrees(
            new Vector3(0f, 0f, tilt));
    }

    /// <summary>Adjusts the authored bird light for the active time of day.</summary>
    /// <param name="night">Whether the night lighting is active.</param>
    public void SetNightMode(bool night)
    {
        Light2D light = (glow ?? throw new InvalidOperationException("The bird glow scene reference is missing."))
            .GetComponent<Light2D>();
        light.intensity = night ? 3.5f : 1.8f;
        light.range = night ? 2.7f : 2.3f;
    }

    /// <summary>Shows the bird light while the bird is inside the camera view.</summary>
    /// <param name="visible">Whether the bird light should illuminate the scene.</param>
    public void SetGlowVisible(bool visible)
        => (glow ?? throw new InvalidOperationException("The bird glow scene reference is missing."))
            .SetActive(visible);

    /// <inheritdoc />
    protected override void Start()
    {
        m_renderer = gameObject.GetComponent<SpriteRenderer2D>();
        if (frames.Length == 0)
            throw new InvalidOperationException("The bird has no serialized sprite frames.");
    }

    /// <inheritdoc />
    protected override void Update()
    {
        m_animationClock += Math.Clamp(Time.deltaTime, 0f, 0.05f);
        int frame = (int)(m_animationClock * 9f) % frames.Length;
        m_renderer!.sprite = frames[frame];
    }
}
