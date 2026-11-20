// Vendored from codegen/emitted-contracts/EntitlementsUpdatedV0.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
// Generated from the published Haiven contract schemas. Do not edit by hand.
//
// The nullability of a presence discriminant is deliberate and is NOT derived
// from `required`.

#nullable enable

namespace Haiven.Sdk.Contracts
{
	/// <summary>A refresh hint that the signed-in account's entitlements changed, sent to its open sockets after the change commits. It carries no grants and authorizes nothing: re-read GET /users/me/entitlements unless that read already returned this projection version or a later one.</summary>
	public sealed class EntitlementsUpdatedV0
	{
		/// <summary>The account's entitlement projection version after the change; it only increases.</summary>
		public int ProjectionVersion { get; set; }
	}
}
