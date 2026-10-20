# Resource-free launcher development

Production code is in `Assets/PortPreparation/Launcher` and `Assets/Plugins/MeteorLauncherCore`; iOS folder access is in `Assets/Plugins/iOS/MeteorContentPicker.mm`. Exact Unity: 2020.1.0f1, revision 2ab9c4179772. No gameplay assets belong to the release project.

Run bounded managed and real TCP HTTP fixture tests:

```sh
python3 Tools/ResourceLauncher/run_tests.py
```

The test runner writes artifacts outside Git to a temporary directory unless `METEOR_TEST_EVIDENCE` specifies another output. HTTP fixtures are synthetic; they are not game assets or proof of gameplay.

Create a NEW isolated release input (never source-clean or delete game assets):

```sh
python3 Tools/ResourceLauncher/stage_release.py /absolute/experiment /absolute/new-release-stage
```

Use an external resource watchdog and the exact Editor to invoke `MeteorResourceBuild.BuildResourceFree` in this staging project with `-buildTarget iOS -job-worker-count 1 -disable-assembly-updater`. Set `METEOR_RELEASE_STAGING=YES`, `METEOR_RESOURCE_FREE_OUTPUT` to a new absolute output. The method creates a new empty launcher scene, enables the resource-free define, exports unsigned ARM64/IL2CPP/Metal, and rejects unexpected project assets in BuildReport. Build only within authorized host resource/time limits. Existing Xcode legacy Thumb and UnityFramework strict-aliasing compatibility fixes remain required.

## User import format

The first supported data profile is **METEOR_CONTENT_V1**, matched to Unity 2020.1.0f1 revision 2ab9c4179772 and iOS or macOS. It is not a general retail-PC-folder importer. A user who owns the source resources can invoke `MeteorResourceBuild.MakeOwnedContent` from the Unity menu in their local resource project. It exports only configured game scenes, all Resources assets and their dependencies, never executable code. Conversion output contains game data and must not be distributed with the program or committed.

Import these four files from the conversion output:

- `content.manifest`: header, exact runtime, platform, three SHA256 / length / filename rows.
- `resources.bundle`: Unity data-only Resource assets.
- `scenes.bundle`: external Patch, Startup and configured game scenes.
- `resources.index`: key-to-bundle-address index.

On the device, choose **从电脑网页导入**, connect the computer and device to the same trusted LAN, open the displayed URL, choose the folder or four files, and wait for transfer plus validation. The listener is foreground-only, size-bounded and non-encrypted HTTP. **从文件导入** selects a folder using the system picker. Data is validated on a private stage, parsed with Unity before activation, then atomically selected. Old valid installs and saves are preserved on failed replacement. iOS content is placed in Application Support and excluded from cloud backup.

No data means the launcher must remain locked; it must not start Main/LuaSvr. Missing resources never fall back to bundled commercial assets in a release. Game-derived previews are external data too.

## Controls

Native combat controls are remapped through normal input polling; no HP, AI or direct action/damage calls are used. Action binding, size, position preview dragging, opacity, visibility, separate tablet/phone profiles, keyboard and pad capture, conflict rejection and reset are supported. Defence holds until release. Long jump uses normal Jump input with a minimum hold across the original 0.12s full-press threshold; it is not the ninja-specific 0.35s super jump. Short taps survive one input poll, and overlapping sources release independently.

Editor screenshots, synthetic fixtures, data-bundle loading, player compilation and iPad gameplay are distinct evidence categories. A successful build does not prove that imported gameplay or physical buttons work on a device.
