#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Excel2Json;

// Tests the actual menu prefab without touching saved progression or loading a level.
public static class Phase0NMapCatalogProbe {
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool value,string message){if(!value)throw new Exception("MAP_CATALOG_ASSERT: "+message);Debug.Log("MAP_CATALOG_PASS "+message);}
 public static void PreparePreviewAssets(){
  foreach(string file in Directory.GetFiles("Assets/PortPreparation/Resources/MapPreviews","*.png")){
   var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(file);
   importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=1024;
   importer.SetPlatformTextureSettings(new UnityEditor.TextureImporterPlatformSettings{name="iPhone",overridden=true,maxTextureSize=1024,format=UnityEditor.TextureImporterFormat.ASTC_6x6});
   importer.SaveAndReimport();
  }
 }
 public static void RunAndExport(){PreparePreviewAssets();Run();Phase0EHUnsignedIosExport.Export();}
 static void CaptureMenu(GameObject go,Canvas canvas,Camera camera,string file){
  Canvas.ForceUpdateCanvases();int width=Mathf.RoundToInt(canvas.GetComponent<RectTransform>().rect.width);var rt=new RenderTexture(width,1080,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
  var image=new Texture2D(width,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,1080),0,0);image.Apply();File.WriteAllBytes(file,image.EncodeToPNG());
  camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
 }
 public static void Run(){
  var prior=GameStateMgr.Ins.gameStatus;GameObject go=null,canvasRoot=null,cameraRoot=null;
  Vector2 resolution=UIHelper.resolution;float cw=UIHelper.CanvasWidth,ch=UIHelper.CanvasHeight;
  try {
   DataMgr.Ins.LoadAllData();// Test-only state: its production constructor requires a running Main.
   GameStateMgr.Ins.gameStatus=(GameState)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(GameState));GameStateMgr.Ins.gameStatus.Level=1;
   canvasRoot=new GameObject("MenuPreviewCanvas",typeof(RectTransform),typeof(Canvas));canvasRoot.layer=5;
   var canvas=canvasRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvasRoot.GetComponent<RectTransform>().sizeDelta=new Vector2(1920,1080);
   cameraRoot=new GameObject("MenuPreviewCamera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=540;camera.aspect=16f/9;camera.transform.position=new Vector3(0,0,-2000);camera.farClipPlane=4000;camera.cullingMask=1<<5;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;canvas.worldCamera=camera;
   UIHelper.resolution=new Vector2(1920,1080);UIHelper.CanvasWidth=1920;UIHelper.CanvasHeight=1080;
   go=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("UI/Dialogs/LevelDialog"));go.transform.SetParent(canvasRoot.transform,false);
   var rootRect=go.GetComponent<RectTransform>();rootRect.anchorMin=Vector2.zero;rootRect.anchorMax=Vector2.one;rootRect.offsetMin=rootRect.offsetMax=Vector2.zero;
   var dialog=go.GetComponent<LevelDialog>();var type=typeof(LevelDialog);
   type.GetField("normalLevel",Flags).SetValue(dialog,true);type.GetMethod("Init",Flags).Invoke(dialog,null);
   var root=(GameObject)type.GetField("rootMenu",Flags).GetValue(dialog);
   var yes=(Button)type.GetField("Yes",Flags).GetValue(dialog);
   Check(root.transform.childCount==DataMgr.Ins.GetLevelDatas().Count,"all configured maps visible");
   int locked=0;foreach(Transform row in root.transform)if(row.GetComponentInChildren<Text>().text.Contains("未解锁"))locked++;
   Check(locked==root.transform.childCount-1,"fresh progress locks every map except first");Check(yes.interactable,"first map entry enabled");
   type.GetMethod("OnSelectLevel",Flags).Invoke(dialog,new object[]{DataMgr.Ins.GetLevelData(2)});
   Check(!yes.interactable,"second map preview disables entry");
   type.GetMethod("OnEnterLevel",Flags).Invoke(dialog,null); // Must return before U3D.LoadLevel / Main access.
   Check(true,"locked entry handler returns without loading scene");
   GameStateMgr.Ins.gameStatus.Level=2;
   type.GetMethod("OnSelectLevel",Flags).Invoke(dialog,new object[]{DataMgr.Ins.GetLevelData(2)});
   Check(yes.interactable,"normal progression enables second map");
   string dir=Environment.GetEnvironmentVariable("PHASE0I_RUN_DIR");Directory.CreateDirectory(dir);
   var description=(Text)type.GetField("Description",Flags).GetValue(dialog);var background=(Image)type.GetField("background",Flags).GetValue(dialog);
   int previews=0;foreach(var level in DataMgr.Ins.GetLevelDatas()){
    type.GetMethod("OnSelectLevel",Flags).Invoke(dialog,new object[]{level});Canvas.ForceUpdateCanvases();
    Check(background.sprite!=null,"preview loads for "+level.Id);Check(!String.IsNullOrEmpty(level.Desc)&&description.text.Contains(level.Desc),"description displayed for "+level.Id);
    Check(description.preferredHeight<=description.rectTransform.rect.height+1,"description fits for "+level.Id);
    previews++;if(level.Id==10||level.Id==34)CaptureMenu(go,canvas,camera,Path.Combine(dir,"menu-map"+level.Id+".png"));
   }
   canvasRoot.GetComponent<RectTransform>().sizeDelta=new Vector2(1552,1080);UIHelper.CanvasWidth=1552;camera.aspect=1552f/1080;
   foreach(var level in DataMgr.Ins.GetLevelDatas()){
    type.GetMethod("OnSelectLevel",Flags).Invoke(dialog,new object[]{level});Canvas.ForceUpdateCanvases();
    Check(description.preferredHeight<=description.rectTransform.rect.height+1,"iPad-aspect description fits for "+level.Id);
   }
   CaptureMenu(go,canvas,camera,Path.Combine(dir,"menu-ipad-aspect-map34.png"));
   File.WriteAllText(Path.Combine(dir,"map-previews-probe.txt"),"PASS "+previews+" textures + introductions + text heights; locked-entry gate preserved; screenshots map10/map34; Editor prefab rendering, not device UI");
   File.WriteAllText(Path.Combine(dir,"map-catalog-probe.txt"),"PASS actual LevelDialog prefab; maps="+root.transform.childCount+"; locked="+locked+"; selection/entry guard/progression checks passed; no level loaded, no saved progression changed");
  } finally {GameStateMgr.Ins.gameStatus=prior;if(go!=null)UnityEngine.Object.DestroyImmediate(go);if(canvasRoot!=null)UnityEngine.Object.DestroyImmediate(canvasRoot);if(cameraRoot!=null)UnityEngine.Object.DestroyImmediate(cameraRoot);UIHelper.resolution=resolution;UIHelper.CanvasWidth=cw;UIHelper.CanvasHeight=ch;}
 }
}
#endif
