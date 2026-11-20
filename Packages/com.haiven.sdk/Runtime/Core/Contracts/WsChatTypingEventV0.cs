// Vendored from codegen/emitted-contracts/WsChatTypingEventV0.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
// Generated from the published Haiven contract schemas. Do not edit by hand.
//
// The nullability of a presence discriminant is deliberate and is NOT derived
// from `required`.

#nullable enable

namespace Haiven.Sdk.Contracts
{
	/// <summary>The payload of a chat typing event.</summary>
	public sealed class WsChatTypingEventV0
	{
		/// <summary>The channel the typing state applies to.</summary>
		public string ChannelId { get; set; } = null!;

		/// <summary>The user whose typing state changed.</summary>
		public string UserId { get; set; } = null!;

		/// <summary>Whether the user is currently typing.</summary>
		public bool IsTyping { get; set; }
	}
}
