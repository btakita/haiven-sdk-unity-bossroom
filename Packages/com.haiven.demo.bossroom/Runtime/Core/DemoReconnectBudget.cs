#nullable enable
using System;
using Haiven.Sdk;

namespace Haiven.Demo.BossRoom
{
    /// <summary>
    /// The attempts left to reopen a dropped realtime socket. An open socket
    /// does not start them over: the server can accept it and close it again
    /// before it accepts the credential (for example 1013 auth_unavailable
    /// while its auth backend is down), so that close uses the next attempt.
    /// Only a session that authenticates, or a new sign-in, starts them over.
    /// Used on the game's thread only.
    /// </summary>
    internal sealed class DemoReconnectBudget
    {
        public DemoReconnectBudget(int attempts)
        {
            if (attempts < 1) throw new ArgumentOutOfRangeException(nameof(attempts));
            Attempts = attempts;
        }

        /// <summary>How many attempts there are between two resets.</summary>
        public int Attempts { get; }

        /// <summary>The attempts taken since the last reset.</summary>
        public int Used { get; private set; }

        /// <summary>
        /// Takes the next attempt, numbered from 1, or returns false once every
        /// attempt is used.
        /// </summary>
        public bool TryTake(out int attempt)
        {
            if (Used >= Attempts)
            {
                attempt = 0;
                return false;
            }
            attempt = ++Used;
            return true;
        }

        /// <summary>Starts the attempts over.</summary>
        public void Reset() => Used = 0;

        /// <summary>
        /// Starts the attempts over once <paramref name="session"/> authenticates.
        /// The reset is posted to the game's thread, and is dropped there unless
        /// <paramref name="isCurrent"/> still holds, so a replaced client cannot
        /// reset the attempts of the one after it.
        /// </summary>
        public void ResetWhenAuthenticated(
            HaivenSessionCore session,
            Action<Action> post,
            Func<bool> isCurrent)
        {
            session.Authenticated += () => post(() =>
            {
                if (isCurrent()) Reset();
            });
        }
    }
}
