using System;
using System.Threading.Tasks;
using UnityEngine;

namespace WonderJump.Net
{
    /// <summary>How this device takes part in the current game.</summary>
    public enum SessionRole
    {
        /// <summary>Single player. No network, no Unity Services sign-in required.</summary>
        Offline = 0,
        /// <summary>Co-op host: authoritative for shared gameplay events.</summary>
        Host = 1,
        /// <summary>Co-op client: sends requests, renders replicated state.</summary>
        Client = 2
    }

    public interface IAuthorityContext
    {
        SessionRole Role { get; }
        /// <summary>
        /// True when this device decides shared outcomes: collectibles, enemy AI and damage,
        /// checkpoints, level completion. True for Offline and Host.
        /// </summary>
        bool HasSharedStateAuthority { get; }
    }

    public sealed class FixedAuthorityContext : IAuthorityContext
    {
        private readonly SessionRole role;

        public FixedAuthorityContext(SessionRole role)
        {
            this.role = role;
        }

        public SessionRole Role { get { return role; } }
        public bool HasSharedStateAuthority { get { return role != SessionRole.Client; } }
    }

    /// <summary>
    /// Global authority switch. Defaults to Offline, so every system works with no network.
    /// Phase 2: the NGO session service sets Host/Client when a session starts and resets on shutdown.
    /// </summary>
    public static class GameAuthority
    {
        private static IAuthorityContext current = new FixedAuthorityContext(SessionRole.Offline);

        public static IAuthorityContext Current { get { return current; } }

        public static void Set(IAuthorityContext context)
        {
            current = context ?? new FixedAuthorityContext(SessionRole.Offline);
        }

        public static void ResetToOffline()
        {
            current = new FixedAuthorityContext(SessionRole.Offline);
        }

        // Keeps behaviour correct when "Enter Play Mode Options" disables domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            ResetToOffline();
        }
    }

    /// <summary>
    /// Session seam over Netcode for GameObjects + Unity Multiplayer Services (Sessions/Relay).
    /// Phase 2 implementation. Gameplay code depends on this interface only, never on NGO types.
    /// </summary>
    public interface INetworkSessionService
    {
        SessionRole Role { get; }
        bool IsRunning { get; }
        /// <summary>Join code while hosting; null otherwise.</summary>
        string JoinCode { get; }

        /// <summary>Starts single player as a local offline host. Must succeed with no internet.</summary>
        void StartOffline();

        /// <summary>Signs in anonymously, creates a 2-player session over Relay, returns the join code.</summary>
        Task<string> StartHostAsync(int maxPlayers);

        Task JoinAsync(string joinCode);

        void Shutdown();

        event Action<SessionRole> RoleChanged;
        /// <summary>Reason text, e.g. "Host left the game".</summary>
        event Action<string> Disconnected;
    }
}
