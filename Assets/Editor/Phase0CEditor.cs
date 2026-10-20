using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class Phase0CEditor {
    public static void OpenPatch(){
        Debug.Log("PHASE0C_EDITOR_VERSION "+UnityEditorInternal.InternalEditorUtility.GetFullUnityVersion());
        var s=EditorSceneManager.OpenScene("Assets/Scene/Patch.unity",OpenSceneMode.Single);
        Phase0CTrace.Node("Editor.Patch.open",s.path+" loaded="+s.isLoaded);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.isPlaying=true;
    }
}
