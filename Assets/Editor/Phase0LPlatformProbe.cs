#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class Phase0LPlatformProbe {
 public static void ConfigureAndPlay(){
  PlayerSettings.SetArchitecture(BuildTargetGroup.iOS,1);
  PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS,ScriptingImplementation.IL2CPP);
  PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS,false);
  PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS,new[]{UnityEngine.Rendering.GraphicsDeviceType.Metal});
  PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
  PlayerSettings.iOS.appleEnableAutomaticSigning=false;
  Run(false);Phase0CEditor.OpenPatch();
 }
 public static void Run(){Run(true);}
 static void Run(bool stop){
  string dir=Environment.GetEnvironmentVariable("PHASE0I_RUN_DIR");
  Action<string> note=s=>File.AppendAllText(Path.Combine(dir,"ios-platform-probe.txt"),DateTime.UtcNow.ToString("o")+" "+s+"\n");
  try{
   note("version="+UnityEditorInternal.InternalEditorUtility.GetFullUnityVersion());note("applicationContentsPath="+EditorApplication.applicationContentsPath);
   bool supported=BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS,BuildTarget.iOS);note("IsBuildTargetSupported.iOS="+supported);
   string path=BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.iOS,BuildOptions.None);note("PlaybackEngineDirectory="+path+" exists="+Directory.Exists(path));
   var type=typeof(Editor).Assembly.GetType("UnityEditor.Modules.ModuleManager");
   if(type!=null){foreach(var n in new[]{"GetTargetStringFromBuildTarget","IsPlatformSupportLoaded"}){
    foreach(var m in type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static))if(m.Name==n){
     try{var args=m.GetParameters();object value=args.Length==1?m.Invoke(null,new object[]{args[0].ParameterType==typeof(BuildTarget)?(object)BuildTarget.iOS:"iOS"}):null;note(n+"="+value);}catch(Exception e){note(n+" query error="+e.Message);}
    }
   }}
   var importer=AssetImporter.GetAtPath("Assets/Plugins/x86_64/slua.dll") as PluginImporter;
   note("WindowsSlua.iOS.enabled="+(importer!=null&&importer.GetCompatibleWithPlatform(BuildTarget.iOS)));
   note("architecture="+PlayerSettings.GetArchitecture(BuildTargetGroup.iOS)+" backend="+PlayerSettings.GetScriptingBackend(BuildTargetGroup.iOS)+" autoSigning="+PlayerSettings.iOS.appleEnableAutomaticSigning+" orientation="+PlayerSettings.defaultInterfaceOrientation+" graphics="+String.Join(",",Array.ConvertAll(PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS),x=>x.ToString())));
   foreach(var asm in AppDomain.CurrentDomain.GetAssemblies())if(asm.GetName().Name.Contains("iOS"))note("loaded="+asm.FullName+" file="+asm.Location);
   note("PROBE_COMPLETE supported="+supported);
  }catch(Exception e){note("PROBE_ERROR "+e);}
  if(stop)File.WriteAllText(Path.Combine(dir,"stop-editor"),"read-only iOS platform probe complete");
 }
}
#endif
