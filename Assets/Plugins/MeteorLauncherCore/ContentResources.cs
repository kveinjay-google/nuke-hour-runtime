using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Meteor.Launcher {
 public static class ContentResources {
  [Serializable] public sealed class Entry {public string key,asset;}
  [Serializable] public sealed class Index {public Entry[] entries;}
  static AssetBundle resources,scenes;static readonly Dictionary<string,List<string>> paths=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase);
  static readonly Dictionary<string,Shader> shaders=new Dictionary<string,Shader>(StringComparer.Ordinal);
  public static Shader FindShader(string name){Shader shader;if(!Mounted||!shaders.TryGetValue(name,out shader))shader=Shader.Find(name);if(shader==null)throw new InvalidOperationException("游戏文件缺少渲染程序："+name+"，请重新准备并导入游戏文件");if(!shader.isSupported)throw new InvalidOperationException("此设备无法使用游戏文件中的渲染程序："+name+"，请使用适配此平台的游戏文件");return shader;}
  public static bool Mounted{get{return resources!=null&&scenes!=null;}}
  public static void Mount(string root,string platform){
   if(Mounted)throw new InvalidOperationException("请先退出游戏，再更换游戏文件");ContentInstall.Validate(root,platform);
   try{var index=JsonUtility.FromJson<Index>(File.ReadAllText(Path.Combine(root,"resources.index")));if(index==null||index.entries==null||index.entries.Length>50000)throw new InvalidDataException("文件清单无法读取，请重新准备游戏文件");
    resources=AssetBundle.LoadFromFile(Path.Combine(root,"resources.bundle"));scenes=AssetBundle.LoadFromFile(Path.Combine(root,"scenes.bundle"));if(resources==null||scenes==null||!scenes.GetAllScenePaths().Any(p=>Path.GetFileNameWithoutExtension(p)=="Patch"))throw new InvalidDataException("游戏文件无法打开，或缺少启动所需的内容，请重新导入");
    foreach(var e in index.entries){if(e==null||String.IsNullOrEmpty(e.key)||String.IsNullOrEmpty(e.asset)||e.key.Contains("..")||!resources.Contains(e.asset))throw new InvalidDataException("文件清单与游戏内容不一致，请重新准备游戏文件");List<string> list;if(!paths.TryGetValue(e.key,out list))paths[e.key]=list=new List<string>();list.Add(e.asset);}
    var bundledShaders=resources.LoadAllAssets<Shader>();
    foreach(var sh in bundledShaders)shaders[sh.name]=sh;
    foreach(var sh in bundledShaders)Debug.Log("[NUKE_SHADER] bundled="+sh.name+" supported="+sh.isSupported);
    foreach(string name in new[]{"Unlit/Texture","UnlitAdditive","Mobile/Particles/Alpha Blended","Custom/MeteorBlend4","UI/Default"}){var sh=Shader.Find(name);Debug.Log("[NUKE_SHADER] find="+name+" present="+(sh!=null)+" supported="+(sh!=null&&sh.isSupported));var resolved=FindShader(name);Debug.Log("[NUKE_SHADER] resolved="+name+" supported="+resolved.isSupported);}
   }catch{Unmount();throw;}
  }
  public static void Unmount(){shaders.Clear();paths.Clear();if(scenes!=null)scenes.Unload(true);if(resources!=null)resources.Unload(true);scenes=null;resources=null;}
  public static UnityEngine.Object Load(string path){return Load(path,typeof(UnityEngine.Object));}
  public static T Load<T>(string path)where T:UnityEngine.Object{return Load(path,typeof(T))as T;}
  public static UnityEngine.Object Load(string path,Type type){
   if(Mounted){List<string> candidates;if(path!=null&&paths.TryGetValue(path.Replace('\\','/'),out candidates))foreach(var address in candidates){var obj=resources.LoadAsset(address,type);if(obj!=null)return obj;}return null;}
#if METEOR_RESOURCE_FREE_RELEASE
   throw new InvalidOperationException("请先回到启动器，导入游戏文件后再开始游戏");
#else
   return UnityEngine.Resources.Load(path,type);
#endif
  }
 }
}
