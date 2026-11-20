// Vendored from codegen/emitted-contracts/ChatTypingV0.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
// Generated from the published Haiven contract schemas. Do not edit by hand.
//
// The nullability of a presence discriminant is deliberate and is NOT derived
// from `required`.

#nullable enable

namespace Haiven.Sdk.Contracts
{
	/// <summary>A user's typing state in a chat channel.</summary>
	public sealed class ChatTypingV0
	{
		/// <summary>The chat channel identifier.</summary>
		public string ChannelId { get; set; } = null!;

		/// <summary>The user identifier.</summary>
		public string? UserId { get; set; }

		/// <summary>Whether the user is currently typing in the channel.</summary>
		public bool IsTyping { get; set; }
	}
}
