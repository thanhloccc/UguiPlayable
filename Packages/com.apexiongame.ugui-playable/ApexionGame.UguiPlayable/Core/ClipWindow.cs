using UnityEngine;
using UnityEngine.Timeline;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Where a clip sits on its track, captured when the mixer is created, so the mixer can compute normalized
    /// progress and find finished clips.
    /// </summary>
    internal readonly struct ClipWindow
    {
        public readonly double Start;
        public readonly double End;
        public readonly double ClipIn;
        public readonly double LocalLength;

        public ClipWindow(TimelineClip clip)
        {
            Start = clip.start;
            End = clip.end;
            ClipIn = clip.clipIn;
            LocalLength = clip.duration * clip.timeScale;
        }

        /// <summary>
        /// Maps the input playable's local time to [0, 1]. Timeline sets the local time to
        /// <c>(time - start) * timeScale + clipIn</c> and applies extrapolation, so this stays correct for
        /// clip-in, speed, loop and ping-pong; the playable's own duration includes extrapolation and is not used.
        /// </summary>
        public float ToProgress(double localTime)
        {
            if (LocalLength <= 0d)
            {
                return 1f;
            }

            return Mathf.Clamp01((float)((localTime - ClipIn) / LocalLength));
        }
    }
}
