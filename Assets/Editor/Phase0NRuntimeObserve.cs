#if UNITY_EDITOR
using System;using System.IO;using System.Text;using UnityEditor;using UnityEngine;using UnityEngine.UI;using Meteor.PortPreparation;
[InitializeOnLoad] public static class Phase0NRuntimeObserve {
 static readonly string Dir=Environment.GetEnvironmentVariable("PHASE0I_RUN_DIR");static double next;
 [Serializable] class Targets {public string primary,secondary;}
 static Phase0NRuntimeObserve(){if(Environment.GetEnvironmentVariable("PHASE0N_OBSERVE")=="1")EditorApplication.update+=Tick;}
 static string PathOf(Transform t){string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;}
 static void Tick(){if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next||String.IsNullOrEmpty(Dir))return;next=EditorApplication.timeSinceStartup+.2;
  var targets=new Targets();foreach(var t in UnityEngine.Object.FindObjectsOfType<Toggle>())if(t.transform.parent!=null&&t.transform.parent.parent!=null){string group=t.transform.parent.parent.name;if(group=="FirstWeapon"&&t.name=="0")targets.primary=PathOf(t.transform);if(group=="SubWeapon"&&t.name=="1")targets.secondary=PathOf(t.transform);}if(targets.primary!=null&&targets.secondary!=null)File.WriteAllText(System.IO.Path.Combine(Dir,"menu-input-targets.json"),JsonUtility.ToJson(targets));
  if(Main.Ins==null||Main.Ins.LocalPlayer==null)return;var p=Main.Ins.LocalPlayer;var overlay=MeteorTouchOverlay.Instance;File.AppendAllText(System.IO.Path.Combine(Dir,"input-consumption.txt"),DateTime.UtcNow.ToString("o")+" frame="+Time.frameCount+" action="+(p.ActionMgr.mActiveAction!=null?p.ActionMgr.mActiveAction.Idx:-1)+" weaponType="+p.GetWeaponType()+" weapon="+p.Attr.Weapon+" secondary="+p.Attr.Weapon2+" hp="+p.Attr.hpCur+" pos="+p.transform.position+" pause="+CombatData.Ins.PauseAll+" pointers="+(overlay!=null?overlay.Router.ActivePointers:0)+"\n");
 }
}
#endif
