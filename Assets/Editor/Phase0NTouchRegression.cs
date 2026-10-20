#if UNITY_EDITOR
using System;using System.IO;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.EventSystems;using Meteor.PortPreparation;
[InitializeOnLoad]
public static class Phase0NTouchRegression {
 static readonly string Dir=Environment.GetEnvironmentVariable("PHASE0I_RUN_DIR");static int stage,checks;static double next;static bool initialized;
 static Dictionary<int,PointerEventData> pointers=new Dictionary<int,PointerEventData>();static Dictionary<int,GameObject> targets=new Dictionary<int,GameObject>();
 static Phase0NTouchRegression(){if(Environment.GetEnvironmentVariable("PHASE0N_REAL_TOUCH")=="1")EditorApplication.update+=Tick;}
 public static void Run(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.ExecuteMenuItem("Window/General/Game");EditorApplication.isPlaying=true;}
 static void Log(string text){File.AppendAllText(Path.Combine(Dir,"touch-game-regression.txt"),DateTime.UtcNow.ToString("o")+" "+text+"\n");}
 static void Check(bool ok,string name){checks++;Log((ok?"PASS ":"FAIL ")+name);if(!ok)throw new Exception(name);}
 static void Down(string name,int id){var go=GameObject.Find("MeteorTouch_"+name);if(go==null)throw new Exception("widget missing "+name);var rect=go.GetComponent<RectTransform>();var pos=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));var data=new PointerEventData(EventSystem.current){pointerId=id,position=pos,pressPosition=pos,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Check(hits.Count>0&&(hits[0].gameObject==go||hits[0].gameObject.transform.IsChildOf(go.transform)),"raycast "+name+" id="+id);data.pointerCurrentRaycast=hits[0];data.pointerPressRaycast=hits[0];targets[id]=ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerDownHandler);pointers[id]=data;}
 static void Drag(int id,Vector2 delta){var data=pointers[id];data.delta=delta;data.position=data.pressPosition+delta;ExecuteEvents.Execute(targets[id],data,ExecuteEvents.beginDragHandler);ExecuteEvents.Execute(targets[id],data,ExecuteEvents.dragHandler);}
 static void Up(int id){ExecuteEvents.Execute(targets[id],pointers[id],ExecuteEvents.pointerUpHandler);targets.Remove(id);pointers.Remove(id);}
 static void Tick(){if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next||File.Exists(Path.Combine(Dir,"stop-editor")))return;next=EditorApplication.timeSinceStartup+.4;
  try{var overlay=MeteorTouchOverlay.Instance;if(overlay==null)return;if(Main.Ins==null||Main.Ins.LocalPlayer==null)return;overlay.TestMode=false;
   var player=Main.Ins.LocalPlayer;Log("STATE stage="+stage+" pos="+player.transform.position+" action="+(player.ActionMgr.mActiveAction!=null?player.ActionMgr.mActiveAction.Idx:-1)+" weapon="+player.GetWeaponType()+" hp="+player.Attr.hpCur+" pause="+CombatData.Ins.PauseAll+" pointers="+overlay.Router.ActivePointers+" move="+PortInputState.MergeMove(0,0).X+","+PortInputState.MergeMove(0,0).Y+" lookSamples="+overlay.LookSamples);
   if(!initialized){if(EventSystem.current==null)new GameObject("TouchProbeEventSystem",typeof(EventSystem),typeof(StandaloneInputModule));Canvas.ForceUpdateCanvases();initialized=true;return;}
   if(Environment.GetEnvironmentVariable("PHASE0N_ISOLATED_TOUCH")=="1"){
    switch(stage){
     case 0:Down("Jump",15);stage=1;break;
     case 1:Check(PortInputState.Pressed("Jump",false),"idle jump source");Up(15);stage=2;break;
     case 2:if(!player.IsOnGround())return;Down("Defence",16);stage=3;break;
     case 3:Check(PortInputState.Pressed("Defence",false),"idle defence source");Up(16);stage=4;next=EditorApplication.timeSinceStartup+.6;break;
     case 4:Down("ChangeWeapon",17);stage=5;break;
     case 5:Up(17);stage=6;next=EditorApplication.timeSinceStartup+1;break;
     case 6:Log("WEAPON_AFTER_IDLE_SWITCH primary="+player.Attr.Weapon+" secondary="+player.Attr.Weapon2+" type="+player.GetWeaponType());Down("Attack",12);stage=7;break;
     case 7:Up(12);stage=8;break;
     case 8:Down("Attack",12);stage=9;break;
     case 9:Up(12);stage=10;break;
     case 10:Down("Attack",12);stage=11;break;
     case 11:Up(12);stage=12;break;
     case 12:ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"idle-input-game.png"));File.WriteAllText(Path.Combine(Dir,"stop-editor"),"bounded idle-input sequence complete; evaluate actual action log");stage=13;break;
    }return;
   }
   switch(stage++){
    case 0:Check(PortInputState.Enabled,"source enabled");Down("Move",11);Drag(11,new Vector2(25,0));Down("Look",14);Drag(14,new Vector2(15,0));break;
    case 1:Check(PortInputState.MergeMove(0,0).X>0,"UI drag to input source");Down("Attack",12);Down("Attack",13);break;
    case 2:Check(PortInputState.Pressed("Attack",false),"attack source held");Check(overlay.Router.ActivePointers==4,"four independent pointers: move/look/two attacks");Up(12);break;
    case 3:Check(PortInputState.Pressed("Attack",false),"one finger release retains second");Up(13);Up(11);Up(14);Down("Look",14);Drag(14,new Vector2(15,10));break;
    case 4:Check(!PortInputState.Pressed("Attack",false),"last attack finger releases");Check(overlay.LookSamples>0,"look drag reaches sink");Up(14);Down("Jump",15);break;
    case 5:Check(PortInputState.Pressed("Jump",false),"jump source");Up(15);Down("Defence",16);break;
    case 6:Check(PortInputState.Pressed("Defence",false),"defence source");Up(16);Down("ChangeWeapon",17);break;
    case 7:Check(PortInputState.Pressed("ChangeWeapon",false),"weapon source");Up(17);Check(PortInputState.Pressed("Attack",true),"physical source preserved");ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"touch-game-GameView.png"));Down("Pause",18);break;
    case 8:Check(overlay.Router.ActivePointers==0,"pause cancels captured pointers");Check(!PortInputState.Pressed("ChangeWeapon",false),"no stuck key");Log("EDITOR_GAME_UI_CHECKS_PASS checks="+checks+" device NOT_TESTED");var cont=GameObject.Find("Continue");if(cont==null)throw new Exception("Continue not visible");var ev=new PointerEventData(EventSystem.current);ExecuteEvents.Execute(cont,ev,ExecuteEvents.pointerClickHandler);Log("normal Continue UI click");break;
    case 9:Check(!CombatData.Ins.PauseAll,"normal UI resume");Check(overlay.Router.ActivePointers==0,"no pointers after pause/resume");break;
    case 10:File.WriteAllText(Path.Combine(Dir,"stop-editor"),"touch UI smoke complete");break;
   }
  }catch(Exception e){Log("ERROR "+e);File.WriteAllText(Path.Combine(Dir,"stop-editor"),"touch UI smoke failed");}
 }
}
#endif
