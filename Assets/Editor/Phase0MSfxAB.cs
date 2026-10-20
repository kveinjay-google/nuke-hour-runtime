#if UNITY_EDITOR
using System;using System.IO;using UnityEditor;using UnityEngine;
// Frozen scene, same renderer, same camera and properties; only candidate shader differs.
public static class Phase0MSfxAB {
 public static void Run(int id,string dir){
  var rend=EditorUtility.InstanceIDToObject(id) as Renderer;var cam=Main.Ins!=null?Main.Ins.MainCamera:null;
  var shader=Shader.Find("Hidden/Phase0M/MeteorBlend4Additive");if(rend==null||cam==null||shader==null||!shader.isSupported)throw new Exception("AB prerequisites missing");
  var materials=rend.sharedMaterials;var candidate=new Material[materials.Length];var rt=RenderTexture.GetTemporary(691,352,24,RenderTextureFormat.ARGB32);var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;
  Action<string> save=name=>{cam.Render();RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);try{t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();File.WriteAllBytes(Path.Combine(dir,name+".png"),t.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(t);}};
  try{cam.targetTexture=rt;save("sfx-A-original");for(int i=0;i<materials.Length;i++){candidate[i]=new Material(materials[i]);candidate[i].shader=shader;candidate[i].renderQueue=materials[i].renderQueue;}rend.sharedMaterials=candidate;save("sfx-B-additive");rend.sharedMaterials=materials;save("sfx-A-restored");File.WriteAllText(Path.Combine(dir,"sfx-ab.txt"),DateTime.UtcNow.ToString("o")+" same frozen renderer="+id+" object="+rend.name+" original="+materials[0].shader.name+" candidate="+shader.name+" texture="+AssetDatabase.GetAssetPath(materials[0].mainTexture)+" frame="+Time.frameCount+" camera="+cam.name+" no simulation step; Camera.Render captures exclude overlay UI; original restored\n");}
  finally{rend.sharedMaterials=materials;cam.targetTexture=oldTarget;RenderTexture.active=oldActive;foreach(var m in candidate)if(m!=null)UnityEngine.Object.DestroyImmediate(m);RenderTexture.ReleaseTemporary(rt);}
 }
}
#endif
