#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Excel2Json;

// Offline render of the shipped scene geometry. Never enters Play Mode or saves scenes/materials.
public static class Phase0NMapPreviewCapture {
 [Serializable] class Evidence {public int levelId,renderers;public string scene,path,kind="Editor Camera.Render / scene overview, not gameplay";public Vector3 center,size,camera;public double pixelVariance;}
 public static void Capture(){
  string output=Environment.GetEnvironmentVariable("METEOR_PREVIEW_OUTPUT");
  string evidence=Environment.GetEnvironmentVariable("PHASE0I_RUN_DIR");
  if(String.IsNullOrEmpty(output)||String.IsNullOrEmpty(evidence))throw new Exception("Explicit preview/evidence directories required");
  Directory.CreateDirectory(output);Directory.CreateDirectory(evidence);DataMgr.Ins.LoadAllData();
  foreach(string id in Environment.GetEnvironmentVariable("METEOR_PREVIEW_IDS").Split(',')){
   var level=DataMgr.Ins.GetLevelData(Int32.Parse(id));string file=Path.Combine(output,"map"+level.Id+".png");
   string scene=AssetDatabase.FindAssets(level.Scene+" t:Scene").Select(AssetDatabase.GUIDToAssetPath).First(p=>Path.GetFileNameWithoutExtension(p)==level.Scene);
   EditorSceneManager.OpenScene(scene,OpenSceneMode.Single);
   var renderers=UnityEngine.Object.FindObjectsOfType<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.name.ToLowerInvariant().Contains("sky")&&r.bounds.size.magnitude>0.1f&&r.bounds.size.magnitude<100000).ToArray();
   if(renderers.Length==0)throw new Exception("No scene mesh renderers: "+scene);
   Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
   float radius=Mathf.Max(10,bounds.extents.magnitude);var go=new GameObject("PreviewCaptureOnly");var cam=go.AddComponent<Camera>();
   cam.orthographic=true;cam.orthographicSize=radius*.92f;cam.aspect=16f/9;cam.nearClipPlane=.1f;cam.farClipPlane=radius*8;
   cam.transform.position=bounds.center+Quaternion.Euler(48,35,0)*Vector3.back*radius*2.5f;cam.transform.LookAt(bounds.center);
   cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.045f,.055f,.075f);cam.allowHDR=false;cam.allowMSAA=false;
   var lightGo=new GameObject("PreviewFillOnly");var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(50,-30,0);
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
   RenderSettings.ambientLight=new Color(.75f,.75f,.75f);
   var waypoints=WayLoader.ReLoad(level.sceneItems);
   if(waypoints.Count>1){
    Vector3 routeCenter=Vector3.zero;foreach(var point in waypoints)routeCenter+=point.pos;routeCenter/=waypoints.Count;
    var viewpoint=waypoints.OrderBy(p=>Mathf.Abs(Vector3.Distance(p.pos,routeCenter)-radius*.28f)).First();
    if(level.Id==14||level.Id==15||level.Id==16||level.Id==19)viewpoint=waypoints[0];
    cam.orthographic=false;cam.fieldOfView=72;cam.transform.position=viewpoint.pos+Vector3.up*75;
    Vector3 target=routeCenter+Vector3.up*65;if(Vector3.Distance(cam.transform.position,target)<60)target=waypoints.OrderByDescending(p=>Vector3.Distance(p.pos,viewpoint.pos)).First().pos+Vector3.up*65;
    if((level.Id==15||level.Id==16||level.Id==19)&&viewpoint.link!=null&&viewpoint.link.Count>0){
     int next=viewpoint.link.OrderByDescending(pair=>pair.Value.length).First().Key;
     if(next>=0&&next<waypoints.Count)target=waypoints[next].pos+Vector3.up*65;
    }
    if(level.Id==14)target=new Vector3(bounds.center.x,cam.transform.position.y-50,bounds.center.z);
    cam.transform.LookAt(target);
   }
   if(waypoints.Count<2){
    Vector3[] markers=new Vector3[0];
    if(!String.IsNullOrEmpty(level.sceneItems)){var des=new DesFile();des.Load(level.sceneItems+".des");markers=des.SceneItems.Where(item=>item.name.StartsWith("D_user")).Select(item=>item.pos).ToArray();}
    if(markers.Length<2)markers=UnityEngine.Object.FindObjectsOfType<Transform>().Where(t=>t.name.StartsWith("D_user")||t.name.StartsWith("D_team")).Select(t=>t.position).ToArray();
    if(markers.Length>=2){Vector3 center=Vector3.zero;foreach(var marker in markers)center+=marker;center/=markers.Length;cam.orthographic=false;cam.fieldOfView=72;cam.transform.position=markers[0]+Vector3.up*75;cam.transform.LookAt(center+Vector3.up*65);}
   }
   var rt=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
   var tex=new Texture2D(960,540,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();
   double sum=0,square=0;foreach(var c in tex.GetPixels32()){double v=(c.r+c.g+c.b)/3.0;sum+=v;square+=v*v;}double n=960*540;double variance=square/n-(sum/n)*(sum/n);
   if(variance<2)throw new Exception("Empty/flat preview for "+level.Name);
   File.WriteAllBytes(file,tex.EncodeToPNG());var row=new Evidence{levelId=level.Id,scene=scene,path=file,renderers=renderers.Length,center=bounds.center,size=bounds.size,camera=cam.transform.position,pixelVariance=variance};
   File.WriteAllText(Path.Combine(evidence,"map"+level.Id+".json"),JsonUtility.ToJson(row,true));Debug.Log("MAP_PREVIEW_CAPTURED "+level.Id+" "+level.Name+" meshes="+renderers.Length+" variance="+variance);
   cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(lightGo);
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorUtility.UnloadUnusedAssetsImmediate();
  }
  AssetDatabase.Refresh();
 }
}
#endif
