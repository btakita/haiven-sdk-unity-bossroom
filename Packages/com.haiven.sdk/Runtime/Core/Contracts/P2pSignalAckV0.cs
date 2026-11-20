// Vendored from codegen/emitted-contracts/P2pSignalAckV0.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
// Generated from the published Haiven contract schemas. Do not edit by hand.
//
// The nullability of a presence discriminant is deliberate and is NOT derived
// from `required`.

#nullable enable

namespace Haiven.Sdk.Contracts
{
	/// <summary>A recipient's acknowledgement of one signaling message.</summary>
	public sealed class P2pSignalAckV0
	{
		/// <summary>The session both members belong to.</summary>
		public string SessionId { get; set; } = null!;

		/// <summary>The original sender of the acknowledged message.</summary>
		public string ToUserId { get; set; } = null!;

		/// <summary>The attempt of the acknowledged message.</summary>
		public string AttemptId { get; set; } = null!;

		/// <summary>The sequence number of the acknowledged message.</summary>
		public int Seq { get; set; }
	}
}
