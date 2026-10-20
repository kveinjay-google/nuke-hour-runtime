#if UNITY_EDITOR
using System;using System.IO;using System.Collections.Generic;using UnityEditor;using UnityEngine;
[InitializeOnLoad]
public static class Phase0LCombatObserve {
 static readonly string Dir=Environment.GetEnvironmentVariable("PHASE0J_RUN_DIR");static double next;static bool ui;static Dictionary<int,int> actions=new Dictionary<int,int>();
 static Phase0LCombatObserve(){if(!String.IsNullOrEmpty(Dir))EditorApplication.update+=Tick;}
 static void Note(string v){File.AppendAllText(Path.Combine(Dir,"combat-observe.txt"),DateTime.UtcNow.ToString("o")+" frame="+Time.frameCount+" "+v+"\n");}
 static Vector2 Screen(RectTransform t){var c=t.GetComponentInParent<Canvas>();return RectTransformUtility.WorldToScreenPoint(c!=null&&c.renderMode!=RenderMode.ScreenSpaceOverlay?c.worldCamera:null,t.TransformPoint(t.rect.center));}
 static void Tick(){if(!EditorApplication.isPlaying||Main.Ins==null||Main.Ins.LocalPlayer==null)return;
  var player=Main.Ins.LocalPlayer;
  foreach(var unit in UnityEngine.Object.FindObjectsOfType<MeteorUnit>()){
   int action=unit.ActionMgr!=null&&unit.ActionMgr.mActiveAction!=null?unit.ActionMgr.mActiveAction.Idx:-1;
   int prev;if(!actions.TryGetValue(unit.GetInstanceID(),out prev)||prev!=action){Note("action unit="+unit.InstanceId+" name="+unit.name+" action="+action+" hp="+unit.Attr.hpCur);actions[unit.GetInstanceID()]=action;}
  }
  if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.5;
  if(!ui){var joy=UnityEngine.Object.FindObjectOfType<UGUIJoystick>();if(joy!=null){
   var pad=new SerializedObject(joy).FindProperty("GamePad").objectReferenceValue as Component;
   if(pad!=null){Vector2 center=Screen(pad.GetComponent<RectTransform>());Note("UI pad="+pad.name+" center="+center);
    foreach(var button in new[]{joy.wKey,joy.sKey,joy.aKey,joy.dKey}){var offset=Screen(button.GetComponent<RectTransform>())-center;Note("UI arrow="+button.name+" offset="+offset);}
   }
   var graph=new SerializedObject(joy).FindProperty("GamePadGraph").objectReferenceValue as Component;if(graph!=null)Note("UI joystickGraph="+graph.name+" parent="+graph.transform.parent.name);ui=true;
  }}
  foreach(var enemy in UnityEngine.Object.FindObjectsOfType<MeteorUnit>())if(enemy!=player){var delta=enemy.transform.position-player.transform.position;Note("range player="+player.transform.position+" forward="+player.transform.forward+" enemy="+enemy.InstanceId+" pos="+enemy.transform.position+" distance="+delta.magnitude+" angle="+Vector3.Angle(-player.transform.forward,delta)+" hp="+enemy.Attr.hpCur);
   foreach(var c in enemy.GetComponentsInChildren<Collider>())if(c.enabled)Note("enemyCollider id="+c.GetInstanceID()+" name="+c.name+" type="+c.GetType().Name+" trigger="+c.isTrigger+" center="+c.bounds.center+" size="+c.bounds.size);
  }
 }
}
#endif
