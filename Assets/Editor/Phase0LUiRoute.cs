#if UNITY_EDITOR
using System;using System.IO;using UnityEditor;using UnityEngine;
// Test input only: commands are consumed by the existing raycast/ExecuteEvents UI harness.
[InitializeOnLoad]
public static class Phase0LUiRoute {
 static readonly string Dir=Environment.GetEnvironmentVariable("PHASE0J_RUN_DIR");
 static double start,pauseAt;static int step;static bool ready,captured;static Vector2 w,d;
 [Serializable] class Command{public int seq;public string action,name;public float dx,dy;}
 static readonly float[] Times={.1f,.4f,4.7f,4.9f,5.6f,5.9f,6.08f,6.25f,6.43f,6.6f,6.78f,7.1f,7.28f,8.0f,8.18f,8.35f,8.53f,8.7f,8.88f,9.4f,9.58f,9.75f,9.93f,10.1f,10.28f};
 static Vector2 Point(Component c){var rect=c.GetComponent<RectTransform>();var canvas=c.GetComponentInParent<Canvas>();return RectTransformUtility.WorldToScreenPoint(canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null,rect.TransformPoint(rect.rect.center));}
 static Phase0LUiRoute(){if(!String.IsNullOrEmpty(Dir)&&Environment.GetEnvironmentVariable("PHASE0L_UI_ROUTE")=="1")EditorApplication.update+=Tick;}
 static void Tick(){
  if(!EditorApplication.isPlaying||Main.Ins==null||Main.Ins.LocalPlayer==null||File.Exists(Path.Combine(Dir,"stop-editor")))return;
  if(EditorApplication.isPaused){if(pauseAt==0)pauseAt=EditorApplication.timeSinceStartup;return;}
  if(pauseAt>0){start+=EditorApplication.timeSinceStartup-pauseAt;pauseAt=0;}
  if(!ready){var joy=UnityEngine.Object.FindObjectOfType<UGUIJoystick>();if(joy==null)return;var pad=new SerializedObject(joy).FindProperty("GamePad").objectReferenceValue as Component;if(pad==null)return;w=Point(joy.wKey)-Point(pad);d=Point(joy.dKey)-Point(pad);start=EditorApplication.timeSinceStartup;ready=true;File.AppendAllText(Path.Combine(Dir,"ui-route.txt"),DateTime.UtcNow.ToString("o")+" initialized W="+w+" D="+d+" normal UI input only\n");}
  double elapsed=EditorApplication.timeSinceStartup-start;
  if(step<Times.Length&&elapsed>=Times[step]){
   var path=Path.Combine(Dir,"command.json");int seq=File.Exists(path)?JsonUtility.FromJson<Command>(File.ReadAllText(path)).seq:0;
   var c=new Command{seq=seq+1,action="down",name="Attack"};
   if(step==0)c.action="benchmark";
   else if(step==1){c.name="GamePad";c.dx=w.x;c.dy=w.y;}
   else if(step==2||step==4)c.action="up";
   else if(step==3){c.name="GamePad";c.dx=d.x;c.dy=d.y;}
   else if(step==11){c.name="Jump";}
   else if(step==12||step%2==0)c.action="up";
   File.WriteAllText(path,JsonUtility.ToJson(c));File.AppendAllText(Path.Combine(Dir,"ui-route.txt"),DateTime.UtcNow.ToString("o")+" step="+step+" elapsed="+elapsed+" "+JsonUtility.ToJson(c)+"\n");step++;
  }
  if(!captured&&elapsed>5.9){
   foreach(var fx in UnityEngine.Object.FindObjectsOfType<SFXUnit>()){
    if(fx.source==null||fx.source.BlendType!=4)continue;string t=fx.source.Texture.ToUpperInvariant();if(!t.Contains("LIGHT")&&!t.Contains("HITP")&&!t.Contains("ZSS"))continue;
    var renderer=fx.GetComponent<Renderer>();if(renderer==null||!renderer.enabled||!renderer.isVisible)continue;
    ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"combat-before-debugger.png"));File.WriteAllText(Path.Combine(Dir,"frame-debugger-request"),"SFX-visible trigger; pixel attribution still required");captured=true;File.AppendAllText(Path.Combine(Dir,"ui-route.txt"),DateTime.UtcNow.ToString("o")+" capture trigger SFX="+fx.source.EffectName+" texture="+fx.source.Texture+" object="+fx.GetInstanceID()+"\n");break;
   }
  }
 }
}
#endif
