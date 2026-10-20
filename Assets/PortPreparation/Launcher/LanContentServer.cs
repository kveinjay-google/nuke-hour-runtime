using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace Meteor.Launcher {
 // One bounded receiver thread and one client; intentionally no HttpListener on iOS.
 public sealed class LanContentServer : IDisposable {
  readonly Func<string,string> runtimeValidation;readonly ContentInstall install;readonly TcpListener listener;readonly object sync=new object();
  readonly string platformName;Thread worker;TcpClient client;volatile bool stopping;
  public string Address{get;private set;}
  public volatile string Status="等待电脑连接…";public volatile string InstalledRoot;
  public long ReceivedBytes{get{return install.ReceivedBytes;}}
  public LanContentServer(string root,string platform,IPAddress address,Func<string,string> validateRuntime=null){
   if(address.Equals(IPAddress.Any)||address.Equals(IPAddress.IPv6Any))throw new ArgumentException("Choose a local interface");
   platformName=platform;runtimeValidation=validateRuntime;install=new ContentInstall(root,platform);listener=new TcpListener(address,0);
  }
  public void Start(){if(worker!=null)throw new InvalidOperationException();listener.Start(2);Address="http://"+listener.LocalEndpoint.ToString()+"/";worker=new Thread(Loop){IsBackground=true,Name="Meteor resource receiver"};worker.Start();}
  void Loop(){try{while(!stopping){TcpClient next;try{next=listener.AcceptTcpClient();}catch(SocketException){if(stopping)break;throw;}lock(sync)client=next;
    using(next){next.ReceiveTimeout=15000;next.SendTimeout=15000;try{Handle(next.GetStream());}catch(IOException){Status="连接断开了，请重新打开“从电脑传入”后再试";}catch(SocketException){Status="网络连接断开了，请检查 Wi-Fi 后重试";}finally{lock(sync)client=null;}}
   }}catch(Exception e){if(!stopping){Status="传输已停止："+e.GetType().Name;}}finally{install.Dispose();}}
  void Handle(NetworkStream stream){
   var bytes=new List<byte>();int tail=0;
   while(bytes.Count<16384){int b=stream.ReadByte();if(b<0)return;bytes.Add((byte)b);tail=((tail<<8)|b);if(tail==0x0d0a0d0a)break;if(stopping)return;}
   if(tail!=0x0d0a0d0a){Reply(stream,431,"无法处理这次连接，请刷新网页后重试");return;}
   string[] lines=Encoding.ASCII.GetString(bytes.ToArray()).Split(new[]{"\r\n"},StringSplitOptions.None),first=lines[0].Split(' ');
   if(first.Length!=3||first[2]!="HTTP/1.1"){Reply(stream,400,"连接出现问题，请刷新网页后重试");return;}
   var headers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
   for(int i=1;i<lines.Length;i++){if(lines[i].Length==0)continue;int colon=lines[i].IndexOf(':');if(colon<=0||headers.ContainsKey(lines[i].Substring(0,colon))){Reply(stream,400,"连接出现问题，请刷新网页后重试");return;}headers.Add(lines[i].Substring(0,colon),lines[i].Substring(colon+1).Trim());}
   string host,origin;if(!headers.TryGetValue("Host",out host)||host!=new Uri(Address).Authority||(headers.TryGetValue("Origin",out origin)&&origin!=Address.TrimEnd('/'))){Reply(stream,403,"网址已失效，请使用设备当前显示的网址");return;}
   if(headers.ContainsKey("Transfer-Encoding")){Reply(stream,400,"无法读取文件大小，请重新选择文件");return;}
   if(first[0]=="GET"&&first[1]=="/"){Reply(stream,200,Page,"text/html; charset=utf-8");return;}
   if(first[0]=="GET"&&first[1]=="/api/status"){Reply(stream,200,"{\"bytes\":"+ReceivedBytes+",\"ready\":"+(InstalledRoot!=null?"true":"false")+"}","application/json");return;}
   try {
    if(first[0]=="PUT"&&first[1].StartsWith("/api/upload?name=",StringComparison.Ordinal)){
     string lengthValue;long length;if(!headers.TryGetValue("Content-Length",out lengthValue)||!Int64.TryParse(lengthValue,out length))throw new InvalidDataException("无法读取文件大小，请重新选择文件");
     string name=Uri.UnescapeDataString(first[1].Substring("/api/upload?name=".Length));Status="正在传输："+ (ContentInstall.Allowed(name)?name:"文件不受支持");
     var deadline=DateTime.UtcNow.AddMinutes(5);install.Receive(name,stream,length,()=>stopping||DateTime.UtcNow>deadline);Status="已收到："+name;Reply(stream,200,"文件已传完");
    } else if(first[0]=="POST"&&first[1]=="/api/commit"){
     Status="文件已传完，正在检查，请保持本页打开…";ContentInstall.Validate(install.StageRoot,platformName);if(runtimeValidation!=null){string problem=runtimeValidation(install.StageRoot);if(problem!=null)throw new InvalidDataException(problem);}InstalledRoot=install.Commit(()=>stopping);Status="导入完成，可以开始游戏了";Reply(stream,200,"导入完成！请回到设备，点“开始游戏”。");
    } else Reply(stream,404,"页面不存在，请打开设备当前显示的网址");
   }catch(InvalidDataException e){Status=e.Message;Reply(stream,400,e.Message);}catch(OperationCanceledException){Reply(stream,408,"传输已取消或超时，请重新开始");}
    catch(UnauthorizedAccessException){Status="无法保存文件，请检查存储空间和访问权限";Reply(stream,500,Status);}
  }
  static void Reply(Stream s,int status,string body,string type="text/plain; charset=utf-8"){
   byte[] data=Encoding.UTF8.GetBytes(body),header=Encoding.ASCII.GetBytes("HTTP/1.1 "+status+" Response\r\nContent-Type: "+type+"\r\nContent-Length: "+data.Length+"\r\nConnection: close\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nContent-Security-Policy: default-src 'self'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; frame-ancestors 'none'\r\n\r\n");s.Write(header,0,header.Length);s.Write(data,0,data.Length);
  }
  public void Dispose(){if(stopping)return;stopping=true;listener.Stop();lock(sync)if(client!=null)client.Close();if(worker!=null){worker.Join(2000);}else install.Dispose();}
  public const string Page=@"<!doctype html><html lang='zh-CN'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>NUKE HOUR · 传入游戏文件</title><style>*{box-sizing:border-box}body{margin:0;background:#180e16;color:#f0dfbd;font:17px system-ui}main{max-width:820px;margin:8vh auto;padding:32px}h1{font-size:34px}p{line-height:1.7;color:#b8c1c6}section{border:1px solid #70523a;padding:24px;margin:20px 0}button,input{font:inherit;padding:12px;margin:8px 0}button{background:#9f322a;color:#fff0ce;border:0;cursor:pointer}input{max-width:100%}progress{width:100%;height:18px}#status{white-space:pre-wrap}small{color:#b8c1c6}</style><main><small>NUKE HOUR</small><h1>把游戏文件传到设备</h1><p>让电脑和设备连接同一个 Wi-Fi，然后按下面的步骤传入文件。传输时请保持设备上的导入页面打开，不要锁屏。</p><section><label>选择准备好的游戏文件夹<br><input id='folder' type='file' webkitdirectory multiple></label><br><label>也可以一次选中下面这四个文件<br><input id='files' type='file' multiple></label><p>文件夹中应包含：content.manifest、resources.index、resources.bundle 和 scenes.bundle。请使用你自己拥有的游戏文件，并先用配套工具整理；原版电脑游戏的安装文件夹目前不能直接使用。</p><button id='send'>开始传输</button><br><progress id='progress' value='0' max='1'></progress><p id='status'>选好文件后，点“开始传输”。</p></section><p>想取消时，在设备上点“结束传输”。如果连接中断，请重新打开“从电脑传入”，再用新网址重试。导入失败不会替换原有游戏文件，也不会影响存档。</p><p><a href='https://nukehour.com' target='_blank' rel='noopener noreferrer' style='color:#dbb77c'>NUKE HOUR 官网 ↗</a></p></main><script>
const allowed=['content.manifest','resources.index','resources.bundle','scenes.bundle'];let running=false;
const el=id=>document.getElementById(id);el('send').onclick=async()=>{if(running)return;let selected=[...el('folder').files,...el('files').files].filter(f=>allowed.includes(f.name));if(selected.length!==4||new Set(selected.map(f=>f.name)).size!==4){el('status').textContent='请选齐上面列出的四个文件，不要重复选择。';return;}const code=el('code').value.trim();if(!/^\d{6}$/.test(code)){el('status').textContent='请输入设备显示的六位数字。';return;}running=true;el('send').disabled=true;let total=selected.reduce((n,f)=>n+f.size,0),done=0;try{for(const f of selected){await new Promise((resolve,reject)=>{let x=new XMLHttpRequest();x.open('PUT','/api/upload?name='+encodeURIComponent(f.name));x.timeout=300000;x.upload.onprogress=e=>{el('progress').value=(done+e.loaded)/total;el('status').textContent='正在传输：'+f.name+' · '+Math.round((done+e.loaded)/total*100)+'%';};x.onload=()=>x.status===200?resolve():reject(new Error(x.responseText));x.onerror=()=>reject(new Error('连接断开了，请检查电脑和设备是否仍在同一个 Wi-Fi 下'));x.ontimeout=()=>reject(new Error('传输超时了，请检查网络后重试'));x.send(f);});done+=f.size;}el('status').textContent='文件已传完，正在检查，请稍候…';let response=await fetch('/api/commit',{method:'POST',headers:{}});let message=await response.text();if(!response.ok)throw new Error(message);el('status').textContent=message;el('progress').value=1;}catch(e){el('status').textContent=e.message+'\n请在设备上重新打开“从电脑传入”，再用新网址重试。';}finally{running=false;el('send').disabled=false;}};
</script></html>";
 }
}
