// Vendored from codegen/emitted-contracts/WsNotificationReadRequestV0.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
// Generated from the published Haiven contract schemas. Do not edit by hand.
//
// The nullability of a presence discriminant is deliberate and is NOT derived
// from `required`.

#nullable enable

namespace Haiven.Sdk.Contracts
{
	/// <summary>The payload a client sends on a notifications.read frame, marking a whole category read.</summary>
	public sealed class WsNotificationReadRequestV0
	{
		/// <summary>The notification category to mark read, one of chat, user or general.</summary>
		public string Kind { get; set; } = null!;
	}
}
