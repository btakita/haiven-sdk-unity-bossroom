#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Haiven.Sdk;
using Haiven.Sdk.Rest;

namespace Haiven.Demo.BossRoom
{
    /// <summary>
    /// Everything the Boss Room demo does with Haiven, without Unity: chat, and
    /// the player's active item entitlements as inventory. Chat is on the
    /// configured channel, or on the room's own channel while the player is in
    /// a Haiven room. The socket is opened by the caller; every change is handed
    /// to <c>post</c> so the game applies it on its own thread.
    /// </summary>
    public sealed class HaivenDemoClient : IDisposable
    {
        private readonly DemoSettings settings;
        private readonly HaivenSessionCore session;
        private readonly HaivenChat chat = new HaivenChat();
        private readonly IRestApiTransport rest;
        private readonly RestApi api;
        private readonly RewardClaims rewards;
        private readonly Action<Action> post;
        private readonly List<string> items = new List<string>();
        private readonly WsEventRouter? motdRouter;
        private readonly Action<ReceivedFrame>? motdEventHandler;
        private readonly object rewardGate = new object();
        private const long HistoryPageSize = 50;
        private readonly Dictionary<string, string> rewardStatus = new Dictionary<string, string>();
        private readonly HashSet<string> rewardInFlight = new HashSet<string>();
        private readonly HashSet<string> rewardClosed = new HashSet<string>();

        // The rest of these are touched only on the game's thread.
        // The room's chat while the player is in a Haiven room; null outside one.
        private SessionChatChannelFilter? roomChat;
        // The configured channel refused a send with not_channel_member.
        private bool configuredChannelRefused;
        // The history read the log waits for. Changing channel drops it, and a
        // dropped read's answer is ignored.
        private int historyRead;
        private bool historyPending;

        /// <summary>The game socket's session, for the lobby's session operations and signaling.</summary>
        internal HaivenSessionCore Session => session;

        /// <summary>The signed-in REST transport.</summary>
        internal IRestApiTransport Rest => rest;

        /// <summary>The server's id for the game socket, from <c>auth.success</c>; null before it.</summary>
        internal string? ConnectionId { get; private set; }

        public HaivenDemoClient(
            DemoSettings settings,
            IHaivenSocket socket,
            IRestApiTransport rest,
            Func<string> token,
            Action<Action> post,
            DemoMotdController? motd = null)
        {
            this.settings = settings;
            this.post = post;
            this.rest = rest;
            api = new RestApi(rest);
            rewards = new RewardClaims(api);
            session = new HaivenSessionCore(socket, token);
            // The lobby binds sessions to this socket, and the server names it
            // only in auth.success, long before the lobby opens.
            session.FrameReceived += received =>
            {
                if (received.Frame.Type == "auth.success" && received.Frame.Payload != null
                    && received.Frame.Payload.TryGetProperty("connectionId", out var id)
                    && id.Kind == Haiven.Sdk.Json.HaivenJsonKind.String)
                    ConnectionId = id.GetString();
            };
            session.Authenticated += () => post(() =>
            {
                ConnectionStatus = "ONLINE · AUTHENTICATED";
                Log.Note("Haiven chat connected. Press Enter to talk.");
                PresentationChanged?.Invoke();
            });
            // The game service's game-credential route list does not admit
            // GET /chat/channels/{id}/messages, so the browser-handoff credential is
            // refused there. The configured channel's history is read only when the
            // presenter opts in against a backend that answers it. There is no
            // claimed-rewards read: no backend lists claims, so claimed state comes
            // from the claim response. History is anchored before the entitlements
            // read, so an unlock seen first waits for the page and is then placed
            // by grant time.
            if (settings.ChatHistory)
            {
                session.Authenticated += () => post(() =>
                {
                    // In a room the drawer shows the room's channel, which reads its own page.
                    if (roomChat == null) LoadHistory(settings.ChannelId, quietWhenForbidden: false);
                });
            }
            session.Authenticated += () => _ = RefreshEntitlementsAsync();
            session.AuthenticationFailed += reason => post(() =>
            {
                ConnectionStatus = "AUTH FAILED";
                Log.Note(DemoChatLog.SignInUnavailable);
                PresentationChanged?.Invoke();
            });
            session.Closed += (code, reason) => post(() =>
            {
                ConnectionStatus = "OFFLINE · " + code;
                Log.Note("Haiven chat offline (" + code + ").");
                PresentationChanged?.Invoke();
                SocketClosed?.Invoke(code);
            });
            session.EventReceived += received =>
            {
                // The push is a refresh hint: it carries a projection version, not the entitlements.
                if (received.Frame.Type == WsTypes.EntitlementsUpdated) _ = RefreshEntitlementsAsync();
            };
            chat.OutgoingChanged += message => post(() =>
            {
                // An acknowledgement can arrive after the player switches chats,
                // even after they return to the same channel.
                if (Log.HasOutgoing(message.ClientMessageId)) ShowOutgoing(message);
                // The game service lets a game credential reach only the chat of the
                // room it is in, and refuses it the configured channel's community
                // join too, so the refusal is final and nothing is joined.
                if (message.State == OutgoingChatState.Refused
                    && message.Error == "not_channel_member"
                    && message.ChannelId == settings.ChannelId
                    && !configuredChannelRefused)
                {
                    configuredChannelRefused = true;
                    if (roomChat == null) Log.Note(DemoChatLog.ChatOpensInRoom);
                }
            });
            chat.MessageReceived += message => post(() => Log.Incoming(message, ChatChannelId));
            chat.Attach(session);
            if (motd is not null)
            {
                motdRouter = new WsEventRouter();
                motd.Attach(motdRouter);
                motdEventHandler = received =>
                {
                    try { motdRouter.Dispatch(received.Frame); }
                    catch (Exception) { post(() => Log.Note("Message update could not be read.")); }
                };
                session.EventReceived += motdEventHandler;
            }
        }

