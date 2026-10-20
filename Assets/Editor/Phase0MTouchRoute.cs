#if UNITY_EDITOR
using System;using System.IO;using UnityEditor;using UnityEngine;
// Closed-loop normal UI gestures. Never writes actor transforms/HP or calls damage APIs.
[InitializeOnLoad]
public static class Phase0MTouchRoute {
 static readonly string Dir=Environment.GetEnvironmentVariable("PHASE0I_RUN_DIR");
 static int stage,pulses,pending;static double start,next,pauseAt;static bool captured,benchmark;static float lookX;static bool pressed;
 [Serializable] class Command{public int seq;public string action,name;public float dx,dy;}
 static Phase0MTouchRoute(){if(Environment.GetEnvironmentVariable("PHASE0M_TOUCH_ROUTE")=="1")EditorApplication.update+=Tick;}
 static void Log(string value){File.AppendAllText(Path.Combine(Dir,"touch-route.txt"),DateTime.UtcNow.ToString("o")+" "+value+"\n");}
 static void Send(string action,string name="",float dx=0,float dy=0){var path=Path.Combine(Dir,"command.json");int seq=File.Exists(path)?JsonUtility.FromJson<Command>(File.ReadAllText(path)).seq:0;var cmd=new Command{seq=seq+1,action=action,name="MeteorTouch_"+name,dx=dx,dy=dy};pending=cmd.seq;File.WriteAllText(path,JsonUtility.ToJson(cmd));Log(JsonUtility.ToJson(cmd));}
 static void Tick(){
  if(!EditorApplication.isPlaying||Main.Ins==null||Main.Ins.LocalPlayer==null||File.Exists(Path.Combine(Dir,"stop-editor")))return;
  if(EditorApplication.isPaused){if(pauseAt==0)pauseAt=EditorApplication.timeSinceStartup;return;}
  if(pauseAt>0){double delta=EditorApplication.timeSinceStartup-pauseAt;start+=delta;next+=delta;pauseAt=0;}
  double now=EditorApplication.timeSinceStartup;if(now<next)return;next=now+.18;
  if(pending>0){int ack;string ackPath=Path.Combine(Dir,"command-ack.txt");if(!File.Exists(ackPath)||!Int32.TryParse(File.ReadAllText(ackPath),out ack)||ack<pending)return;pending=0;}
  var player=Main.Ins.LocalPlayer;if(player.Dead)return;
  if(start==0)start=now;
  if(now-start>19){if(pressed){Send("up");pressed=false;}return;}
  MeteorUnit target=null;float best=Single.MaxValue;
  foreach(var unit in UnityEngine.Object.FindObjectsOfType<MeteorUnit>()){if(unit==player||unit.Dead||unit.Camp==player.Camp)continue;var v=unit.transform.position-player.transform.position;v.y=0;if(v.sqrMagnitude<best){best=v.sqrMagnitude;target=unit;}}
  if(target==null)return;var deltaTarget=target.transform.position-player.transform.position;deltaTarget.y=0;float distance=deltaTarget.magnitude;
  // Source SetVelocity/FaceToTarget establish that model forward is opposite facing.
  float angle=Vector3.SignedAngle(-player.transform.forward,deltaTarget,Vector3.up);
  Log("stage="+stage+" distance="+distance+" facingAngle="+angle+" player="+player.transform.position+" target="+target.InstanceId+" hp="+target.Attr.hpCur);
  if(!benchmark){Send("benchmark");benchmark=true;return;}
  switch(stage){
   case 0:Send("down","Look");pressed=true;lookX=0;stage=1;break;
   case 1:
    if(Mathf.Abs(angle)<7){Send("up");pressed=false;stage=2;}
    else{lookX+=Mathf.Clamp(angle/12*100,-100,100);Send("drag","Look",lookX,0);}break;
   case 2:
    if(distance<85){stage=4;break;}Send("down","Move");pressed=true;stage=3;break;
   case 3:
    if(distance<85||Mathf.Abs(angle)>28){Send("up");pressed=false;stage=distance<85?4:0;}
    else Send("drag","Move",0,100);break;
   case 4:
    if(distance>110||Mathf.Abs(angle)>40){stage=0;break;}Send("down","Attack");pressed=true;stage=5;break;
   case 5:Send("up");pressed=false;pulses++;stage=pulses<(Environment.GetEnvironmentVariable("PHASE0M_EXTRA_INPUT")=="1"?4:14)?4:6;break;
   case 6:Send("capture","after-input");stage=7;break;
   case 7:if(Environment.GetEnvironmentVariable("PHASE0M_EXTRA_INPUT")!="1")return;Send("down","Jump");pressed=true;stage=8;break;
   case 8:Send("up");pressed=false;stage=9;break;
   case 9:Send("down","Defence");pressed=true;stage=10;break;
   case 10:Send("up");pressed=false;stage=11;break;
   case 11:Send("down","ChangeWeapon");pressed=true;stage=12;break;
   case 12:Send("up");pressed=false;stage=13;break;
   case 13:Send("down","Attack");pressed=true;stage=14;break;
   case 14:Send("up");pressed=false;stage=15;break;
   case 15:Send("click","Pause");stage=16;break;
   case 16:Send("capture","touch-pause");stage=17;break;
  }
  if(!captured&&pulses>0){foreach(var fx in UnityEngine.Object.FindObjectsOfType<SFXUnit>()){
   if(fx.source==null||fx.source.BlendType!=4)continue;string texture=fx.source.Texture.ToUpperInvariant();if(Environment.GetEnvironmentVariable("PHASE0N_CAPTURE_PLAYER_SFX")=="1"&&!texture.Contains("LIGHTD"))continue;if(!texture.Contains("LIGHT")&&!texture.Contains("HITP")&&!texture.Contains("ZSS"))continue;
   var renderer=fx.GetComponent<Renderer>();if(renderer==null||!renderer.enabled||!renderer.isVisible)continue;
   File.WriteAllText(Path.Combine(Dir,"frame-debugger-request"),"SFX after normal UI attack; black pixel attribution still required");captured=true;Log("capture source="+fx.source.EffectName+" texture="+fx.source.Texture+" renderer="+renderer.GetInstanceID());break;
  }}
 }
}
#endif
