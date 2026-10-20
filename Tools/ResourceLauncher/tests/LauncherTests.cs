using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Meteor.Launcher;
class LauncherTests {
 static int checks;
 static void Check(bool b,string n){checks++;if(!b)throw new Exception(n);}
 static void Reject(Action a,string n){bool rejected=false;try{a();}catch(InvalidDataException){rejected=true;}Check(rejected,n);}
 static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
 static string Manifest(byte[] a,byte[] b,byte[] c,string platform="ios") {return "METEOR_CONTENT_V1\n2020.1.0f1-2ab9c4179772\n"+platform+"\n"+Hash(a)+"\t"+a.Length+"\tresources.bundle\n"+Hash(b)+"\t"+b.Length+"\tscenes.bundle\n"+Hash(c)+"\t"+c.Length+"\tresources.index\n";}
 static void Upload(ContentInstall i,string name,byte[] data){using(var s=new MemoryStream(data))i.Receive(name,s,data.Length,()=>false);}
 static void Main(){
  var root=Path.Combine(Path.GetTempPath(),"meteor-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  try{
   var a=Encoding.ASCII.GetBytes("UnityFS\0resources-fixture");var b=Encoding.ASCII.GetBytes("UnityFS\0scenes-fixture");var c=Encoding.UTF8.GetBytes("{\"entries\":[]}");
   using(var low=new ContentInstall(root,"ios",_=>0))Reject(()=>Upload(low,"resources.bundle",a),"low disk rejects before writing");
   using(var unknown=new ContentInstall(root,"ios",_=>-1))Reject(()=>Upload(unknown,"resources.bundle",a),"unknown disk space fails closed");
   using(var boundary=new ContentInstall(root,"ios",_=>128L*1024*1024)){Upload(boundary,"resources.bundle",a);Check(boundary.ReceivedBytes==a.Length,"128MiB reserve boundary accepted");}
   using(var i=new ContentInstall(root,"ios")){
    Check(i.CurrentRoot==null,"fresh install is locked");
    Reject(()=>Upload(i,"../evil.dll",a),"path traversal");Reject(()=>Upload(i,"code.dll",a),"executable rejected");
    Upload(i,"resources.bundle",a);Reject(()=>i.Commit(),"missing manifest rejects");Check(i.CurrentRoot==null,"failed commit does not unlock");
    Upload(i,"scenes.bundle",b);Upload(i,"resources.index",c);Upload(i,"content.manifest",Encoding.UTF8.GetBytes(Manifest(a,b,c)));
    var p=i.Commit();Check(Directory.Exists(p),"install atomically activated");Check(i.CurrentRoot==p,"current points to valid install");
   }
   string previous=ContentInstall.ReadCurrent(root,"ios");Check(previous!=null,"persisted install validates");
   using(var i=new ContentInstall(root,"ios")){
    Upload(i,"resources.bundle",a);Upload(i,"scenes.bundle",b);Upload(i,"resources.index",c);Upload(i,"content.manifest",Encoding.UTF8.GetBytes(Manifest(a,b,c,"windows")));
    Reject(()=>i.Commit(),"wrong platform rejects");Check(ContentInstall.ReadCurrent(root,"ios")==previous,"bad replacement preserves previous");
   }
   using(var i=new ContentInstall(root,"ios")){
    using(var s=new MemoryStream(a))Reject(()=>i.Receive("resources.bundle",s,a.Length+1,()=>false),"truncated upload");
    Check(!File.Exists(Path.Combine(i.StageRoot,"resources.bundle")),"truncated data never committed");
   }
   using(var i=new ContentInstall(root,"ios")){
    var wrong=(byte[])a.Clone();wrong[wrong.Length-1]^=1;Upload(i,"resources.bundle",wrong);Upload(i,"scenes.bundle",b);Upload(i,"resources.index",c);Upload(i,"content.manifest",Encoding.UTF8.GetBytes(Manifest(a,b,c)));
    Reject(()=>i.Commit(),"checksum mismatch");
   }
   var tap=new ButtonHoldState();tap.Down(90,ControlAction.Attack,0);tap.Up(90,.005);Check(tap.IsHeld(ControlAction.Attack,.03),"quick attack survives until next input poll");Check(!tap.IsHeld(ControlAction.Attack,.06),"quick attack releases after observed frame");
   var hold=new ButtonHoldState();hold.Down(1,ControlAction.Defence,0);hold.Down(2,ControlAction.Defence,0);hold.Up(1,.01);Check(hold.IsHeld(ControlAction.Defence,.01),"one finger cannot release other defence");hold.Up(2,.02);Check(!hold.IsHeld(ControlAction.Defence,.02),"defence releases last source");
   hold.Down(3,ControlAction.LongJump,1);hold.Up(3,1.01);Check(hold.IsHeld(ControlAction.Jump,1.10),"short long-jump tap holds past threshold");Check(!hold.IsHeld(ControlAction.Jump,1.21),"long jump finite release");
   hold.Down(4,ControlAction.Jump,2);hold.Down(5,ControlAction.LongJump,2);hold.Up(5,2.01);Check(hold.IsHeld(ControlAction.Jump,2.3),"normal jump remains after longjump release");hold.Cancel();Check(!hold.IsHeld(ControlAction.Jump,2.31),"cancel releases all including delayed");
   Reject(()=>hold.Down(7,(ControlAction)100,0),"unknown action");
   File.WriteAllText(Path.Combine(previous,"resources.index"),"tampered");Check(ContentInstall.ReadCurrent(root,"ios")==null,"tampered install relocks launcher");
   var setting=new ControlBinding{action=ControlAction.Attack,x=float.NaN,y=2,size=-2,opacity=0};setting.Normalize();Check(setting.x==.85f&&setting.y<=.95f&&setting.size>=48&&setting.opacity>=.2f,"layout sanitized");
   Console.WriteLine("PASS "+checks+" launcher/input checks");
  }finally{Directory.Delete(root,true);}
 }
}
