// Vendored from codegen/emitted-contracts/SessionResumeReplyV0.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
// Generated from the published Haiven contract schemas. Do not edit by hand.
//
// The nullability of a presence discriminant is deliberate and is NOT derived
// from `required`.

#nullable enable

namespace Haiven.Sdk.Contracts
{
	/// <summary>The socket a resumed participation is now bound to.</summary>
	public sealed class SessionResumeReplyV0
	{
		/// <summary>The resumed participation.</summary>
		public string ParticipationId { get; set; } = null!;

		/// <summary>The server-generated WebSocket connection identifier.</summary>
		public string ConnectionId { get; set; } = null!;
	}
}
