using System;
namespace Meteor.Launcher {
 public enum ControlAction { Attack,Jump,LongJump,Defence,ChangeWeapon,BreakOut,Crouch,Unlock,Pause }
 [Serializable] public sealed class ControlBinding {
  public string id;public ControlAction action;public float x=.85f,y=.2f,size=84,opacity=.8f;public bool visible=true;public string key="",pad="";
  static float Range(float v,float lo,float hi,float fallback){return Single.IsNaN(v)||Single.IsInfinity(v)?fallback:Math.Max(lo,Math.Min(hi,v));}
  public void Normalize(){if(!Enum.IsDefined(typeof(ControlAction),action))action=ControlAction.Attack;x=Range(x,.08f,.92f,.85f);y=Range(y,.12f,.88f,.2f);size=Range(size,48,160,84);opacity=Range(opacity,.2f,1,.8f);if(key==null)key="";if(pad==null)pad="";}
 }
 [Serializable] public sealed class ControlProfile {
  public int version=1;public int layoutRevision;public bool gyroEnabled;public float gyroSensitivity=1;public ControlBinding[] bindings;
  public void Satellite(float aspect){
   aspect=Math.Max(1,aspect);int slot=0;
   foreach(var b in bindings){if(b.id=="Pause")continue;if(b.id=="Attack"){b.x=.82f;b.y=.28f;b.size=106;continue;}
    double angle=(15+slot++*360.0/7)*Math.PI/180;b.x=.82f+(float)Math.Cos(angle)*.135f/aspect;b.y=.28f+(float)Math.Sin(angle)*.135f;b.size=68;
   }layoutRevision=1;
  }
  public bool SwapSlots(string first,string second){
   ControlBinding a=null,b=null;foreach(var item in bindings){if(item.id==first)a=item;if(item.id==second)b=item;}
   if(a==null||b==null||a==b||a.id=="Attack"||b.id=="Attack"||a.id=="Pause"||b.id=="Pause")return false;
   float x=a.x,y=a.y;a.x=b.x;a.y=b.y;b.x=x;b.y=y;return true;
  }
  public static ControlProfile Default(){return new ControlProfile{bindings=new[]{
   new ControlBinding{id="Attack",action=ControlAction.Attack,x=.91f,y=.16f,size=108},
   new ControlBinding{id="Jump",action=ControlAction.Jump,x=.82f,y=.12f},
   new ControlBinding{id="LongJump",action=ControlAction.LongJump,x=.73f,y=.12f},
   new ControlBinding{id="Defence",action=ControlAction.Defence,x=.91f,y=.32f},
   new ControlBinding{id="ChangeWeapon",action=ControlAction.ChangeWeapon,x=.91f,y=.46f,size=64},
   new ControlBinding{id="BreakOut",action=ControlAction.BreakOut,x=.73f,y=.26f,size=64},
   new ControlBinding{id="Crouch",action=ControlAction.Crouch,x=.82f,y=.30f,size=64},
   new ControlBinding{id="Unlock",action=ControlAction.Unlock,x=.82f,y=.46f,size=64},
   new ControlBinding{id="Pause",action=ControlAction.Pause,x=.92f,y=.87f,size=56}
  }};}
 }
}
