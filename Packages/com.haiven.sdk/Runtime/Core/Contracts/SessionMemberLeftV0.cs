// Vendored from codegen/emitted-contracts/SessionMemberLeftV0.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
// Generated from the published Haiven contract schemas. Do not edit by hand.
//
// The nullability of a presence discriminant is deliberate and is NOT derived
// from `required`.

#nullable enable

namespace Haiven.Sdk.Contracts
{
	/// <summary>A member's participation ended; the host closes its connection to that member.</summary>
	public sealed class SessionMemberLeftV0
	{
		/// <summary>The session.</summary>
		public string SessionId { get; set; } = null!;

		/// <summary>The member whose participation ended.</summary>
		public string UserId { get; set; } = null!;

		/// <summary>The participation that ended.</summary>
		public string ParticipationId { get; set; } = null!;

		/// <summary>Why the participation ended.</summary>
		public string Reason { get; set; } = null!;
	}
}
