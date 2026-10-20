#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
// Read-only observation: return exactly the incoming paths, never request a save.
public class Phase0JAssetSaveTrace : UnityEditor.AssetModificationProcessor {
 static string[] OnWillSaveAssets(string[] paths){
  string dir=Environment.GetEnvironmentVariable("PHASE0J_RUN_DIR");
  if(String.IsNullOrEmpty(dir))return paths;
  try{
   foreach(var path in paths){
    if(!path.EndsWith("/sn14_17.mat")&&!path.EndsWith("/sn14_20.mat")&&!path.EndsWith("/sn14_21.mat"))continue;
    string hash="missing";if(File.Exists(path))using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");
    var material=AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    File.AppendAllText(Path.Combine(dir,"asset-save-trace.txt"),DateTime.UtcNow.ToString("o")+" path="+path+" dirty="+(material!=null&&EditorUtility.IsDirty(material))+" diskBeforeSave="+hash+" texture="+(material==null?"null":AssetDatabase.GetAssetPath(material.mainTexture))+"\n"+Environment.StackTrace+"\n");
   }
  }catch(Exception e){File.AppendAllText(Path.Combine(dir,"asset-save-trace-errors.txt"),e.ToString());}
  return paths;
 }
}
#endif
