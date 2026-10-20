using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace Meteor.Launcher {
 // Data-only transaction. No Unity calls: network/background operations may use this class.
 public sealed class ContentInstall : IDisposable {
  public const string RuntimeVersion="2020.1.0f1-2ab9c4179772";
  public const long MaxFileBytes=2L*1024*1024*1024,MaxSessionBytes=6L*1024*1024*1024;
  static readonly string[] Required={"resources.bundle","scenes.bundle","resources.index"};
  readonly Func<string,long> freeBytes;readonly string root,platform;long received;bool disposed,committed;
  public string StageRoot {get;private set;}
  public string CurrentRoot {get{return ReadCurrent(root,platform);}}
  public long ReceivedBytes {get{return System.Threading.Interlocked.Read(ref received);}}
  public ContentInstall(string storageRoot,string targetPlatform,Func<string,long> availableBytes=null) {
   freeBytes=availableBytes??AvailableBytes;
   root=Path.GetFullPath(storageRoot);platform=targetPlatform;
   Directory.CreateDirectory(root);NoLinks(root);Directory.CreateDirectory(Path.Combine(root,"staging"));NoLinks(Path.Combine(root,"staging"));
   StageRoot=Path.Combine(root,"staging",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(StageRoot);
  }
#if UNITY_IOS && !UNITY_EDITOR
  [System.Runtime.InteropServices.DllImport("__Internal")]static extern long MeteorAvailableContentBytes(string path);
#endif
  static long AvailableBytes(string path){
#if UNITY_IOS && !UNITY_EDITOR
   return MeteorAvailableContentBytes(path);
#else
   return new DriveInfo(Path.GetPathRoot(path)).AvailableFreeSpace;
#endif
  }
  void CheckSpace(){if(freeBytes(root)<128L*1024*1024)throw new InvalidDataException("存储空间不足，请腾出空间后再试");}
  static void NoLinks(string p) {if((new DirectoryInfo(p).Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("请选择实际存放游戏文件的文件夹，不要选择快捷方式");}
  static void PlainFile(string p){if((File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("请选择原文件，不要选择快捷方式");}
  public static bool Allowed(string name){return name=="content.manifest"||Required.Contains(name);}
  public void Receive(string name,Stream input,long length,Func<bool> cancelled) {
   if(disposed||committed)throw new InvalidDataException("本次导入已结束，请重新开始");
   if(!Allowed(name)||name!=Path.GetFileName(name)||length<=0||length>MaxFileBytes||received>MaxSessionBytes-length)throw new InvalidDataException("文件不受支持或过大，请重新选择完整的游戏文件");
   if((name=="content.manifest"&&length>65536)||(name=="resources.index"&&length>8*1024*1024))throw new InvalidDataException("文件清单过大，无法导入，请重新准备游戏文件");
   CheckSpace();NoLinks(StageRoot);string destination=Path.Combine(StageRoot,name);if(File.Exists(destination))throw new InvalidDataException("这个文件已经传过了，请重新开始导入");
   string temp=Path.Combine(StageRoot,Guid.NewGuid().ToString("N")+".part");long copied=0;
   try {
    var buffer=new byte[65536];using(var output=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
     while(copied<length){if(cancelled())throw new OperationCanceledException();int count=input.Read(buffer,0,(int)Math.Min(buffer.Length,length-copied));if(count==0)throw new InvalidDataException("文件没有传完，请重新导入");output.Write(buffer,0,count);copied+=count;
      if(copied%(4*1024*1024)<buffer.Length)CheckSpace();
     }output.Flush();
    }
    File.Move(temp,destination);System.Threading.Interlocked.Add(ref received,copied);
   } finally {if(File.Exists(temp))File.Delete(temp);}
  }
  public string Commit(Func<bool> cancelled=null){
   if(disposed||committed)throw new InvalidDataException("本次导入已结束，请重新开始");Validate(StageRoot,platform,cancelled);if(cancelled!=null&&cancelled())throw new OperationCanceledException();
   string installs=Path.Combine(root,"installs");Directory.CreateDirectory(installs);NoLinks(installs);
   string id=Guid.NewGuid().ToString("N"),installed=Path.Combine(installs,id),pointer=Path.Combine(root,"current.txt"),pending=Path.Combine(root,"current-"+id+".tmp");
   Directory.Move(StageRoot,installed);
   try {
    using(var f=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None))using(var w=new StreamWriter(f,Encoding.ASCII)){w.Write(id);w.Flush();f.Flush(true);}
    if(File.Exists(pointer)){PlainFile(pointer);File.Replace(pending,pointer,null);}else File.Move(pending,pointer);
    committed=true;return installed;
   }catch{Directory.Delete(installed,true);throw;}finally{if(File.Exists(pending))File.Delete(pending);}
  }
  public static string ReadCurrent(string storageRoot,string targetPlatform){
   try{NoLinks(storageRoot);string marker=Path.Combine(storageRoot,"current.txt");if(!File.Exists(marker))return null;PlainFile(marker);if(new FileInfo(marker).Length>64)return null;
    string id=File.ReadAllText(marker).Trim();Guid parsed;if(!Guid.TryParseExact(id,"N",out parsed))return null;
    NoLinks(Path.Combine(storageRoot,"installs"));string installed=Path.Combine(storageRoot,"installs",id);Validate(installed,targetPlatform);return installed;
   }catch(InvalidDataException){return null;}catch(IOException){return null;}catch(UnauthorizedAccessException){return null;}
  }
  static string Hash(string p,Func<bool> cancelled){using(var s=File.OpenRead(p))using(var h=SHA256.Create()){var buffer=new byte[65536];int count;while((count=s.Read(buffer,0,buffer.Length))>0){if(cancelled!=null&&cancelled())throw new OperationCanceledException();h.TransformBlock(buffer,0,count,buffer,0);}h.TransformFinalBlock(buffer,0,0);return BitConverter.ToString(h.Hash).Replace("-","").ToLowerInvariant();}}
  public static void Validate(string path,string targetPlatform,Func<bool> cancelled=null){
   if(!Directory.Exists(path))throw new InvalidDataException("还没有导入游戏文件");NoLinks(path);
   string manifest=Path.Combine(path,"content.manifest");if(!File.Exists(manifest))throw new InvalidDataException("缺少 content.manifest，请选择整理好的完整游戏文件夹");PlainFile(manifest);
   if(new FileInfo(manifest).Length>65536)throw new InvalidDataException("文件清单过大，无法导入，请重新准备游戏文件");var lines=File.ReadAllLines(manifest);
   if(lines.Length!=6||lines[0]!="METEOR_CONTENT_V1"||lines[1]!=RuntimeVersion||lines[2]!=targetPlatform)throw new InvalidDataException("这份游戏文件不适用于当前设备或应用版本，请重新准备后导入");
   var seen=new HashSet<string>(StringComparer.Ordinal);long total=0;
   foreach(string line in lines.Skip(3)){
    string[] bits=line.Split('\t');long length;
    if(bits.Length!=3||!Required.Contains(bits[2])||!seen.Add(bits[2])||bits[0].Length!=64||bits[0].Any(c=>!((c>='0'&&c<='9')||(c>='a'&&c<='f')))||!Int64.TryParse(bits[1],out length)||length<=0||length>MaxFileBytes)throw new InvalidDataException("文件清单无法读取，请重新准备游戏文件");
    string file=Path.Combine(path,bits[2]);if(!File.Exists(file))throw new InvalidDataException("游戏文件不完整，缺少："+bits[2]);PlainFile(file);
    if(new FileInfo(file).Length!=length||Hash(file,cancelled)!=bits[0])throw new InvalidDataException("文件已损坏或没有传完，请重新导入："+bits[2]);total+=length;
    if(bits[2]=="resources.index"&&length>8*1024*1024)throw new InvalidDataException("文件清单过大，无法导入，请重新准备游戏文件");
    if(bits[2].EndsWith(".bundle")){using(var f=File.OpenRead(file)){byte[] magic=new byte[8];if(f.Read(magic,0,8)!=8||Encoding.ASCII.GetString(magic)!="UnityFS\0")throw new InvalidDataException("无法读取游戏文件，请重新准备后导入："+bits[2]);}}
   }
   if(total>MaxSessionBytes||Directory.GetFiles(path).Any(f=>!Allowed(Path.GetFileName(f)))||Directory.GetDirectories(path).Length!=0)throw new InvalidDataException("文件夹中含有多余文件，请选择整理好的游戏文件夹");
  }
  public void Dispose(){if(disposed)return;disposed=true;if(!committed&&Directory.Exists(StageRoot))Directory.Delete(StageRoot,true);}
 }
}
