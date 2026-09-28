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

    /// <summary>Places the bird and rotates it to show its current flight direction.</summary>
    /// <param name="height">The vertical world coordinate.</param>
    /// <param name="velocity">The current vertical velocity used to tilt the bird.</param>
    public void SetFlight(float height, float velocity)
    {
        gameObject.transform.localPosition = new Vector3(-1.15f, height, 0.2f);
        (glow ?? throw new InvalidOperationException("The bird glow scene reference is missing."))
            .transform.localPosition = new Vector3(-1.15f, height, 0.5f);
        float tilt = Math.Clamp(velocity * 5f, -75f, 25f);
        gameObject.transform.localRotation = Quaternion.FromEulerAnglesXYZDegrees(
            new Vector3(0f, 0f, tilt));
    }

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
