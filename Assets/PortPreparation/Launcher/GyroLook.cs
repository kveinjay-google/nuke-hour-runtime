using UnityEngine;
namespace Meteor.Launcher {
 // Rate integration: no absolute-attitude offset or camera snap when enabling.
 public sealed class GyroLook:MonoBehaviour {
  bool focused=true,active;float warmup;public int Samples{get;private set;}public Vector2 LastDelta{get;private set;}
  void Update(){
   var owner=CustomCombatControls.Instance;var p=CustomCombatControls.Profile;
   bool wanted=focused&&p.gyroEnabled&&SystemInfo.supportsGyroscope;
   if(wanted!=active){active=wanted;Input.gyro.enabled=wanted;warmup=Time.realtimeSinceStartup+.15f;}
   LastDelta=Vector2.zero;
   if(!active||Time.realtimeSinceStartup<warmup||owner==null||CustomCombatControls.Editing||owner.Reordering||CombatData.Ins.PauseAll||Main.Ins==null||Main.Ins.LocalPlayer==null)return;
   var player=Main.Ins.LocalPlayer;if(player.Dead||player.meteorController==null||player.meteorController.InputLocked||Main.Ins.GameBattleEx==null||Main.Ins.GameBattleEx.BattleFinished())return;
   Vector3 rate=Input.gyro.rotationRateUnbiased;float pitch=rate.x,yaw=rate.y;
   if(Screen.orientation==ScreenOrientation.LandscapeLeft){pitch=-rate.y;yaw=rate.x;}
   else if(Screen.orientation==ScreenOrientation.LandscapeRight){pitch=rate.y;yaw=-rate.x;}
   float sensitivity=GyroMath.Sensitivity(p.gyroSensitivity);
   float dx=GyroMath.Delta(pitch,Time.unscaledDeltaTime,sensitivity),dy=GyroMath.Delta(yaw,Time.unscaledDeltaTime,sensitivity);
   if(player.ActionMgr!=null&&!player.ActionMgr.CanRotateY)dy=0;
   LastDelta=new Vector2(-dx,dy);Samples++;if(dx!=0||dy!=0)player.OnPlayerMouseDelta(-dx,dy);
  }
  void OnApplicationFocus(bool value){focused=value;if(!value)Stop();}
  void OnApplicationPause(bool value){focused=!value;if(value)Stop();}
  void OnDisable(){Stop();}
  void Stop(){if(active)Input.gyro.enabled=false;active=false;LastDelta=Vector2.zero;}
 }
}
