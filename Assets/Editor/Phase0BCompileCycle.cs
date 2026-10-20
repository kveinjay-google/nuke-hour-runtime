using System;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
[InitializeOnLoad]
public static class Phase0BCompileCycle {
    const string Key = "Phase0B.CompileCycle";
    static double start;
    static Phase0BCompileCycle() { if (SessionState.GetBool(Key, false)) Watch(); }
    static void Watch() { start = EditorApplication.timeSinceStartup; EditorApplication.update -= Poll; EditorApplication.update += Poll; }
    public static void Run() {
        SessionState.SetBool(Key, true);
        Watch();
        CompilationPipeline.RequestScriptCompilation();
        Debug.Log("PHASE0B_REQUEST_SCRIPT_COMPILATION");
    }
    static void Poll() {
        if (EditorApplication.isCompiling || EditorApplication.timeSinceStartup - start < 5) return;
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) {
            if (a.GetType("SLua.BindUnity") == null) continue;
            Debug.Log("PHASE0B_GENERATED_BINDER_COMPILED " + a.FullName);
            SessionState.SetBool(Key, false); EditorApplication.Exit(0); return;
        }
        if (EditorApplication.timeSinceStartup - start > 45) {
            Debug.LogError("PHASE0B_GENERATED_BINDER_MISSING_AFTER_RECOMPILE");
            SessionState.SetBool(Key, false); EditorApplication.Exit(1);
        }
    }
}
