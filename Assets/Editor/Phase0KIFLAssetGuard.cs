#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
// Only the three confirmed animated source assets; no runtime material instantiation.
[InitializeOnLoad]
public static class Phase0KIFLAssetGuard {
 const string Key="Meteor.Phase0K.IFLSnapshot.v1";
 [Serializable] class Entry {public string path,texture;public int queue;public bool dirty;}
 [Serializable] class Snapshot {public Entry[] entries;}
 static readonly string[] Paths={"Assets/Resource/Version/Resources/sn14_17.mat","Assets/Resource/Version/Resources/sn14_20.mat","Assets/Resource/Version/Resources/sn14_21.mat"};
 static Phase0KIFLAssetGuard(){EditorApplication.playModeStateChanged+=Changed;}
 static void Note(string value){string dir=Environment.GetEnvironmentVariable("PHASE0J_RUN_DIR");if(!String.IsNullOrEmpty(dir))File.AppendAllText(Path.Combine(dir,"ifl-asset-guard.txt"),DateTime.UtcNow.ToString("o")+" "+value+"\n");}
 static void Changed(PlayModeStateChange state){
  if(state==PlayModeStateChange.ExitingEditMode){
   var snap=new Snapshot{entries=new Entry[Paths.Length]};
   for(int i=0;i<Paths.Length;i++){
    var m=AssetDatabase.LoadAssetAtPath<Material>(Paths[i]);if(m==null)continue;
    snap.entries[i]=new Entry{path=Paths[i],texture=AssetDatabase.GetAssetPath(m.mainTexture),queue=m.renderQueue,dirty=EditorUtility.IsDirty(m)};
   }
   SessionState.SetString(Key,JsonUtility.ToJson(snap));Note("snapshot "+JsonUtility.ToJson(snap));
  }else if(state==PlayModeStateChange.EnteredEditMode){
   string json=SessionState.GetString(Key,"");if(String.IsNullOrEmpty(json))return;
   var snap=JsonUtility.FromJson<Snapshot>(json);
   foreach(var entry in snap.entries){
    if(entry==null)continue;var m=AssetDatabase.LoadAssetAtPath<Material>(entry.path);if(m==null)continue;
    m.mainTexture=String.IsNullOrEmpty(entry.texture)?null:AssetDatabase.LoadAssetAtPath<Texture>(entry.texture);m.renderQueue=entry.queue;
    // Preserve a pre-existing user edit; never clear an asset that was already dirty.
    if(entry.dirty)EditorUtility.SetDirty(m);else EditorUtility.ClearDirty(m);
    Note("restored "+entry.path+" texture="+entry.texture+" queue="+entry.queue+" initialDirty="+entry.dirty+" finalDirty="+EditorUtility.IsDirty(m));
   }
   SessionState.EraseString(Key);
  }
 }
}
#endif
