// Vendored from codegen/emitted-contracts/P2pIceServersReplyV0.cs by scripts/vendor-unity.py.
// Edit or regenerate the source, then run `make vendor-unity`.
// Generated from the published Haiven contract schemas. Do not edit by hand.
//
// The nullability of a presence discriminant is deliberate and is NOT derived
// from `required`.

#nullable enable

namespace Haiven.Sdk.Contracts
{
	public sealed class P2pIceServersReplyV0IceServersItem
	{
		/// <summary>STUN and TURN URLs of one server.</summary>
		public string[] Urls { get; set; } = null!;

		/// <summary>The TURN username: absent for STUN.</summary>
		public string? Username { get; set; }

		/// <summary>The TURN password: absent for STUN.</summary>
		public string? Credential { get; set; }
	}

	/// <summary>STUN and TURN servers for the caller's session participation, with when to fetch them again.</summary>
	public sealed class P2pIceServersReplyV0
	{
		/// <summary>Whether direct candidates are allowed: auto allows them and relay-only does not.</summary>
		public string RelayPolicy { get; set; } = null!;

		/// <summary>The time at which this value expires.</summary>
		public string? ExpiresAt { get; set; }

		/// <summary>When to fetch new credentials: null with STUN only.</summary>
		public object? RefreshAfter { get; set; } = null!;

		/// <summary>Servers to pass to RTCPeerConnection.</summary>
		public P2pIceServersReplyV0IceServersItem[] IceServers { get; set; } = null!;
	}
}
