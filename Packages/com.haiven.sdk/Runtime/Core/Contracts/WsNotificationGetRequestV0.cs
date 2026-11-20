// Vendored from codegen/emitted-contracts/WsNotificationGetRequestV0.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
// Generated from the published Haiven contract schemas. Do not edit by hand.
//
// The nullability of a presence discriminant is deliberate and is NOT derived
// from `required`.

#nullable enable

namespace Haiven.Sdk.Contracts
{
	/// <summary>The payload a client sends on a notifications.get frame, selecting one notification.</summary>
	public sealed class WsNotificationGetRequestV0
	{
		/// <summary>The notification to fetch.</summary>
		public string Id { get; set; } = null!;
	}
}
