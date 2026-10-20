#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class Phase0KIFLTrace {
 static readonly string Dir=Environment.GetEnvironmentVariable("PHASE0J_RUN_DIR");
 static int count;
 static Phase0KIFLTrace(){if(!String.IsNullOrEmpty(Dir))IFLLoader.DiagnosticTextureWrite+=Observe;}
 static void Observe(IFLLoader loader,Renderer renderer,int frame,string phase){
  if(count++>=1024||renderer==null)return;
  var materials=renderer.sharedMaterials;if(loader.matIndex<0||loader.matIndex>=materials.Length)return;
  var m=materials[loader.matIndex];if(m==null)return;
  string mat=AssetDatabase.GetAssetPath(m);if(!mat.EndsWith("/sn14_17.mat")&&!mat.EndsWith("/sn14_20.mat")&&!mat.EndsWith("/sn14_21.mat"))return;
  string tex=AssetDatabase.GetAssetPath(m.mainTexture);
  File.AppendAllText(Path.Combine(Dir,"ifl-texture-writes.txt"),DateTime.UtcNow.ToString("o")+" phase="+phase+" engineFrame="+Time.frameCount+" iflInstance="+loader.GetInstanceID()+" object="+loader.name+" IFL="+AssetDatabase.GetAssetPath(loader.IFLFile)+" animationFrame="+frame+" renderer="+renderer.GetInstanceID()+" material="+m.GetInstanceID()+" materialPath="+mat+" texture="+tex+" GUID="+AssetDatabase.AssetPathToGUID(tex)+" dirty="+EditorUtility.IsDirty(m)+"\n");
 }
}
#endif
