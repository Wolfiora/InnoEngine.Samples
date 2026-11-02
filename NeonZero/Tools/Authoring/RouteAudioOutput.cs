using System.Collections.Generic;
using Inno.VisualNovel.Player.Ports;

namespace Story.Tools.Authoring;

internal sealed class RouteAudioOutput : INovelAudioOutput
{
    private readonly Dictionary<long, (double gain, bool paused)> m_voices = [];
    private long m_next;

    /// <inheritdoc />
    public long Play(
        string cueId,
        double gain,
        bool loop,
        double position
    ) {
        long token = ++m_next; m_voices.Add(token, (gain, false)); return token;
    }
    /// <inheritdoc />
    public void Stop(long token) => m_voices.Remove(token);
    /// <inheritdoc />
    public void SetGain(
        long token,
        double gain
    ) {
        if (m_voices.TryGetValue(token, out var voice)) m_voices[token] = (gain, voice.paused);
    }
    /// <inheritdoc />
    public void SetPaused(
        long token,
        bool paused
    ) {
        if (m_voices.TryGetValue(token, out var voice)) m_voices[token] = (voice.gain, paused);
    }
    /// <inheritdoc />
    public bool IsActive(long token) => m_voices.ContainsKey(token);
}
