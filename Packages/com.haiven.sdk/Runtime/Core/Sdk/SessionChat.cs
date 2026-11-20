// Vendored from sdks/dotnet/Haiven.Sdk/SessionChat.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
#nullable enable
using System;
using Haiven.Sdk.Contracts;

namespace Haiven.Sdk
{
    /// <summary>
    /// A session-scoped view of headless chat. It sends on the admission's
    /// moderated channel and drops events for every other channel before the
    /// game sees them. It marks that channel open with <c>chat.channel.active</c>,
    /// because the server delivers <c>chat.typing</c> only to the users who have
    /// the channel open.
    ///
    /// Built from a <see cref="HaivenP2PSession"/>, it also holds sends on the
    /// channel while the participation is not on the chat's socket. After a drop,
    /// the game service keeps the participation, and with it the channel membership, on
    /// the old connection until <c>session.resume</c> moves it. A send that reached
    /// the new socket first would be refused with <c>not_channel_member</c> for
    /// good, so it waits as <see cref="OutgoingChatState.Unconfirmed"/> and goes
    /// out on <see cref="HaivenP2PSession.Resumed"/>.
    /// </summary>
    public sealed class SessionChatChannelFilter : IDisposable
    {
        private readonly HaivenChat chat;
        private readonly HaivenP2PSession? p2p;
        private readonly Func<HaivenSessionCore, bool>? mayWrite;
        private bool disposed;

        /// <summary>
        /// Filters <paramref name="chat"/> to the channel of the session
        /// <paramref name="session"/> is in, holds sends on it during a resume, and
        /// marks that channel open again each time the participation resumes on a
        /// new socket. Attach the same game sockets to both, as each one replaces
        /// the last.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="session"/> is not in a session: it is idle, or its create
        /// or join has not returned yet.
        /// </exception>
        /// <exception cref="ArgumentException">The admission has no chat channel id.</exception>
        /// <remarks>
        /// Attach <paramref name="chat"/> to the same <see cref="HaivenSessionCore"/>
        /// given to <see cref="HaivenP2PSession.Attach"/>, including after a reconnect.
        /// The server refuses session-channel requests from any other socket with
        /// <c>not_channel_member</c>.
        /// </remarks>
        public SessionChatChannelFilter(HaivenChat chat, HaivenP2PSession session)
            : this(chat, RoomOf(session))
        {
            p2p = session;
            var room = session.Room;
            // Write on the socket the participation is bound to, or anywhere once
            // this session is over, when a send gets the server's final answer.
            // That socket may be re-authenticating after auth.expired: the core
            // queues the frame until auth.success, and the chat already holds
            // sends from a closed core.
            mayWrite = core => session.Room != room
                || (session.State == HaivenP2PState.InSession && session.Channel?.Session == core);
            chat.Gate(ChannelId, mayWrite);
            session.Resumed += OnResumed;
            session.Ended += OnEnded;
        }

        /// <summary>
        /// Filters <paramref name="chat"/> to the channel of a session admission.
        /// Sends do not wait for a resume; build the filter from the
        /// <see cref="HaivenP2PSession"/> for that.
        /// </summary>
        /// <exception cref="ArgumentException">The admission has no chat channel id.</exception>
        /// <remarks>
        /// Attach <paramref name="chat"/> to the same <see cref="HaivenSessionCore"/>
        /// given to <see cref="HaivenP2PSession.Attach"/>. The server refuses
        /// session-channel sends from any other socket with <c>not_channel_member</c>.
        /// </remarks>
        public SessionChatChannelFilter(HaivenChat chat, HaivenSessionAdmission admission)
            : this(chat, admission?.ChatChannelId ?? throw new ArgumentNullException(nameof(admission)))
        {
        }