        public DemoChatLog Log { get; } = new DemoChatLog();

        /// <summary>
        /// The channel the drawer sends to and shows: the room's own channel while
        /// the player is in a Haiven room, the configured channel otherwise.
        /// </summary>
        public string ChatChannelId => roomChat?.ChannelId ?? settings.ChannelId;

        /// <summary>Item entitlement codes granted this session, oldest first.</summary>
        public IReadOnlyList<string> Items => items;

        public string ConnectionStatus { get; private set; } = "CONNECTING";

        public string ModerationStatus { get; private set; } = "READY";

        public string RewardStatus => StatusOf(settings.RewardRef);

        /// <summary>The configured reward can be claimed. A finished claim stays disabled.</summary>
        public bool CanClaimReward => CanClaim(settings.RewardRef);

        /// <summary>Configured reward first, then the extra unclaimed demo rewards.</summary>
        public IReadOnlyList<DemoRewardOffer> RewardOffers
        {
            get
            {
                var specs = DemoRewardCatalog.For(settings.RewardRef);
                var offers = new List<DemoRewardOffer>(specs.Count);
                lock (rewardGate)
                {
                    foreach (var spec in specs)
                    {
                        offers.Add(new DemoRewardOffer(
                            spec.RewardRef,
                            spec.Title,
                            StatusOfLocked(spec.RewardRef),
                            CanClaimLocked(spec.RewardRef)));
                    }
                }
                return offers;
            }
        }

        public event Action? ItemsChanged;

        public event Action? PresentationChanged;

        /// <summary>A reward claim was recorded; raised on the game's thread with the reward ref.</summary>
        public event Action<string>? RewardClaimed;

        /// <summary>
        /// An SDK call failed, named by a <see cref="DemoOperations"/> value; raised on
        /// the game's thread. Refusals the demo presents as state are not failures.
        /// </summary>
        public event Action<string, Exception>? OperationFailed;

        /// <summary>
        /// The realtime socket closed, with the server's close code, raised on the
        /// game's thread. The client does not reopen it: the host decides whether a
        /// drop ends the session or is reconnected while the player stays signed in.
        /// </summary>
        public event Action<int>? SocketClosed;

        /// <summary>Starts authentication; call once the socket is connected.</summary>
        public void Start() => session.Opened();

        public void Send(string content)
        {
            if (content.Trim().Length == 0) return;
            if (roomChat != null)
            {
                ShowOutgoing(roomChat.Send(content));
                return;
            }
            // The configured channel already refused this player for good.
            if (configuredChannelRefused)
            {
                Log.Note(DemoChatLog.ChatOpensInRoom);
                return;
            }
            ShowOutgoing(chat.Send(settings.ChannelId, content));
        }

        private void ShowOutgoing(OutgoingChatMessage message)
        {
            if (message.ChannelId != ChatChannelId) return;
            ModerationStatus = ModerationOf(message);
            Log.Upsert(message);
            PresentationChanged?.Invoke();
        }

