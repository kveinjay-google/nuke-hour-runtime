# External dependencies and exclusions

The complete experimental project used third-party code and libraries. This source-only publication deliberately excludes them pending per-component redistribution review. A root Apache license does not establish the license of vendored components.

Restore separately under the appropriate license: SLua managed runtime / generated wrappers and platform-native arm64 libslua.a; BytesHelper; protobuf-net; SharpZipLib / Ionic.Zip; DOTween; NGUI; RnM UI; Jitter; 7zip; Unity Standard Assets; I18N assemblies; ConvexDecomposition and other optional plugins. Dependency versions must match the original interfaces; no automatic Unity upgrade is prescribed. The excluded-files manifest describes omitted paths, not a download of those files.

Launcher fonts, icon and background are omitted too. Supply your own artwork/font files at the paths required by MeteorResourceBuild and preserve their licenses. This also avoids bundling any artwork whose provenance has not been independently reviewed for publication.

The source snapshot is intentionally not standalone-buildable until these dependencies are restored. Current importer format additionally requires privately converted Unity resources matching the exact runtime, platform and script contract. No resources or commercial download links are provided.

另外排除 Cartoon FX、LitJson 和 ImageEffects 编辑器源码；请自行恢复其合法授权版本。
