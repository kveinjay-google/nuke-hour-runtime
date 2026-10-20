using System;using System.Security.Cryptography;using System.IO;using System.Net;using System.Text;using Meteor.Launcher;
class LanTests{
 static int n;static void Check(bool c,string s){n++;if(!c)throw new Exception(s);}
 static int Request(LanContentServer server,string method,string path,string code,byte[] body=null,string origin=null){
  var u=new Uri(server.Address);using(var c=new System.Net.Sockets.TcpClient()){c.ReceiveTimeout=4000;c.SendTimeout=4000;c.Connect(u.Host,u.Port);using(var st=c.GetStream()){
   string header=method+" "+path+" HTTP/1.1\r\nHost: "+u.Authority+"\r\nConnection: close\r\nContent-Length: "+(body==null?0:body.Length)+"\r\n"+(origin==null?"":"Origin: "+origin+"\r\n")+"\r\n";
   byte[] h=Encoding.ASCII.GetBytes(header),packet=new byte[h.Length+(body==null?0:body.Length)];Buffer.BlockCopy(h,0,packet,0,h.Length);if(body!=null)Buffer.BlockCopy(body,0,packet,h.Length,body.Length);st.Write(packet,0,packet.Length);
   using(var reader=new StreamReader(st)){string line=reader.ReadLine();if(line==null||!line.StartsWith("HTTP/1.1 "))throw new Exception("No HTTP response");int status=Int32.Parse(line.Split(' ')[1]);Console.WriteLine(line);return status;}
  }}
 }
 static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
 static void Main(){string root=Path.Combine(Path.GetTempPath(),"meteor-http-"+Guid.NewGuid().ToString("N"));try{using(var s=new LanContentServer(root,"ios",IPAddress.Loopback)){s.Start();Check(Request(s,"GET","/",null)==200,"upload page served");Check(Request(s,"GET","/api/status",null)==200,"status works without connection code");Check(Request(s,"GET","/api/status",null)==200,"paired status");Check(Request(s,"GET","/api/status",null,null,"http://evil.invalid")==403,"cross-site origin denied");Check(Request(s,"PUT","/api/upload?name=evil.dll",null,Encoding.ASCII.GetBytes("x"))==400,"executables rejected");Check(Request(s,"PUT","/api/upload?name=resources.bundle",null,Encoding.ASCII.GetBytes("UnityFS\0"))==200,"streamed upload");Check(Request(s,"POST","/api/commit",null,new byte[0])==400,"incomplete content cannot activate");Check(ContentInstall.ReadCurrent(root,"ios")==null,"incomplete import remains locked");
 byte[] a=Encoding.ASCII.GetBytes("UnityFS\0"),b=Encoding.ASCII.GetBytes("UnityFS\0test"),c=Encoding.ASCII.GetBytes("{}");
 Check(Request(s,"PUT","/api/upload?name=scenes.bundle",null,b)==200,"scene data upload");Check(Request(s,"PUT","/api/upload?name=resources.index",null,c)==200,"index upload");
 string manifest="METEOR_CONTENT_V1\n"+ContentInstall.RuntimeVersion+"\nios\n"+Hash(a)+"\t"+a.Length+"\tresources.bundle\n"+Hash(b)+"\t"+b.Length+"\tscenes.bundle\n"+Hash(c)+"\t"+c.Length+"\tresources.index\n";
 Check(Request(s,"PUT","/api/upload?name=content.manifest",null,Encoding.UTF8.GetBytes(manifest))==200,"manifest upload");Check(Request(s,"POST","/api/commit",null,new byte[0])==200,"complete fixture transaction");Check(ContentInstall.ReadCurrent(root,"ios")!=null,"HTTP activated structurally verified fixture");}Console.WriteLine("PASS "+n+" real HTTP checks");}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
}
