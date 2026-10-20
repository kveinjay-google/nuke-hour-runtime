using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meteor.Launcher {
 [UnityEngine.Scripting.Preserve]
 public sealed class MeteorLauncher : MonoBehaviour {
  sealed class ValidationRequest{public string Root;public readonly TaskCompletionSource<string> Completion=new TaskCompletionSource<string>();}
  ValidationRequest validation;bool destroyed,importProbe;float probeReadyAt;
  string ValidateOnMainThread(string path){if(destroyed)return "传输已结束，请重新开始";var request=new ValidationRequest{Root=path};if(System.Threading.Interlocked.CompareExchange(ref validation,request,null)!=null)return "正在检查文件，请稍候";return request.Completion.Task.Wait(30000)?request.Completion.Task.Result:"检查文件超时了，请保持本页打开后重试";}
  LanContentServer receiver;Task<string> job;string current,message="正在检查游戏文件…",folder="";bool help,oldSleep;int previousSleep;Texture2D backdrop;
  public static string Platform{get{
#if UNITY_IOS && !UNITY_EDITOR
   return "ios";
#else
   return "macos";
#endif
  }}
  public static string Storage{get{
#if UNITY_IOS && !UNITY_EDITOR
   return Path.Combine(Path.GetDirectoryName(Application.persistentDataPath),"Library","Application Support","Meteor","UserContent");
#else
   return Path.Combine(Application.persistentDataPath,"UserContent");
#endif
  }}
#if UNITY_IOS && !UNITY_EDITOR
  [DllImport("__Internal")]static extern void MeteorPickContentFolder();
  [DllImport("__Internal")]static extern void MeteorExcludeContentBackup(string path);
  [DllImport("__Internal")]static extern void MeteorEndContentFolderAccess();
#endif
  void Awake(){if(BodyFace!=null)LauncherStyle.FixedFont=BodyFace;gameObject.name="MeteorLauncher";backdrop=new Texture2D(1,1);backdrop.SetPixel(0,0,new Color(.045f,.065f,.085f));backdrop.Apply();Directory.CreateDirectory(Storage);
#if UNITY_IOS && !UNITY_EDITOR
   MeteorExcludeContentBackup(Storage);
#endif
   string root=Storage;job=Task.Run(()=>ContentInstall.ReadCurrent(root,Platform));if(NukeImportProbe.Requested){importProbe=!NukeImportProbe.HomePreview;NukeImportProbe.Begin();if(importProbe)StartReceive();}}
  void Update(){var request=System.Threading.Interlocked.Exchange(ref validation,null);if(request!=null){string problem=null;try{ContentResources.Mount(request.Root,Platform);}catch(Exception e){problem=e.Message;}finally{ContentResources.Unmount();request.Completion.TrySetResult(problem);}}if(job!=null&&job.IsCompleted){if(job.IsFaulted)message="导入失败："+job.Exception.GetBaseException().Message;else{current=job.Result;message=current==null?"还没有游戏文件":"游戏文件已准备好，可以开始游戏了";}job=null;
#if UNITY_IOS && !UNITY_EDITOR
   MeteorEndContentFolderAccess();
#endif
  }if(receiver!=null){message=receiver.Status;if(receiver.InstalledRoot!=null){current=receiver.InstalledRoot;if(importProbe&&probeReadyAt==0)probeReadyAt=Time.realtimeSinceStartup+2;if(importProbe&&job==null&&Time.realtimeSinceStartup>=probeReadyAt){importProbe=false;NukeImportProbe.Record("IMPORT_VALIDATED",current);StartGame();}}}}
  void StopReceive(){if(receiver!=null){receiver.Dispose();receiver=null;}if(oldSleep){Screen.sleepTimeout=previousSleep;oldSleep=false;}}
  static IPAddress LocalAddress(){return Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(a=>a.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a)&&Private(a));}
  static bool Private(IPAddress a){var b=a.GetAddressBytes();return b[0]==10||(b[0]==192&&b[1]==168)||(b[0]==172&&b[1]>=16&&b[1]<=31);}
  void StartReceive(){try{StopReceive();var ip=LocalAddress();if(ip==null)throw new InvalidDataException("未连接到可用网络，请先连接 Wi-Fi 再试");receiver=new LanContentServer(Storage,Platform,ip,ValidateOnMainThread);receiver.Start();if(importProbe)NukeImportProbe.Record("ADDRESS",receiver.Address);previousSleep=Screen.sleepTimeout;oldSleep=true;Screen.sleepTimeout=SleepTimeout.NeverSleep;}catch(Exception e){StopReceive();message=e.Message;}}
  [UnityEngine.Scripting.Preserve] public void FolderSelected(string path){folder=path;ImportFolder();}
  [UnityEngine.Scripting.Preserve] public void FolderSelectionCancelled(string unused){message="已取消选择";}
  void ImportFolder(){if(job!=null||receiver!=null)return;string source=folder,root=Storage,platform=Platform;message="正在导入游戏文件，请稍候…";job=Task.Run(()=>{if(!Directory.Exists(source)||(File.GetAttributes(source)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("请选择包含全部游戏文件的文件夹");using(var tx=new ContentInstall(root,platform)){foreach(string name in new[]{"content.manifest","resources.index","resources.bundle","scenes.bundle"}){string file=Path.Combine(source,name);if(!File.Exists(file)||(File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("找不到文件，或所选文件只是快捷方式："+name);using(var s=File.OpenRead(file))tx.Receive(name,s,s.Length,()=>false);}ContentInstall.Validate(tx.StageRoot,platform);string problem=ValidateOnMainThread(tx.StageRoot);if(problem!=null)throw new InvalidDataException(problem);return tx.Commit(()=>destroyed);}});}
  void StartGame(){try{StopReceive();ContentResources.Mount(current,Platform);if(NukeImportProbe.Requested)NukeImportProbe.Record("MOUNT","PASS");SceneManager.LoadScene("Patch");}catch(Exception e){ContentResources.Unmount();current=null;message="游戏启动失败："+e.Message;}}
  public Texture2D Background;public Font BodyFace,HeroFace;
  static readonly Color Gold=new Color(.86f,.70f,.46f),Ink=new Color(.065f,.035f,.055f),Ivory=new Color(.97f,.9f,.77f);
  GUIStyle Text(int size,Color color,bool center=false){return new GUIStyle(GUI.skin.label){font=BodyFace!=null?BodyFace:LauncherStyle.Font,fontSize=size,wordWrap=true,alignment=center?TextAnchor.MiddleCenter:TextAnchor.UpperLeft,normal={textColor=color}};}
  void Label(Rect r,string text,int size=24,bool center=false){var style=Text(size,Ivory,center);if(text=="重返江湖"&&HeroFace!=null)style.font=HeroFace;GUI.Label(r,text,style);}
  void Fill(Rect r,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
  void Rule(float x,float y,float w){Fill(new Rect(x,y,w,1),new Color(Gold.r,Gold.g,Gold.b,.4f));}
  bool Button(Rect r,string text,bool primary=false){bool click=GUI.Button(r,GUIContent.none,GUIStyle.none);Fill(r,Gold);Fill(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),primary?new Color(.56f,.13f,.11f):new Color(.09f,.045f,.065f,.97f));GUI.Label(r,text,Text(25,GUI.enabled?Ivory:new Color(.5f,.46f,.4f),true));return click;}
  void Computer(float x,float y){Fill(new Rect(x,y,64,45),Gold);Fill(new Rect(x+3,y+3,58,39),Ink);Fill(new Rect(x+30,y+45,4,12),Gold);Fill(new Rect(x+12,y+57,40,3),Gold);}
  void Folder(float x,float y){Fill(new Rect(x,y,30,12),Gold);Fill(new Rect(x,y+8,65,43),Gold);Fill(new Rect(x+3,y+12,59,36),Ink);}
  void OnGUI(){
   if(Background!=null)GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Background,ScaleMode.ScaleAndCrop);else Fill(new Rect(0,0,Screen.width,Screen.height),Ink);
   if(CustomCombatControls.Editing){CustomCombatControls.DrawSettings();return;}
   Matrix4x4 old=GUI.matrix;float scale=Mathf.Min(Screen.width/1280f,Screen.height/960f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-960*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
   GUI.Label(new Rect(58,38,550,82),"NUKE HOUR",Text(52,Gold));
   if(GUI.Button(new Rect(1010,45,90,48),"设置",Text(23,Gold,true)))CustomCombatControls.OpenSettings();if(GUI.Button(new Rect(1110,45,100,48),"帮助",Text(23,Gold,true)))help=!help;
   if(receiver!=null)TransferPage();else if(help)HelpPage();else if(current!=null&&!NukeImportProbe.HomePreview)ReadyPage();else HomePage();
   if(GUI.Button(new Rect(62,886,400,44),"NUKE HOUR 官网 ↗  nukehour.com",Text(20,Gold)))Application.OpenURL("https://nukehour.com");
   GUI.Label(new Rect(850,892,365,35),"本应用不附带游戏资源",Text(18,new Color(.65f,.58f,.51f)));GUI.matrix=old;
  }
  void HomePage(){
   Fill(new Rect(720,125,505,730),new Color(.055f,.025f,.045f,.95f));
   Label(new Rect(155,390,540,125),"重返江湖",90);Label(new Rect(165,505,510,60),"导入游戏文件，即可开始",25);
   Label(new Rect(770,155,405,70),"导入游戏文件",40);Rule(770,242,405);
   Computer(775,290);Label(new Rect(865,285,315,45),"从电脑传入",30);Label(new Rect(865,337,330,45),"打开网页，直接上传",21);
   GUI.enabled=job==null;if(Button(new Rect(775,398,400,66),"开始传输  ›",true))StartReceive();Rule(775,510,400);
   Folder(775,558);Label(new Rect(865,550,315,45),"选择本机文件",30);Label(new Rect(865,600,310,62),"从“文件”中选择游戏文件夹",21);
   if(Button(new Rect(775,680,400,66),"选择文件夹  ›"))PickFolder();GUI.enabled=true;
   if(GUI.Button(new Rect(775,790,400,40),"如何准备游戏文件？  ›",Text(22,Gold)))help=true;
   if(job!=null||message!="还没有游戏文件")Label(new Rect(90,748,595,110),message,21);
  }
  void PickFolder(){
#if UNITY_IOS && !UNITY_EDITOR
   MeteorPickContentFolder();
#elif UNITY_EDITOR
   folder=UnityEditor.EditorUtility.OpenFolderPanel("选择游戏文件夹","","");if(!String.IsNullOrEmpty(folder))ImportFolder();
#endif
  }
  void Panel(){Fill(new Rect(170,190,940,650),new Color(.055f,.025f,.045f,.96f));Rule(170,190,940);Rule(170,840,940);}
  void TransferPage(){Panel();if(GUI.Button(new Rect(205,210,120,42),"‹ 返回",Text(23,Gold))){StopReceive();return;}
   Label(new Rect(290,265,700,70),"从电脑传入",44,true);Label(new Rect(250,350,780,45),"电脑和 iPad 连接同一个 Wi-Fi，即可传输",25,true);
   Label(new Rect(245,430,790,45),"①  在电脑浏览器打开此网址",27);Fill(new Rect(245,488,790,82),new Color(.12f,.075f,.09f));Label(new Rect(265,505,565,50),receiver.Address,29);if(Button(new Rect(855,500,163,56),"复制网址"))GUIUtility.systemCopyBuffer=receiver.Address;
   Label(new Rect(245,600,790,45),"②  选择游戏文件，直接上传",27);Rule(245,665,790);
   Label(new Rect(245,690,540,55),message,23);Label(new Rect(245,752,560,60),"已收到 "+(receiver.ReceivedBytes/(1024f*1024f)).ToString("F1")+" MB · 保持本页打开，不要锁屏",19);if(Button(new Rect(845,743,190,58),"结束传输")){StopReceive();message=current!=null?"游戏文件已准备好，可以开始游戏了":"还没有游戏文件";}
  }
  void ReadyPage(){Panel();Label(new Rect(260,300,760,90),"游戏文件已就绪",47,true);Label(new Rect(260,415,760,70),"导入完成，现在可以开始游戏了",25,true);Rule(300,520,680);GUI.enabled=job==null;if(Button(new Rect(340,585,600,78),"开始游戏",true))StartGame();GUI.enabled=true;
   if(GUI.Button(new Rect(340,715,280,55),"更换游戏文件",Text(23,Gold,true))){current=null;help=false;message="请选择新的游戏文件，原有文件会保留到导入成功";}if(GUI.Button(new Rect(675,715,265,55),"按键设置",Text(23,Gold,true)))CustomCombatControls.OpenSettings();
  }
  void HelpPage(){Panel();Label(new Rect(235,235,805,65),"如何准备游戏文件",40);Label(new Rect(235,335,805,350),"请使用你自己拥有的游戏文件，本应用不提供游戏下载。\n\n目前需要先用配套工具整理游戏文件；原版电脑安装文件夹不能直接导入。整理后的文件夹应包含以下四个文件：\n\ncontent.manifest    resources.index\nresources.bundle    scenes.bundle\n\n准备好后，可以从电脑网页传入，或在设备上选择整个文件夹。导入失败不会覆盖原有文件或存档。",24);if(Button(new Rect(235,735,250,62),"返回导入"))help=false;}
  void OnApplicationPause(bool p){if(p)StopReceive();}void OnApplicationFocus(bool f){if(!f)StopReceive();}void OnDestroy(){destroyed=true;var pending=System.Threading.Interlocked.Exchange(ref validation,null);if(pending!=null)pending.Completion.TrySetResult("传输已结束，请重新开始");StopReceive();if(backdrop!=null)Destroy(backdrop);}
 }
}
