// Vendored from codegen/emitted-contracts/ChatCommunityDeletedV0.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
// Generated from the published Haiven contract schemas. Do not edit by hand.
//
// The nullability of a presence discriminant is deliberate and is NOT derived
// from `required`.

#nullable enable

namespace Haiven.Sdk.Contracts
{
	/// <summary>A chat community its owner deleted; the last event it sends before its subscribers are dropped.</summary>
	public sealed class ChatCommunityDeletedV0
	{
		/// <summary>The chat community identifier.</summary>
		public string CommunityId { get; set; } = null!;
	}
}
