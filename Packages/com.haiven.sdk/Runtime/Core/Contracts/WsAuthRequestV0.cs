// Vendored from codegen/emitted-contracts/WsAuthRequestV0.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
// Generated from the published Haiven contract schemas. Do not edit by hand.
//
// The nullability of a presence discriminant is deliberate and is NOT derived
// from `required`.

#nullable enable

namespace Haiven.Sdk.Contracts
{
	public sealed class WsAuthRequestV0Sdk
	{
		/// <summary>The SDK distribution name identifying the transport layer sending the frame.</summary>
		public string Name { get; set; } = null!;

		/// <summary>The version of the SDK distribution named by name.</summary>
		public string Version { get; set; } = null!;
	}

	/// <summary>The payload a client sends on an auth frame, re-authenticating an open socket.</summary>
	public sealed class WsAuthRequestV0
	{
		/// <summary>The replacement bearer token; it must resolve to the same identity the socket already authenticated with.</summary>
		public string Token { get; set; } = null!;

		/// <summary>Identifies the SDK transport layer sending the frame; observability metadata and never an authentication input.</summary>
		public WsAuthRequestV0Sdk? Sdk { get; set; }
	}
}
