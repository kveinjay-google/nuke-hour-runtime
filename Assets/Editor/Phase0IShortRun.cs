#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
[InitializeOnLoad]
public static class Phase0IShortRun {
    static readonly string DirectoryPath=Environment.GetEnvironmentVariable("PHASE0I_RUN_DIR");
    static bool exiting;
    static Phase0IShortRun(){if(!String.IsNullOrEmpty(DirectoryPath))EditorApplication.update+=CheckExit;}
    public static void Open(){Phase0CEditor.OpenPatch();}
    static void CheckExit(){
        if(String.IsNullOrEmpty(DirectoryPath)||!File.Exists(Path.Combine(DirectoryPath,"stop-editor")))return;
        if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
        if(!exiting){exiting=true;EditorApplication.Exit(0);}
    }
}
#endif
