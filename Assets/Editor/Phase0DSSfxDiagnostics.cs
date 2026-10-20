#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

// Read-only, opt-in Editor observer. Does not enter Play Mode or control gameplay.
[InitializeOnLoad]
public static class Phase0DSSfxDiagnostics
{
    const int MaxEvents = 256;
    const int MaxSeconds = 300;
    static readonly string Output = Environment.GetEnvironmentVariable("PHASE0DS_RUN_DIR");
    static readonly HashSet<string> Seen = new HashSet<string>();
    static double started, nextPoll;
    static int events, captures, trailEvents;
    static readonly HashSet<int> Trails = new HashSet<int>();
    static bool wasPlaying, failed;
    static readonly string[] MatPaths = {
        "Assets/Resource/Version/Resources/sn14_17.mat",
        "Assets/Resource/Version/Resources/sn14_20.mat",
        "Assets/Resource/Version/Resources/sn14_21.mat"
    };
    [Serializable] public class Field { public string name, value; }
    [Serializable] public class TextureRecord {
        public string name, path, guid, format, alphaSource, alphaSampling;
        public int width, height, alphaMin = -1, alphaMax = -1;
        public bool readable, alphaIsTransparency;
    }
    [Serializable] public class MaterialRecord {
        public int instance; public string name, path, guid, shader;
        public bool shaderSupported, dirty; public int queue;
        public Field[] states; public Field[] colors; public TextureRecord texture;
    }
    [Serializable] public class Record {
        public string utc, kind, scene, objectPath, effectName, effectTexture, rendererType, screenshot;
        public int frame, sfxInstance, ownerInstance, effectIndex, blendType;
        public bool rendererEnabled, active, visible;
        public MaterialRecord[] materials; public int meshVertices; public Vector3 boundsCenter,boundsSize;
    }
    [Serializable] public class HashRecord {
        public string utc, kind, path, sha256; public bool dirty;
    }
    static Phase0DSSfxDiagnostics() {
        if (String.IsNullOrEmpty(Output)) return;
        // Require an explicit absolute output directory; never fall back to an asset directory.
        if (!Path.IsPathRooted(Output)) return;
        EditorApplication.update += Tick;
        EditorApplication.quitting += OnQuit;
    }
    static void Append(object record) {
        Directory.CreateDirectory(Output);
        File.AppendAllText(Path.Combine(Output, "sfx-observations.jsonl"), JsonUtility.ToJson(record) + "\n");
    }
    static void Hashes(string stage) {
        foreach (string path in MatPaths) {
            if (!File.Exists(path)) continue;
            string hash;
            using (var sha = SHA256.Create()) using (var input = File.OpenRead(path))
                hash = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
            Append(new HashRecord { utc = DateTime.UtcNow.ToString("o"), kind = stage,
                path = path, sha256 = hash, dirty = EditorUtility.IsDirty(AssetDatabase.LoadAssetAtPath<Material>(path)) });
        }
    }
    static void OnQuit() { if (!failed) try { Hashes("editor-quitting"); } catch (Exception e) { Debug.LogWarning("Phase0DS: " + e.Message); } }
    static string ObjectPath(Transform t) {
        string result = t.name;
        for (int depth = 0; t.parent != null && depth < 32; depth++) { t = t.parent; result = t.name + "/" + result; }
        return result;
    }
    static Field State(Material m, string name) {
        return new Field { name = name, value = m.HasProperty(name) ? m.GetFloat(name).ToString(System.Globalization.CultureInfo.InvariantCulture) : "UNAVAILABLE: inspect shader fixed state" };
    }
    static TextureRecord TextureInfo(Texture t) {
        if (t == null) return null;
        string path = AssetDatabase.GetAssetPath(t);
        var r = new TextureRecord { name = t.name, path = path, guid = AssetDatabase.AssetPathToGUID(path),
            width = t.width, height = t.height, alphaSampling = "NOT_TESTED: not readable or exceeds 1M pixels" };
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null) { r.alphaSource = importer.alphaSource.ToString(); r.alphaIsTransparency = importer.alphaIsTransparency; }
        var texture = t as Texture2D;
        if (texture != null) {
            r.format = texture.format.ToString(); r.readable = texture.isReadable;
            if (texture.isReadable && (long)t.width * t.height <= 1048576) {
                Color32[] pixels = texture.GetPixels32();
                r.alphaMin = 255; r.alphaMax = 0;
                int step = Math.Max(1, pixels.Length / 16384);
                for (int i = 0; i < pixels.Length; i += step) { r.alphaMin = Math.Min(r.alphaMin, pixels[i].a); r.alphaMax = Math.Max(r.alphaMax, pixels[i].a); }
                r.alphaSampling = "CPU readable texture, sampled <=32768 pixels; not full GPU alpha proof";
            }
        }
        return r;
    }
    static MaterialRecord MaterialInfo(Material m) {
        if (m == null) return null;
        string path = AssetDatabase.GetAssetPath(m);
        return new MaterialRecord { instance = m.GetInstanceID(), name = m.name, path = path,
            guid = AssetDatabase.AssetPathToGUID(path), shader = m.shader == null ? "NULL" : m.shader.name,
            shaderSupported = m.shader != null && m.shader.isSupported, queue = m.renderQueue, dirty = EditorUtility.IsDirty(m),
            states = new[] { State(m,"_SrcBlend"), State(m,"_DstBlend"), State(m,"_ZWrite"), State(m,"_ZTest"), State(m,"_Cull") },
            colors = new[] { new Field { name="_Color",value=m.HasProperty("_Color")?m.GetColor("_Color").ToString():"UNAVAILABLE" }, new Field { name="_TintColor",value=m.HasProperty("_TintColor")?m.GetColor("_TintColor").ToString():"UNAVAILABLE" } },
            texture = TextureInfo(m.mainTexture) };
    }
    static void ObserveTrails() {
        if (trailEvents>=96) return;
        foreach (var renderer in Resources.FindObjectsOfTypeAll<MeshRenderer>()) {
            if (trailEvents>=96) break;
            if (!renderer.gameObject.scene.IsValid() || (!renderer.name.EndsWith("_Trail") && renderer.GetComponentInParent<MeteorUnit>()==null) || !renderer.enabled) continue;
            var filter=renderer.GetComponent<MeshFilter>();
            if(filter==null || filter.sharedMesh==null || filter.sharedMesh.vertexCount==0 || !Trails.Add(renderer.GetInstanceID())) continue;
            Material[] shared=renderer.sharedMaterials; var mats=new MaterialRecord[shared.Length];
            for(int i=0;i<shared.Length;i++)mats[i]=MaterialInfo(shared[i]);
            var record=new Record { utc=DateTime.UtcNow.ToString("o"),kind="UnitMeshRenderer",frame=Time.frameCount,
                scene=renderer.gameObject.scene.name,objectPath=ObjectPath(renderer.transform),sfxInstance=renderer.GetInstanceID(),
                blendType=-1,effectName="Unit or trail renderer (outside SFXUnit)",rendererType=renderer.GetType().Name,
                rendererEnabled=renderer.enabled,active=renderer.gameObject.activeInHierarchy,visible=renderer.isVisible,
                meshVertices=filter.sharedMesh.vertexCount,boundsCenter=renderer.bounds.center,boundsSize=renderer.bounds.size,materials=mats };
            Append(record);trailEvents++;
        }
    }
    static void Tick() {
        if (failed) return;
        try {
            bool playing = EditorApplication.isPlaying;
            if (playing != wasPlaying) {
                wasPlaying = playing;
                if (playing) { started = EditorApplication.timeSinceStartup; events = 0; captures = 0; trailEvents = 0; Trails.Clear(); Seen.Clear(); Hashes("play-begin"); }
                else Hashes("play-end");
            }
            if (!playing || EditorApplication.timeSinceStartup - started > MaxSeconds || EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 0.5;
            Hashes("poll");
            ObserveTrails();
            if (events >= MaxEvents) return;
            var units = Resources.FindObjectsOfTypeAll<SFXUnit>();
            int scanned = 0;
            foreach (var unit in units) {
                if (++scanned > 512 || events >= MaxEvents) break;
                if (unit == null || !unit.gameObject.scene.IsValid() || !unit.gameObject.activeInHierarchy || unit.source == null) continue;
                var renderers = unit.GetComponentsInChildren<Renderer>(false);
                foreach (var renderer in renderers) {
                    if (events >= MaxEvents) break;
                    if (!renderer.enabled) continue;
                    Material[] shared = renderer.sharedMaterials;
                    var materials = new MaterialRecord[shared.Length];
                    string signature = unit.GetInstanceID() + ":" + renderer.GetInstanceID();
                    for (int i = 0; i < shared.Length; i++) signature += ":" + (shared[i] == null ? 0 : shared[i].GetInstanceID()) + ":" + (shared[i] == null || shared[i].mainTexture == null ? 0 : shared[i].mainTexture.GetInstanceID());
                    if (!Seen.Add(signature)) continue;
                    for (int i = 0; i < shared.Length; i++) materials[i] = MaterialInfo(shared[i]);
                    var record = new Record { utc = DateTime.UtcNow.ToString("o"), kind = "SFX", frame = Time.frameCount,
                        scene = unit.gameObject.scene.name, objectPath = ObjectPath(renderer.transform), sfxInstance = unit.GetInstanceID(),
                        ownerInstance = unit.mOwner == null ? 0 : unit.mOwner.GetInstanceID(), effectIndex = unit.effectIndex,
                        effectName = unit.source.EffectName, effectTexture = unit.source.Texture, blendType = unit.source.BlendType,
                        rendererType = renderer.GetType().Name, rendererEnabled = renderer.enabled, active = renderer.gameObject.activeInHierarchy,
                        visible = renderer.isVisible, materials = materials };
                    if (captures < 8) {
                        record.screenshot = Path.Combine(Output, "sfx-request-frame-" + Time.frameCount + "-" + captures + ".png");
                        Directory.CreateDirectory(Output); ScreenCapture.CaptureScreenshot(record.screenshot); captures++;
                        // Capture is queued: request frame is not guaranteed to equal rendered capture frame.
                    }
                    Append(record); events++;
                }
            }
        } catch (Exception e) { failed = true; Debug.LogWarning("Phase0DS observer stopped: " + e); }
    }
}
#endif
