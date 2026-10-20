using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Meteor.Launcher {
 // Explicit local QA launch argument only. Uses the real receiver, validation and scene loader.
 [UnityEngine.Scripting.Preserve] public sealed class NukeImportProbe:MonoBehaviour {
#if UNITY_IOS && !UNITY_EDITOR
  [DllImport("__Internal")]static extern int MeteorHasLaunchArgument(string value);
#endif
  public static bool HasArgument(string value){
#if UNITY_IOS && !UNITY_EDITOR
   return MeteorHasLaunchArgument(value)!=0;
#else
   return Array.IndexOf(Environment.GetCommandLineArgs(),value)>=0;
#endif
  }
  public static bool HomePreview{get{return HasArgument("--nukehour-preview-home");}}
  public static bool Requested{get{return HasArgument("--nukehour-import-test")||HomePreview;}}
  public static void Record(string kind,string value){string line=DateTime.UtcNow.ToString("O")+" [NUKE_IMPORT_TEST] "+kind+" "+value;Debug.Log(line);try{File.AppendAllText(Path.Combine(Application.persistentDataPath,"nuke-import-test.log"),line+"\n");}catch(Exception e){Debug.LogWarning(e.Message);}}
  float started,lastCapture;int captures;
  public static void Begin(){var go=new GameObject("NukeImportProbe");DontDestroyOnLoad(go);go.AddComponent<NukeImportProbe>();}
  void Awake(){if(HasArgument("--nukehour-combat-test"))gameObject.AddComponent<CombatVisualProbe>();started=Time.realtimeSinceStartup;SceneManager.sceneLoaded+=Loaded;Record("BEGIN","Direct web import; auto-start after real validation; no gameplay mutation");}
  void Loaded(Scene s,LoadSceneMode mode){Record("SCENE",s.name);lastCapture=Time.realtimeSinceStartup-11;}
  void Update(){float elapsed=Time.realtimeSinceStartup-started;if(elapsed>3&&Time.realtimeSinceStartup-lastCapture>12&&captures<8){lastCapture=Time.realtimeSinceStartup;string path=Path.Combine(Application.persistentDataPath,"nuke-import-"+captures+".png");
#if UNITY_IOS && !UNITY_EDITOR
ScreenCapture.CaptureScreenshot("nuke-import-"+captures+".png");
#else
ScreenCapture.CaptureScreenshot(path);
#endif
Record("CAPTURE",path+" activeScene="+SceneManager.GetActiveScene().name+" mounted="+ContentResources.Mounted+" main="+(Main.Ins!=null));captures++;}if(elapsed>58){Record("END","60-second observation window complete");Destroy(gameObject);}}
  void OnDestroy(){SceneManager.sceneLoaded-=Loaded;}
 }
}
