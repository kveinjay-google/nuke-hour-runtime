#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using Meteor.Launcher;

public static class MeteorResourceBuild {
 const string Scene="Assets/LauncherOnly.unity";
 static readonly string[] OwnAssets={"Assets/LauncherArt/app-icon.png","Assets/LauncherArt/volcanic-night.png","Assets/LauncherArt/Fonts/NotoSerifCJKsc-Regular.otf","Assets/LauncherArt/Fonts/MaShanZheng-Regular.ttf","Assets/LauncherArt/Fonts/Noto-OFL.txt","Assets/LauncherArt/Fonts/MaShanZheng-OFL.txt"};

 static void Exact(){if(!UnityEditorInternal.InternalEditorUtility.GetFullUnityVersion().Contains("2020.1.0f1 (2ab9c4179772)"))throw new InvalidOperationException("Exact Unity required");}
 public static void PrepareComponentPreservation(){
  var types=AssetDatabase.FindAssets("t:MonoScript").Select(AssetDatabase.GUIDToAssetPath).Where(p=>!p.Contains("/Editor/")).Select(p=>AssetDatabase.LoadAssetAtPath<MonoScript>(p)).Where(x=>x!=null).Select(x=>x.GetClass()).Where(t=>t!=null&&typeof(UnityEngine.Object).IsAssignableFrom(t)&&!t.Assembly.GetName().Name.Contains("Editor")).Distinct().ToArray();
  string path="Assets/PortPreparation/Launcher/Preservation/link.xml";Directory.CreateDirectory(Path.GetDirectoryName(path));var text=new StringBuilder("<!-- Generated runtime component contract; contains no game assets. -->\n<linker>\n");
  foreach(var group in types.GroupBy(t=>t.Assembly.GetName().Name)){text.Append("<assembly fullname=\""+group.Key+"\">\n");foreach(var t in group)text.Append("<type fullname=\""+System.Security.SecurityElement.Escape(t.FullName)+"\" preserve=\"all\"/>\n");text.Append("</assembly>\n");}text.Append("</linker>\n");if(!File.Exists(path)||File.ReadAllText(path)!=text.ToString())File.WriteAllText(path,text.ToString());AssetDatabase.ImportAsset(path);
 }
 public static void CreateLauncherScene(){Exact();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var launcher=new GameObject("MeteorLauncher").AddComponent<MeteorLauncher>();launcher.Background=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LauncherArt/volcanic-night.png");launcher.BodyFace=AssetDatabase.LoadAssetAtPath<Font>("Assets/LauncherArt/Fonts/NotoSerifCJKsc-Regular.otf");launcher.HeroFace=AssetDatabase.LoadAssetAtPath<Font>("Assets/LauncherArt/Fonts/MaShanZheng-Regular.ttf");if(launcher.BodyFace==null||launcher.HeroFace==null)throw new InvalidOperationException("Fixed launcher fonts missing");var camera=new GameObject("LauncherCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.045f,.065f,.085f);camera.cullingMask=0;EditorSceneManager.SaveScene(scene,Scene);}
 // Invoked explicitly on the resource owner's local source project. Output is not redistributable.
 [MenuItem("Meteor/用户资源/生成自用资源包")]
 public static void MakeOwnedContent(){var output=EditorUtility.SaveFolderPanel("选择新的自用资源包输出目录（勿分发游戏资源）","","");if(String.IsNullOrEmpty(output))return;ExportContent(output);}
 public static void ExportContent(string output){Exact();PrepareComponentPreservation();if(Directory.Exists(output)&&Directory.EnumerateFileSystemEntries(output).Any())throw new InvalidOperationException("输出目录必须为空");Directory.CreateDirectory(output);
  var entries=new List<ContentResources.Entry>();var assets=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(string path in AssetDatabase.GetAllAssetPaths().Where(x=>x.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(x))){int marker=path.LastIndexOf("/Resources/",StringComparison.Ordinal);if(marker<0)continue;string ext=Path.GetExtension(path).ToLowerInvariant();if(new[]{".cs",".dll",".asmdef",".asmref"}.Contains(ext))continue;string key=path.Substring(marker+11);key=key.Substring(0,key.Length-ext.Length);assets.Add(path);entries.Add(new ContentResources.Entry{key=key,asset=path.ToLowerInvariant()});}
  // Only enabled game scenes; dependencies include their shaders automatically.
  // ShaderMng dynamically resolves the already indexed Shader/Resources assets.
  string[] scenes=EditorBuildSettings.scenes.Where(x=>x.enabled&&File.Exists(x.path)).Select(x=>x.path).ToArray();if(!scenes.Any(p=>Path.GetFileNameWithoutExtension(p)=="Patch"))throw new InvalidDataException("Patch missing");
  var build=new[]{new AssetBundleBuild{assetBundleName="resources.bundle",assetNames=assets.ToArray()},new AssetBundleBuild{assetBundleName="scenes.bundle",assetNames=scenes}};
  var manifest=BuildPipeline.BuildAssetBundles(output,build,BuildAssetBundleOptions.ChunkBasedCompression|BuildAssetBundleOptions.StrictMode,EditorUserBuildSettings.activeBuildTarget);if(manifest==null)throw new InvalidOperationException("资源包构建失败");
  File.WriteAllText(Path.Combine(output,"resources.index"),JsonUtility.ToJson(new ContentResources.Index{entries=entries.ToArray()}),new UTF8Encoding(false));
  string platform=EditorUserBuildSettings.activeBuildTarget==BuildTarget.iOS?"ios":EditorUserBuildSettings.activeBuildTarget==BuildTarget.StandaloneOSX?"macos":null;if(platform==null)throw new InvalidOperationException("Only iOS/macOS resource profiles supported");
  var text=new StringBuilder("METEOR_CONTENT_V1\n"+ContentInstall.RuntimeVersion+"\n"+platform+"\n");
  foreach(string name in new[]{"resources.bundle","scenes.bundle","resources.index"}){var p=Path.Combine(output,name);string hash;using(var file=File.OpenRead(p))using(var h=SHA256.Create())hash=BitConverter.ToString(h.ComputeHash(file)).Replace("-","").ToLowerInvariant();text.Append(hash+"\t"+new FileInfo(p).Length+"\t"+name+"\n");}
  // Extra Unity bundle manifests remain in output for diagnostics; import only the four listed files.
  File.WriteAllText(Path.Combine(output,"content.manifest"),text.ToString(),new UTF8Encoding(false));
 }
 public static void BuildResourceFree(){Exact();PrepareComponentPreservation();if(Environment.GetEnvironmentVariable("METEOR_RELEASE_STAGING")!="YES")throw new InvalidOperationException("Use isolated resource-free staging only");
  string[] unexpected=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(p)&&!p.EndsWith(".cs")&&!p.EndsWith(".dll")&&!p.EndsWith(".a")&&!p.EndsWith(".so")&&!p.EndsWith(".mm")&&!p.EndsWith(".h")&&!p.EndsWith(".c")&&!p.EndsWith(".cpp")&&!p.EndsWith(".asmdef")&&!p.EndsWith(".asmref")&&p!="Assets/mcs.rsp"&&!(p.EndsWith("/link.xml"))&&p!=Scene&&!OwnAssets.Contains(p)).ToArray();if(unexpected.Length>0)throw new InvalidOperationException("发布输入包含未批准资源："+String.Join(",",unexpected.Take(20).ToArray()));
  PlayerSettings.productName="NUKE HOUR";var appIcon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LauncherArt/app-icon.png");if(appIcon==null)throw new InvalidOperationException("Launcher icon missing");int iconCount=PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.iOS).Length;if(iconCount==0)throw new InvalidOperationException("No iOS icon slots");PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS,Enumerable.Repeat(appIcon,iconCount).ToArray());CreateLauncherScene();EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};PlayerSettings.SetPreloadedAssets(new UnityEngine.Object[0]);
  if(!PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS).Split(';').Contains("METEOR_RESOURCE_FREE_RELEASE"))PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS,PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS)+";METEOR_RESOURCE_FREE_RELEASE");PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS,ScriptingImplementation.IL2CPP);PlayerSettings.SetArchitecture(BuildTargetGroup.iOS,1);PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;PlayerSettings.iOS.appleEnableAutomaticSigning=false;PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS,new[]{UnityEngine.Rendering.GraphicsDeviceType.Metal});
  string output=Environment.GetEnvironmentVariable("METEOR_RESOURCE_FREE_OUTPUT");if(String.IsNullOrEmpty(output)||!Path.IsPathRooted(output)||Directory.Exists(output))throw new InvalidOperationException("New absolute output required");
  var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=output,target=BuildTarget.iOS,options=BuildOptions.None});var packed=r.packedAssets.SelectMany(a=>a.contents).Select(a=>a.sourceAssetPath).Distinct().ToArray();File.WriteAllLines(output+".packed-assets.txt",packed);
  var leaked=packed.Where(a=>a.StartsWith("Assets/")&&a!=Scene&&!OwnAssets.Contains(a)&&!a.EndsWith(".cs")&&!a.EndsWith(".dll")).ToArray();if(leaked.Length>0)throw new InvalidOperationException("发布包含未批准资源："+String.Join(",",leaked));
  File.WriteAllText(output+".report.json",JsonUtility.ToJson(new Summary{result=r.summary.result.ToString(),errors=r.summary.totalErrors,warnings=r.summary.totalWarnings,bytes=r.summary.totalSize,seconds=r.summary.totalTime.TotalSeconds},true));if(r.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Resource-free build failed");
 }
 [Serializable]class Summary{public string result;public int errors,warnings;public ulong bytes;public double seconds;}
 [PostProcessBuild(200)]public static void NetworkPrivacy(BuildTarget target,string path){if(target!=BuildTarget.iOS)return;string p=Path.Combine(path,"Info.plist");var doc=new PlistDocument();doc.ReadFromFile(p);doc.root.SetString("NSLocalNetworkUsageDescription","用于接收同一 Wi-Fi 下电脑传来的游戏文件。");doc.WriteToFile(p);string licenses=Path.Combine(path,"Data","Raw","LauncherFontLicenses");Directory.CreateDirectory(licenses);foreach(string fontLicense in OwnAssets.Where(x=>x.EndsWith("-OFL.txt")))File.Copy(fontLicense,Path.Combine(licenses,Path.GetFileName(fontLicense)),true);}
}
#endif
