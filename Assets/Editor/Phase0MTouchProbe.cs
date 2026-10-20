#if UNITY_EDITOR
using System;using System.IO;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.EventSystems;using Meteor.PortPreparation;
[InitializeOnLoad]
public static class Phase0MTouchProbe {
 static readonly string Dir=Environment.GetEnvironmentVariable("PHASE0I_RUN_DIR");static int stage,checks;static double next;static bool initialized;
 static Dictionary<int,PointerEventData> pointers=new Dictionary<int,PointerEventData>();static Dictionary<int,GameObject> targets=new Dictionary<int,GameObject>();
 static Phase0MTouchProbe(){if(Environment.GetEnvironmentVariable("PHASE0M_TOUCH_SMOKE")=="1")EditorApplication.update+=Tick;}
 public static void Run(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.ExecuteMenuItem("Window/General/Game");EditorApplication.isPlaying=true;}
 static void Log(string text){File.AppendAllText(Path.Combine(Dir,"touch-probe.txt"),DateTime.UtcNow.ToString("o")+" "+text+"\n");}
 static void Check(bool ok,string name){checks++;Log((ok?"PASS ":"FAIL ")+name);if(!ok)throw new Exception(name);}
 static void Down(string name,int id){var go=GameObject.Find("MeteorTouch_"+name);if(go==null)throw new Exception("widget missing "+name);var rect=go.GetComponent<RectTransform>();var pos=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));var data=new PointerEventData(EventSystem.current){pointerId=id,position=pos,pressPosition=pos,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Check(hits.Count>0&&(hits[0].gameObject==go||hits[0].gameObject.transform.IsChildOf(go.transform)),"raycast "+name+" id="+id);data.pointerCurrentRaycast=hits[0];data.pointerPressRaycast=hits[0];targets[id]=ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerDownHandler);pointers[id]=data;}
 static void Drag(int id,Vector2 delta){var data=pointers[id];data.delta=delta;data.position=data.pressPosition+delta;ExecuteEvents.Execute(targets[id],data,ExecuteEvents.beginDragHandler);ExecuteEvents.Execute(targets[id],data,ExecuteEvents.dragHandler);}
 static void Up(int id){ExecuteEvents.Execute(targets[id],pointers[id],ExecuteEvents.pointerUpHandler);targets.Remove(id);pointers.Remove(id);}
 static void Tick(){if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next||File.Exists(Path.Combine(Dir,"stop-editor")))return;next=EditorApplication.timeSinceStartup+.15;
  try{var overlay=MeteorTouchOverlay.Instance;if(overlay==null)return;overlay.TestMode=true;
   if(!initialized){if(EventSystem.current==null)new GameObject("TouchProbeEventSystem",typeof(EventSystem),typeof(StandaloneInputModule));Canvas.ForceUpdateCanvases();initialized=true;return;}
   switch(stage++){
    case 0:Check(PortInputState.Enabled,"source enabled");Down("Move",11);Drag(11,new Vector2(25,0));break;
    case 1:Check(PortInputState.MergeMove(0,0).X>0,"UI drag to input source");Down("Attack",12);Down("Attack",13);break;
    case 2:Check(PortInputState.Pressed("Attack",false),"attack source held");Check(overlay.Router.ActivePointers==3,"three independent pointers");Up(12);break;
    case 3:Check(PortInputState.Pressed("Attack",false),"one finger release retains second");Up(13);Up(11);Down("Look",14);Drag(14,new Vector2(15,10));break;
    case 4:Check(!PortInputState.Pressed("Attack",false),"last attack finger releases");Check(overlay.LookSamples>0,"look drag reaches sink");Up(14);Down("Jump",15);break;
    case 5:Check(PortInputState.Pressed("Jump",false),"jump source");Up(15);Down("Defence",16);break;
    case 6:Check(PortInputState.Pressed("Defence",false),"defence source");Up(16);Down("ChangeWeapon",17);break;
    case 7:Check(PortInputState.Pressed("ChangeWeapon",false),"weapon source");Up(17);Check(PortInputState.Pressed("Attack",true),"physical source preserved");ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"touch-editor-GameView.png"));Down("Pause",18);break;
    case 8:Check(overlay.Router.ActivePointers==0,"pause cancels captured pointers");Check(!PortInputState.Pressed("ChangeWeapon",false),"no stuck key");Log("EDITOR_UI_INPUT_PASS checks="+checks+" game and device NOT_TESTED");break;
    case 10:File.WriteAllText(Path.Combine(Dir,"stop-editor"),"touch UI smoke complete");break;
   }
  }catch(Exception e){Log("ERROR "+e);File.WriteAllText(Path.Combine(Dir,"stop-editor"),"touch UI smoke failed");}
 }
}
#endif
