#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Meteor.Launcher;
public static class MeteorLauncherProbe {
 public static void CompileAndInspect(){
  string output=Environment.GetEnvironmentVariable("METEOR_LAUNCHER_EVIDENCE");if(String.IsNullOrEmpty(output))throw new InvalidOperationException("Evidence directory required");Directory.CreateDirectory(output);
  var game=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Dialogs/FightWnd.prefab");if(game==null)throw new Exception("FightWnd prefab missing");
  var labels=ControlProfile.Default().bindings.Select(b=>b.id+":"+CustomCombatControls.Label(b.action)).ToArray();
  File.WriteAllLines(Path.Combine(output,"unity-probe.txt"),new[]{"ExactUnity="+UnityEditorInternal.InternalEditorUtility.GetFullUnityVersion(),"CSharpCompile=PASS","DefenceNode="+(NodeHelper.Find("Defence",game)!=null),"JumpNode="+(NodeHelper.Find("Jump",game)!=null),"LongJumpMinimumHold=0.18","Runtime="+ContentInstall.RuntimeVersion}.Concat(labels).ToArray());
  MeteorResourceBuild.CreateLauncherScene();Debug.Log("METEOR_LAUNCHER_COMPILE_PROBE_PASS");
 }
}
#endif