        /// <summary>Filters <paramref name="chat"/> to <paramref name="channelId"/>.</summary>
        /// <exception cref="ArgumentException"><paramref name="channelId"/> is empty.</exception>
        /// <remarks>
        /// Attach <paramref name="chat"/> to the same <see cref="HaivenSessionCore"/>
        /// given to <see cref="HaivenP2PSession.Attach"/>. The server refuses
        /// session-channel sends from any other socket with <c>not_channel_member</c>.
        /// </remarks>
        public SessionChatChannelFilter(HaivenChat chat, string channelId)
        {
            this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
            if (string.IsNullOrEmpty(channelId))
                throw new ArgumentException("haiven: the session chatChannelId is required", nameof(channelId));
            ChannelId = channelId;
            chat.MessageReceived += OnMessageReceived;
            chat.MessageUpdated += OnMessageUpdated;
            chat.MessageDeleted += OnMessageDeleted;
            chat.Typing += OnTyping;
            chat.SetActiveChannel(channelId);
        }

        /// <summary>The channel returned by the current session admission.</summary>
        public string ChannelId { get; }

        /// <summary>A new message on this session's channel.</summary>
        public event Action<ChatMessageV0>? MessageReceived;

        /// <summary>An edited message on this session's channel.</summary>
        public event Action<ChatMessageV0>? MessageUpdated;

        /// <summary>A deleted message on this session's channel.</summary>
        public event Action<ChatMessageV0>? MessageDeleted;

        /// <summary>A typing update on this session's channel.</summary>
        public event Action<ChatTypingV0>? Typing;

        /// <summary>Sends a message to this session's channel.</summary>
        public OutgoingChatMessage Send(string content, string? parentMessageId = null)
        {
            ThrowIfDisposed();
            return chat.Send(ChannelId, content, parentMessageId);
        }

        /// <summary>Sends typing state to this session's channel.</summary>
        public void SendTyping()
        {
            ThrowIfDisposed();
            chat.SendTyping(ChannelId);
        }

        /// <summary>
        /// Stops listening to the chat, and to the P2P session when it was built
        /// from one; its hold on held sends is released, and another filter's stays.
        /// It stops marking the channel open on later sessions. Sends throw after this.
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            chat.MessageReceived -= OnMessageReceived;
            chat.MessageUpdated -= OnMessageUpdated;
            chat.MessageDeleted -= OnMessageDeleted;
            chat.Typing -= OnTyping;
            chat.ClearActiveChannel(ChannelId);
            if (p2p == null || mayWrite == null) return;
            p2p.Resumed -= OnResumed;
            p2p.Ended -= OnEnded;
            chat.Ungate(ChannelId, mayWrite);
            // Nothing holds them now; they go out on this socket or the next attach.
            chat.Resend(ChannelId);
        }

        // Admitting sets Room before the session is entered, and entering raises
        // no event that would release a held send, so the filter waits for it.
        private static HaivenSessionAdmission RoomOf(HaivenP2PSession session)
        {
            if (session is null) throw new ArgumentNullException(nameof(session));
            if (session.State != HaivenP2PState.InSession && session.State != HaivenP2PState.Resuming)
                throw new InvalidOperationException("haiven: the P2P session is not in a session yet");
            return session.Room ?? throw new InvalidOperationException("haiven: the P2P session has no room yet");
        }

        // The server forgot the mark with the old socket and refused it from the
        // new one until now, when the participation moved to it.
        private void OnResumed()
        {
            if (p2p!.Room?.ChatChannelId == ChannelId) chat.SetActiveChannel(ChannelId);
            chat.Resend(ChannelId);
        }

        private void OnEnded(HaivenP2PEndReason reason, string? code) => chat.Resend(ChannelId);

        private void OnMessageReceived(ChatMessageV0 message)
        {
            if (message.ChannelId == ChannelId) MessageReceived?.Invoke(message);
        }

        private void OnMessageUpdated(ChatMessageV0 message)
        {
            if (message.ChannelId == ChannelId) MessageUpdated?.Invoke(message);
        }

        private void OnMessageDeleted(ChatMessageV0 message)
        {
            if (message.ChannelId == ChannelId) MessageDeleted?.Invoke(message);
        }

        private void OnTyping(ChatTypingV0 typing)
        {
            if (typing.ChannelId == ChannelId) Typing?.Invoke(typing);
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(SessionChatChannelFilter));
        }
    }
}
