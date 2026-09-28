using System;
using InnoEngine.Mathematics;
using InnoEngine.Reflection;
using InnoEngine.Scene;
using InnoEngine.Serialization;

namespace FlappyBird;

/// <summary>Controls one prefab instance containing an upper and lower pipe.</summary>
[StableTypeId("d3140c98-f0e1-4820-b750-51fb6f1c2c3e")]
public sealed class FlappyBirdPipe : GameBehavior
{
    private const float C_HALF_GAP = 1.32f;
    private const float C_HALF_PIPE_HEIGHT = 3.2f;

    private bool m_scored;
    private float m_gapY;

    /// <summary>Gets or sets the upper pipe child authored in the prefab.</summary>
    [SerializableProperty]
    public GameObject? upper { get; set; }

    /// <summary>Gets or sets the lower pipe child authored in the prefab.</summary>
    [SerializableProperty]
    public GameObject? lower { get; set; }

    /// <summary>Gets the pair's horizontal position relative to its scene group.</summary>
    public float x => gameObject.transform.localPosition.x;

    /// <summary>Places the pair and opens its gap around the requested height.</summary>
    /// <param name="horizontalPosition">The horizontal coordinate inside the pipe group.</param>
    /// <param name="gapHeight">The vertical center of the traversable gap.</param>
    public void Place(float horizontalPosition, float gapHeight)
    {
        GameObject top = upper ?? throw new InvalidOperationException("The pipe prefab has no upper child reference.");
        GameObject bottom = lower ?? throw new InvalidOperationException("The pipe prefab has no lower child reference.");
        gameObject.transform.localPosition = new Vector3(horizontalPosition, 0f, 0f);
        top.transform.localPosition = new Vector3(0f, gapHeight + C_HALF_GAP + C_HALF_PIPE_HEIGHT, 0f);
        bottom.transform.localPosition = new Vector3(0f, gapHeight - C_HALF_GAP - C_HALF_PIPE_HEIGHT, 0f);
        m_gapY = gapHeight;
        m_scored = false;
    }

    /// <summary>Moves this prefab instance toward the bird.</summary>
    /// <param name="distance">The nonnegative distance to move left.</param>
    public void Move(float distance)
    {
        gameObject.transform.localPosition = new Vector3(x - distance, 0f, 0f);
    }

    /// <summary>Reports one score after the bird has fully passed this pair.</summary>
    /// <param name="birdX">The bird's horizontal coordinate.</param>
    /// <returns>True exactly once when the pair is passed.</returns>
    public bool TryScore(float birdX)
    {
        if (m_scored || x + 0.52f >= birdX)
            return false;
        m_scored = true;
        return true;
    }

    /// <summary>Checks whether the bird overlaps either pipe in this pair.</summary>
    /// <param name="birdX">The bird's horizontal coordinate.</param>
    /// <param name="birdY">The bird's vertical coordinate.</param>
    /// <returns>True when the bird intersects a pipe.</returns>
    public bool Hits(float birdX, float birdY)
        => MathF.Abs(x - birdX) < 0.74f
           && (birdY + 0.19f > m_gapY + C_HALF_GAP
               || birdY - 0.19f < m_gapY - C_HALF_GAP);
}
