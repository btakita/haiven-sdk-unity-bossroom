// Vendored from codegen/emitted-contracts/P2pSignalAckEventV0.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
// Generated from the published Haiven contract schemas. Do not edit by hand.
//
// The nullability of a presence discriminant is deliberate and is NOT derived
// from `required`.

#nullable enable

namespace Haiven.Sdk.Contracts
{
	/// <summary>An acknowledgement delivered to the member that sent the signaling message.</summary>
	public sealed class P2pSignalAckEventV0
	{
		/// <summary>The session both members belong to.</summary>
		public string SessionId { get; set; } = null!;

		/// <summary>The member that acknowledged as the server authenticated it.</summary>
		public string FromUserId { get; set; } = null!;

		/// <summary>The attempt of the acknowledged message.</summary>
		public string AttemptId { get; set; } = null!;

		/// <summary>The sequence number of the acknowledged message.</summary>
		public int Seq { get; set; }
	}
}
