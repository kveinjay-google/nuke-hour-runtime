#if UNITY_EDITOR
using System;using System.IO;using UnityEditor;using UnityEngine;using Meteor.Launcher;
[InitializeOnLoad]public static class MeteorLauncherPlayProbe {
 static MeteorLauncherPlayProbe(){EditorApplication.update+=Tick;}
 public static void Run(){string output=Environment.GetEnvironmentVariable("METEOR_LAUNCHER_EVIDENCE");if(String.IsNullOrEmpty(output))throw new Exception("Evidence directory required");Directory.CreateDirectory(output);MeteorResourceBuild.PrepareComponentPreservation();MeteorResourceBuild.CreateLauncherScene();var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.position=new Rect(80,80,1280,720);view.Show();view.Focus();SessionState.SetString("MeteorLauncherProbeOutput",output);SessionState.SetFloat("MeteorLauncherProbeStart",(float)EditorApplication.timeSinceStartup);SessionState.SetInt("MeteorLauncherProbeStep",0);EditorApplication.isPlaying=true;}
 static void Tick(){string output=SessionState.GetString("MeteorLauncherProbeOutput","");if(output=="")return;double elapsed=EditorApplication.timeSinceStartup-SessionState.GetFloat("MeteorLauncherProbeStart",0);int step=SessionState.GetInt("MeteorLauncherProbeStep",0);
  if(elapsed>25){File.WriteAllText(Path.Combine(output,"play-probe-timeout.txt"),"Play probe timed out");SessionState.EraseString("MeteorLauncherProbeOutput");EditorApplication.isPlaying=false;EditorApplication.Exit(2);return;}
  if(!EditorApplication.isPlaying){if(step==3){SessionState.EraseString("MeteorLauncherProbeOutput");EditorApplication.Exit(0);}return;}
  if(elapsed>3&&step==0){if(Main.Ins!=null||ContentResources.Mounted)throw new Exception("Fresh launcher initialized game unexpectedly");ScreenCapture.CaptureScreenshot(Path.Combine(output,"launcher-game-view.png"));SessionState.SetInt("MeteorLauncherProbeStep",1);}
  if(elapsed>6&&step==1&&File.Exists(Path.Combine(output,"launcher-game-view.png"))){CustomCombatControls.OpenSettings();SessionState.SetInt("MeteorLauncherProbeStep",2);}
  if(elapsed>8&&step==2){ScreenCapture.CaptureScreenshot(Path.Combine(output,"control-settings-game-view.png"));SessionState.SetInt("MeteorLauncherProbeStep",3);}
  if(elapsed>11&&step==3&&File.Exists(Path.Combine(output,"control-settings-game-view.png"))){File.WriteAllText(Path.Combine(output,"play-probe.txt"),"PASS: Launcher Play Mode, Main.Ins null, external resources unmounted, settings rendered.\nNo gameplay or iPad claim.\n");EditorApplication.isPlaying=false;}
 }
}
#endif
