# Haiven versioning

This page describes how this demo, haiven-sdk-unity-bossroom, is released, and how its version relates to the Unity SDK, [haiven-sdk-unity](https://github.com/haiven-dev/haiven-sdk-unity).

## Where releases come from

Releases are published on this repository's [GitHub Releases page](https://github.com/haiven-dev/haiven-sdk-unity-bossroom/releases). The Haiven packages in this repository are published from Haiven's SDK source, and Haiven's release pipeline builds and publishes every release from that source after a maintainer review. Nothing in this repository cuts a release: there is no release workflow, release pull request, or tag to push here.

Each release carries:

- `com.haiven.demo.bossroom-<version>.tgz`: the Haiven Boss Room demo package (`Packages/com.haiven.demo.bossroom`) in `npm pack` layout. The rest of the project is Unity's Boss Room sample and its Git LFS assets, which releases do not attach; clone this repository for the whole game.
- `SHA256SUMS`: the SHA-256 digest of every asset, in `sha256sum` format. Check a download with `sha256sum -c SHA256SUMS`.

The release notes list each asset with its digest and name the source commit it was built from.

Earlier releases on that page, such as the `players-2026-11-10` desktop player builds, predate Haiven's release pipeline and carry other assets.

## How versions are numbered

A release is tagged `v<version>`. Haiven's SDK and demo repositories share one version line, which follows [Semantic Versioning](https://semver.org/): the repositories in one release carry the same version, so this demo's release has the same major.minor as the haiven-sdk-unity release it ships with. A release can cover only some of the repositories, so a fix or feature that only affects this demo can ship without a haiven-sdk-unity release, and this repository's version numbers can skip. A version with a pre-release suffix (`v1.2.3-rc.1`) is published as a GitHub pre-release and is never marked Latest.

The release tag and the asset name carry the version. The `version` fields in `Packages/com.haiven.demo.bossroom/package.json` and in the embedded copy of the Unity SDK at `Packages/com.haiven.sdk/package.json` come from Haiven's source and are not the demo's release number; read the release tag instead.

## Contributing changes

Source updates arrive as pull requests into `main`. Contributions are welcome as pull requests here too; see [CONTRIBUTING.md](../CONTRIBUTING.md). Haiven's maintainers carry each accepted change to the Haiven packages into Haiven's source, so later updates keep it.
