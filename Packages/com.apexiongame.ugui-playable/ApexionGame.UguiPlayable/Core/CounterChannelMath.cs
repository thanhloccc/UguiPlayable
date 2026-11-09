namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Evaluates a <see cref="CounterChannel"/> without allocation.
    /// </summary>
    internal static class CounterChannelMath
    {
        /// <summary>
        /// The value a count starts from: the captured value in From current, otherwise <c>From</c>. The text is
        /// never read.
        /// </summary>
        public static double StartOf(in CounterChannel channel)
            => channel.Mode == ValueMode.FromCurrent && channel.HasCaptured ? channel.Captured : channel.From;

        /// <summary>
        /// Returns the value at the normalized clip <paramref name="progress"/>; exactly <c>To</c> at progress 1.
        /// </summary>
        /// <param name="channel">The channel to evaluate.</param>
        /// <param name="progress">The clip progress, 0 to 1.</param>
        /// <param name="start">The start of the count, see <see cref="StartOf"/>.</param>
        public static double Evaluate(in CounterChannel channel, float progress, double start)
        {
            if (progress >= 1f)
            {
                return channel.To;
            }

            return start + (channel.To - start) * channel.Ease.Evaluate(progress);
        }
    }
}
