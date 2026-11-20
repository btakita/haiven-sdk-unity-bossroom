// Vendored from codegen/emitted-contracts/NotificationGetRequestV0.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
// Generated from the published Haiven contract schemas. Do not edit by hand.
//
// The nullability of a presence discriminant is deliberate and is NOT derived
// from `required`.

#nullable enable

namespace Haiven.Sdk.Contracts
{
	/// <summary>A notification lookup request.</summary>
	public sealed class NotificationGetRequestV0
	{
		/// <summary>The stable identifier of this value.</summary>
		public string Id { get; set; } = null!;
	}
}