        /// <summary>
        /// Moves the drawer onto the chat channel of the room <paramref name="room"/>
        /// is in, through the SDK's <see cref="SessionChatChannelFilter"/>: sends go
        /// there, held while the room resumes on a new socket, and only that
        /// channel's messages are shown. One page of its history is read. Call it on
        /// the game's thread once a create or join has returned, and again for each
        /// new client while the room lasts, after <see cref="HaivenP2PSession.Attach"/>.
        /// Returns false, leaving the drawer as it was, when the room has no chat
        /// channel or the session has ended.
        /// </summary>
        internal bool EnterRoomChat(HaivenP2PSession room)
        {
            var channelId = room.Room?.ChatChannelId;
            if (string.IsNullOrEmpty(channelId)) return false;
            if (roomChat?.ChannelId == channelId) return true;
            SessionChatChannelFilter filter;
            try
            {
                filter = new SessionChatChannelFilter(chat, room);
            }
            catch (InvalidOperationException)
            {
                // The session ended, or is not entered yet.
                return false;
            }
            var previous = roomChat;
            roomChat = filter;
            ResetChatView();
            previous?.Dispose();
            Log.Note(DemoChatLog.RoomChatOpen);
            LoadHistory(filter.ChannelId, quietWhenForbidden: true);
            PresentationChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Takes the drawer off the room's channel when the player leaves the room or
        /// is removed from it. Call it on the game's thread; nothing happens outside a room.
        /// </summary>
        internal void LeaveRoomChat()
        {
            var filter = roomChat;
            if (filter == null) return;
            roomChat = null;
            ResetChatView();
            filter.Dispose();
            Log.Note(DemoChatLog.RoomChatClosed);
            if (settings.ChatHistory) LoadHistory(settings.ChannelId, quietWhenForbidden: false);
            PresentationChanged?.Invoke();
        }

        /// <summary>Claims the reward configured for this presenter session.</summary>
        public Task ClaimRewardAsync(CancellationToken cancellationToken = default) =>
            ClaimRewardAsync(settings.RewardRef, cancellationToken);

        /// <summary>
        /// Claims one demo reward. A terminal result (claimed, already claimed,
        /// not eligible, conflict, or a non-retryable failure) disables that
        /// reward until the next session. Retryable failures stay clickable.
        /// </summary>
        public async Task ClaimRewardAsync(string rewardRef, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(rewardRef) || !TryBeginClaim(rewardRef)) return;
            post(() =>
            {
                lock (rewardGate) rewardStatus[rewardRef] = "CLAIMING…";
                PresentationChanged?.Invoke();
            });

            RewardClaimOutcome outcome;
            try
            {
                outcome = await rewards.ClaimAsync(
                    settings.IntegrationId,
                    rewardRef,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                lock (rewardGate) rewardInFlight.Remove(rewardRef);
                throw;
            }

            var status = RewardStatusOf(outcome);
            post(() =>
            {
                lock (rewardGate)
                {
                    rewardInFlight.Remove(rewardRef);
                    rewardStatus[rewardRef] = status;
                    if (ClosesClaim(status)) rewardClosed.Add(rewardRef);
                }
                Log.Note(outcome.Succeeded
                    ? "reward claimed: " + rewardRef
                    : "reward claim: " + status.ToLowerInvariant());
                PresentationChanged?.Invoke();
                if (outcome.Succeeded) RewardClaimed?.Invoke(rewardRef);
                else if (outcome.Result == RewardClaimResult.Failed && outcome.Exception is not null)
                    OperationFailed?.Invoke(DemoOperations.RewardClaim, outcome.Exception);
            });
        }

        /// <summary>
        /// Reads one page of <paramref name="channelId"/>'s history, newest from the
        /// server, shown oldest first. A failure is a log note: authentication
        /// itself has already succeeded. With <paramref name="quietWhenForbidden"/>
        /// a 403 shows nothing: the game service refuses a game credential this read until
        /// it admits session channels, and the room's live messages work without it.
        /// Runs on the game's thread.
        /// </summary>
        private void LoadHistory(string channelId, bool quietWhenForbidden)
        {
            var read = ++historyRead;
            historyPending = true;
            Log.BeginHistory();
            _ = ReadHistoryAsync(channelId, read, quietWhenForbidden);
        }

        /// <summary>Leaves only global notices when the drawer changes channel.</summary>
        private void ResetChatView()
        {
            ModerationStatus = "READY";
            Log.ClearMessages();
            if (!historyPending) return;
            historyRead++;
            historyPending = false;
            Log.AbandonHistory();
        }

        private async Task ReadHistoryAsync(string channelId, int read, bool quietWhenForbidden)
        {
            FetchMessagesResponse? response;
            try
            {
                response = await api.GetChatChannelsChannelIdMessagesAsync(
                    channelId,
                    limit: HistoryPageSize).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                var quiet = quietWhenForbidden && error is HaivenRestException { StatusCode: 403 };
                post(() =>
                {
                    if (!SettleHistory(read)) return;
                    Log.AbandonHistory();
                    if (quiet) return;
                    Log.Note(DemoChatLog.HistoryUnavailable);
                    OperationFailed?.Invoke(DemoOperations.ChatHistory, error);
                });
                return;
            }

            var messages = response?.Messages;
            post(() =>
            {
                if (SettleHistory(read)) Log.ShowHistory(messages, channelId);
            });
        }

        // True when this is the read the log still waits for.
        private bool SettleHistory(int read)
        {
            if (!historyPending || read != historyRead) return false;
            historyPending = false;
            return true;
        }

        public async Task RefreshEntitlementsAsync()
        {
            AccountEntitlementsResponse? response;
            try
            {
                response = await api.GetUsersMeEntitlementsAsync().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                post(() =>
                {
                    Log.Note(DemoChatLog.EntitlementsUnavailable);
                    OperationFailed?.Invoke(DemoOperations.EntitlementsRead, error);
                });
                return;
            }

            post(() =>
            {
                var granted = EntitlementGrants.NewlyActiveItems(response, items);
                if (granted.Count == 0) return;
                foreach (var grant in granted)
                {
                    items.Add(grant.Code);
                    Log.ShowUnlock(grant.Code, grant.ValidFrom);
                }
                ItemsChanged?.Invoke();
            });
        }

        public void Dispose()
        {
            if (motdEventHandler is not null) session.EventReceived -= motdEventHandler;
            roomChat?.Dispose();
            roomChat = null;
            chat.Dispose();
            session.Dispose();
        }

        private static string ModerationOf(OutgoingChatMessage message)
        {
            switch (message.State)
            {
                case OutgoingChatState.Sending: return "CHECKING";
                case OutgoingChatState.Accepted: return "ALLOWED";
                case OutgoingChatState.Held: return "HELD FOR REVIEW";
                case OutgoingChatState.Removed: return "NOT SHOWN";
                case OutgoingChatState.Refused: return "REFUSED";
                case OutgoingChatState.Unconfirmed: return "WAITING FOR CONNECTION";
                default: return "READY";
            }
        }

        private bool TryBeginClaim(string rewardRef)
        {
            lock (rewardGate)
            {
                if (!CanClaimLocked(rewardRef)) return false;
                rewardInFlight.Add(rewardRef);
                return true;
            }
        }

        private string StatusOf(string rewardRef)
        {
            lock (rewardGate) return StatusOfLocked(rewardRef);
        }

        private string StatusOfLocked(string rewardRef) =>
            rewardStatus.TryGetValue(rewardRef, out var status) ? status : "READY TO CLAIM";

        private bool CanClaim(string rewardRef)
        {
            lock (rewardGate) return CanClaimLocked(rewardRef);
        }

        private bool CanClaimLocked(string rewardRef) =>
            !rewardInFlight.Contains(rewardRef) && !rewardClosed.Contains(rewardRef);

        /// <summary>Retryable failures stay open. Every other finished status disables the row.</summary>
        private static bool ClosesClaim(string status) => status != "RETRY CLAIM";

        private static string RewardStatusOf(RewardClaimOutcome outcome)
        {
            switch (outcome.Result)
            {
                case RewardClaimResult.Claimed: return "CLAIMED";
                case RewardClaimResult.AlreadyClaimed: return "ALREADY CLAIMED";
                case RewardClaimResult.NotEligible: return "NOT ELIGIBLE";
                case RewardClaimResult.RefundInProgress: return "TEMPORARILY UNAVAILABLE";
                case RewardClaimResult.ClaimConflict: return "CLAIM CONFLICT";
                default: return outcome.IsRetryable ? "RETRY CLAIM" : "CLAIM FAILED";
            }
        }
    }
}
