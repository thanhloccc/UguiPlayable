namespace ApexionGame.UguiPlayable.Easing
{
    /// <summary>
    /// The easing presets, plus <see cref="Custom"/> for a user-authored curve.
    /// </summary>
    /// <remarks>
    /// The integer values are serialized in user data: never renumber a member; add new members at the end with the
    /// next explicit value.
    /// </remarks>
    public enum Ease
    {
        /// <summary>No easing, constant speed.</summary>
        Linear = 0,

        /// <summary>Sine, accelerating.</summary>
        InSine = 1,

        /// <summary>Sine, decelerating.</summary>
        OutSine = 2,

        /// <summary>Sine, accelerating then decelerating.</summary>
        InOutSine = 3,

        /// <summary>Quadratic, accelerating.</summary>
        InQuad = 4,

        /// <summary>Quadratic, decelerating.</summary>
        OutQuad = 5,

        /// <summary>Quadratic, accelerating then decelerating.</summary>
        InOutQuad = 6,

        /// <summary>Cubic, accelerating.</summary>
        InCubic = 7,

        /// <summary>Cubic, decelerating.</summary>
        OutCubic = 8,

        /// <summary>Cubic, accelerating then decelerating.</summary>
        InOutCubic = 9,

        /// <summary>Quartic, accelerating.</summary>
        InQuart = 10,

        /// <summary>Quartic, decelerating.</summary>
        OutQuart = 11,

        /// <summary>Quartic, accelerating then decelerating.</summary>
        InOutQuart = 12,

        /// <summary>Quintic, accelerating.</summary>
        InQuint = 13,

        /// <summary>Quintic, decelerating.</summary>
        OutQuint = 14,

        /// <summary>Quintic, accelerating then decelerating.</summary>
        InOutQuint = 15,

        /// <summary>Exponential, accelerating.</summary>
        InExpo = 16,

        /// <summary>Exponential, decelerating.</summary>
        OutExpo = 17,

        /// <summary>Exponential, accelerating then decelerating.</summary>
        InOutExpo = 18,

        /// <summary>Circular, accelerating.</summary>
        InCirc = 19,

        /// <summary>Circular, decelerating.</summary>
        OutCirc = 20,

        /// <summary>Circular, accelerating then decelerating.</summary>
        InOutCirc = 21,

        /// <summary>Elastic, spring-like start.</summary>
        InElastic = 22,

        /// <summary>Elastic, spring-like finish.</summary>
        OutElastic = 23,

        /// <summary>Elastic, spring-like start and finish.</summary>
        InOutElastic = 24,

        /// <summary>Pulls back before moving forward.</summary>
        InBack = 25,

        /// <summary>Overshoots the end, then settles.</summary>
        OutBack = 26,

        /// <summary>Pulls back at the start and overshoots at the end.</summary>
        InOutBack = 27,

        /// <summary>Bounces at the start.</summary>
        InBounce = 28,

        /// <summary>Bounces at the end.</summary>
        OutBounce = 29,

        /// <summary>Bounces at the start and the end.</summary>
        InOutBounce = 30,

        /// <summary>Uses the curve stored in <see cref="EaseSetting"/>; it has no formula.</summary>
        Custom = 31,
    }
}
