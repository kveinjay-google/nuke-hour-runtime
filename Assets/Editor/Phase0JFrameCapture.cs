#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class Phase0JFrameCapture {
 static readonly string Dir=Environment.GetEnvironmentVariable("PHASE0J_RUN_DIR");
 static readonly Type Utility=typeof(Editor).Assembly.GetType("UnityEditorInternal.FrameDebuggerUtility");
 static readonly BindingFlags Flags=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static EditorWindow window; static double deadline,next; static bool active,pausedBefore; static Array events;
 static int actualRenderer; static List<int> selected; static int cursor,phase; static string before,after;
 static Phase0JFrameCapture(){if(!String.IsNullOrEmpty(Dir)){EditorApplication.update+=Tick;EditorApplication.quitting+=Restore;}}
 static object Call(string n,params object[] args){return Utility.GetMethod(n,Flags).Invoke(null,args);}
 static void Log(string value){File.AppendAllText(Path.Combine(Dir,"frame-debugger.txt"),DateTime.UtcNow.ToString("o")+" "+value+"\n");}
 static string Describe(object value,int depth){
  if(value==null)return "null";var t=value.GetType();var obj=value as UnityEngine.Object;
  if(obj!=null)return t.Name+" id="+obj.GetInstanceID()+" name="+obj.name+" path="+AssetDatabase.GetAssetPath(obj);
  if(t.IsPrimitive||t.IsEnum||value is string||depth<=0)return value.ToString();
  var array=value as Array;if(array!=null){var a=new StringBuilder("[");for(int i=0;i<Math.Min(array.Length,32);i++)a.Append(Describe(array.GetValue(i),depth-1)+";");return a.Append("] length="+array.Length).ToString();}
  var b=new StringBuilder(t.Name+"{");foreach(var f in t.GetFields(BindingFlags.Public|BindingFlags.Instance))b.Append(f.Name+"="+Describe(f.GetValue(value),depth-1)+";");return b.Append("}").ToString();
 }
 static void SaveGameView(string path){
  var type=typeof(Editor).Assembly.GetType("UnityEditor.PlayModeView");
  var view=type.GetMethod("GetMainPlayModeView",Flags).Invoke(null,null);
  var rt=type.GetField("m_TargetTexture",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view) as RenderTexture;
  if(rt==null||rt.width<=0||rt.height<=0||(long)rt.width*rt.height>4194304)throw new InvalidOperationException("Game View RT unavailable or exceeds readback bound");
  var previous=RenderTexture.active;Texture2D pixels=null;
  try{RenderTexture.active=rt;pixels=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);pixels.Apply(false,false);File.WriteAllBytes(path,pixels.EncodeToPNG());Log("GameView RT readback "+rt.width+"x"+rt.height+" "+path);}
  finally{RenderTexture.active=previous;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);}
 }
 static void Restore(){if(!active)return;try{Call("SetEnabled",false,(int)Call("GetRemotePlayerGUID"));Log("disabled local="+Call("IsLocalEnabled")+" remote="+Call("IsRemoteEnabled"));}catch(Exception e){Log("disable failed "+e);}if(Environment.GetEnvironmentVariable("PHASE0M_SFX_AB")=="1"&&actualRenderer!=0){try{Phase0MSfxAB.Run(actualRenderer,Dir);}catch(Exception e){Log("AB_ERROR "+e);}actualRenderer=0;}EditorApplication.isPaused=pausedBefore;active=false;events=null;if(window!=null){window.Close();window=null;}Log("capture stopped; previous pause="+pausedBefore+" restored");}
 static void Limit(int limit){Utility.GetProperty("limit",Flags).SetValue(null,Math.Max(1,limit),null);UnityEditorInternal.InternalEditorUtility.RepaintAllViews();if(window!=null)window.Repaint();EditorApplication.QueuePlayerLoopUpdate();next=EditorApplication.timeSinceStartup+.15;}
 static void Tick(){try{
  if(active&&(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup>=deadline||File.Exists(Path.Combine(Dir,"stop-editor")))){Log("deadline/stop reached phase="+phase);Restore();return;}
  if(!active){
   string request=Path.Combine(Dir,"frame-debugger-request");if(!EditorApplication.isPlaying||!File.Exists(request))return;
   File.Move(request,Path.Combine(Dir,"frame-debugger-request-consumed-"+DateTime.UtcNow.Ticks));
   bool supported=(bool)Utility.GetProperty("locallySupported",Flags).GetValue(null,null);Log("locallySupported="+supported);if(!supported)return;
   if((bool)Call("IsLocalEnabled")||(bool)Call("IsRemoteEnabled")){Log("refused existing session");return;}
   pausedBefore=EditorApplication.isPaused;active=true;deadline=EditorApplication.timeSinceStartup+8;cursor=0;phase=0;events=null;
   EditorApplication.isPaused=true;var type=typeof(Editor).Assembly.GetType("UnityEditor.FrameDebuggerWindow");window=EditorWindow.GetWindow(type,false,"Frame Debug");window.Show();
   // Use the exact Editor's own button path, including its connected profiler GUID.
   type.GetMethod("ClickEnableFrameDebugger",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(window,null);
   Log("enabled="+Call("IsLocalEnabled")+" remote="+Call("IsRemoteEnabled"));SaveGameView(Path.Combine(Dir,"frame-debugger-full.png"));next=EditorApplication.timeSinceStartup+.2;return;
  }
  if(EditorApplication.timeSinceStartup<next)return;
  if(events==null){
   events=(Array)Call("GetFrameEvents");if(events==null||events.Length==0){events=null;return;}selected=new List<int>();Log("events="+events.Length+" frame="+Time.frameCount);
   for(int i=0;i<Math.Min(events.Length,1024);i++){
    var e=events.GetValue(i);var go=e.GetType().GetField("gameObject").GetValue(e) as GameObject;Log("header event="+i+" "+Describe(e,2));if(go==null)continue;
    var fx=go.GetComponent<SFXUnit>();var rend=go.GetComponent<Renderer>();
    if(fx!=null||go.name.EndsWith("_Trail")){if(go.name.EndsWith("_Trail")||(fx!=null&&fx.source!=null&&!fx.source.Texture.ToUpperInvariant().Contains("LOCK")))selected.Insert(0,i);else selected.Add(i);Log("candidate event="+i+" renderer="+(rend==null?0:rend.GetInstanceID())+" source="+(fx==null||fx.source==null?"trail":fx.source.EffectName+" texture="+fx.source.Texture+" blend="+fx.source.BlendType));if(rend!=null)foreach(var m in rend.sharedMaterials)Log("material "+Describe(m,1)+" shader="+(m==null?"null":m.shader.name)+" queue="+(m==null?-1:m.renderQueue)+" texture="+(m==null?"null":Describe(m.mainTexture,1)));}
   }
   if(selected.Count==0){for(int i=1;i<Math.Min(events.Length,4);i++)selected.Add(i);Log("no SFX draw found; sampling ordinary mesh draws for API validation only");}
   if(selected.Count>6)selected.RemoveRange(6,selected.Count-6);
  }
  if(cursor>=selected.Count){Restore();return;}
  int index=selected[cursor];
  if(phase==0){before=Path.Combine(Dir,"event-"+index+"-before.png");after=Path.Combine(Dir,"event-"+index+"-after.png");Limit(index);phase=1;return;}
  if(phase==1){SaveGameView(before);Log("before request event="+index+" limit="+Utility.GetProperty("limit",Flags).GetValue(null,null));next=EditorApplication.timeSinceStartup+.15;phase=2;return;}
  if(phase==2){if(!File.Exists(before))return;Limit(index+1);phase=3;return;}
  if(phase==3){var type=typeof(Editor).Assembly.GetType("UnityEditorInternal.FrameDebuggerEventData");var data=Activator.CreateInstance(type);bool ok=(bool)Call("GetFrameEventData",index,data);Log("event="+index+" valid="+ok+" "+Describe(data,5));if(!ok){next=EditorApplication.timeSinceStartup+.1;return;}var component=EditorUtility.InstanceIDToObject((int)type.GetField("componentInstanceID").GetValue(data)) as Renderer; if(component!=null){var fx=component.GetComponent<SFXUnit>();Log("actual native renderer="+Describe(component,1)+" gameObject="+component.gameObject.name+" source="+(fx!=null&&fx.source!=null?fx.source.EffectName+" texture="+fx.source.Texture+" blend="+fx.source.BlendType:"none"));if(component.sharedMaterial!=null&&component.sharedMaterial.mainTexture!=null&&component.sharedMaterial.shader.name=="Custom/MeteorBlend4"&&(component.sharedMaterial.mainTexture.name.ToUpperInvariant().Contains("LIGHT")||component.sharedMaterial.mainTexture.name.ToUpperInvariant().Contains("HITP")))actualRenderer=component.GetInstanceID();}SaveGameView(after);phase=4;return;}
  if(phase==4){if(!File.Exists(after))return;Log("paired event="+index+" before="+before+" after="+after);cursor++;phase=0;}
 }catch(Exception e){Log("ERROR "+e);Restore();}}
}
#endif
