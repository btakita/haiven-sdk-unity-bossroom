// Vendored from codegen/emitted-contracts/WsDroppedFramesV0.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
// Generated from the published Haiven contract schemas. Do not edit by hand.
//
// The nullability of a presence discriminant is deliberate and is NOT derived
// from `required`.

#nullable enable

namespace Haiven.Sdk.Contracts
{
	public sealed class WsDroppedFramesV0TypesItem
	{
		/// <summary>The frame type that was discarded.</summary>
		public string Type { get; set; } = null!;

		/// <summary>How many frames of this type were discarded.</summary>
		public int Count { get; set; }
	}

	/// <summary>A report of the server-originated frames discarded while a session was expired.</summary>
	public sealed class WsDroppedFramesV0
	{
		/// <summary>How many frames were discarded during the expiry episode.</summary>
		public int Count { get; set; }

		/// <summary>The discarded frames grouped by type, so a client can say what was lost rather than only how much.</summary>
		public WsDroppedFramesV0TypesItem[] Types { get; set; } = null!;

		/// <summary>The identifiers of the discarded replies, oldest first.</summary>
		public string[] RequestIds { get; set; } = null!;

		/// <summary>Whether more correlated replies were discarded than the list names.</summary>
		public bool Truncated { get; set; }
	}
}
