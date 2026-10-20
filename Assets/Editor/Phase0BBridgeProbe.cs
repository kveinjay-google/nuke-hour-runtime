using System;
using System.IO;
using SLua;
using UnityEngine;
using UnityEditor;
public static class Phase0BBridgeProbe {
    [Serializable] public class Evidence { public string version; public bool luaSvrInitialized; public string luaSource; public double input; public double output; public double expected; public bool playing; public bool vector4Params; }
    public static void Inventory() {
        var lines = new System.Collections.Generic.List<string>();
        foreach (var a in UnityEditor.Compilation.CompilationPipeline.GetAssemblies()) {
            int count = 0; foreach (var f in a.sourceFiles) if (f.Contains("Slua/LuaObject")) count++;
            lines.Add(a.name + " sourceFiles=" + a.sourceFiles.Length + " generated=" + count);
        }
        lines.Add("assetType=" + AssetDatabase.GetMainAssetTypeAtPath("Assets/Slua/LuaObject/Unity/Lua_UnityEngine_Vector3.cs"));
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) {
            var t = a.GetType("SLua.BindUnity"); if (t != null) lines.Add("binder=" + t.Assembly.FullName);
        }
        File.WriteAllLines(Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Phase0B/evidence/binder-inventory.txt")), lines.ToArray());
        Debug.Log("PHASE0B_BINDER_INVENTORY " + string.Join(";", lines));
    }
    public static void Run() {
        var e = new Evidence { version = UnityEditorInternal.InternalEditorUtility.GetFullUnityVersion(), input = 7, expected = 8, playing = EditorApplication.isPlaying, luaSource = "function phase0b_roundtrip(x) return UnityEngine.Mathf.NextPowerOfTwo(x) end" };
        if (e.playing) throw new Exception("Probe must not enter Play mode");
        var svr = new LuaSvr();
        try {
            svr.init(null, () => { e.luaSvrInitialized = true; }, LuaSvrFlag.LSF_BASIC);
            if (!e.luaSvrInitialized) throw new Exception("LuaSvr initialization callback did not complete");
            LuaSvr.mainState.doString(e.luaSource);
            using (var fn = LuaSvr.mainState.getFunction("phase0b_roundtrip")) {
                if (fn == null) throw new Exception("Lua function missing");
                e.output = Convert.ToDouble(fn.call(e.input));
            }
            int top = LuaDLL.lua_gettop(LuaSvr.mainState.L);
            LuaObject.pushValue(LuaSvr.mainState.L, new Vector4(1, 2, 3, 4));
            Vector4[] vectors;
            LuaObject.checkParams(LuaSvr.mainState.L, top + 1, out vectors);
            e.vector4Params = vectors.Length == 1 && vectors[0] == new Vector4(1, 2, 3, 4);
            LuaDLL.lua_settop(LuaSvr.mainState.L, top);
            if (!e.vector4Params) throw new Exception("Vector4 params roundtrip mismatch");
            if (Math.Abs(e.output - e.expected) > 0.000001) throw new Exception("Binding roundtrip mismatch");
            var json = JsonUtility.ToJson(e, true);
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Phase0B/evidence/bridge-probe.json")), json);
            Debug.Log("PHASE0B_SLUA_BRIDGE_PASS " + json);
        } finally { LuaSvr.mainState.Dispose(); }
    }
}
