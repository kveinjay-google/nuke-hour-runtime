using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Meteor.Launcher {
 [DefaultExecutionOrder(-110)]
 public sealed class CustomCombatControls : MonoBehaviour {
  public static CustomCombatControls Instance{get;private set;}
  static ControlProfile profile;static bool settings;static int selected;
  static GameObject inputBlocker;static string capture="";static Vector2 scroll;static string message="";
  readonly ButtonHoldState holds=new ButtonHoldState();readonly Dictionary<string,RectTransform> widgets=new Dictionary<string,RectTransform>();
  readonly bool[] keyboardHeld=new bool[32];bool paused;
  public static bool Editing{get{return settings;}}
  public static string Label(ControlAction a){return new[]{"攻击","跳跃","大跳","防御","换武器","爆气","蹲下","切换锁定","暂停"}[(int)a];}
  static string ProfilePath{get{return Path.Combine(Application.persistentDataPath,Screen.width/(float)Math.Max(1,Screen.height)<1.6f?"controls-tablet-v1.json":"controls-phone-v1.json");}}
  public static ControlProfile Profile{get{if(profile==null)Load();return profile;}}
  static void Load(){profile=ControlProfile.Default();try{if(File.Exists(ProfilePath)){var p=JsonUtility.FromJson<ControlProfile>(File.ReadAllText(ProfilePath));var defaults=ControlProfile.Default();if(p!=null&&p.version==1&&p.bindings!=null&&p.bindings.Length==defaults.bindings.Length&&p.bindings.Select(x=>x.id).OrderBy(x=>x).SequenceEqual(defaults.bindings.Select(x=>x.id).OrderBy(x=>x))){foreach(var b in p.bindings){b.Normalize();KeyCode parsed;if(b.key!=""&&(!Enum.TryParse<KeyCode>(b.key,true,out parsed)||!Enum.IsDefined(typeof(KeyCode),parsed)))b.key="";if(b.pad!=""&&(!Enum.TryParse<KeyCode>(b.pad,true,out parsed)||!Enum.IsDefined(typeof(KeyCode),parsed)))b.pad="";}profile=p;}}}catch(Exception){message="按键配置无法读取，已恢复默认";}if(profile.gyroSensitivity<=0||Single.IsNaN(profile.gyroSensitivity)||Single.IsInfinity(profile.gyroSensitivity))profile.gyroSensitivity=1;if(profile.layoutRevision<1)profile.Satellite(Screen.width/(float)Math.Max(1,Screen.height));}
  public static void Bind(GameObject fight){var c=fight.GetComponent<CustomCombatControls>();if(c==null)c=fight.AddComponent<CustomCombatControls>();c.Setup();}
  void Setup(){Instance=this;if(GetComponent<GyroLook>()==null)gameObject.AddComponent<GyroLook>();if(widgets.Count>0){ApplyLayout();return;}var area=new GameObject("CustomActionArea",typeof(RectTransform));var ar=area.GetComponent<RectTransform>();ar.SetParent(transform,false);ar.anchorMin=Vector2.zero;ar.anchorMax=Vector2.one;ar.offsetMin=ar.offsetMax=Vector2.zero;
   foreach(var b in Profile.bindings){GameObject node=NodeHelper.Find(b.id,gameObject);
    if(b.id=="LongJump"){var jump=NodeHelper.Find("Jump",gameObject);if(jump==null)throw new InvalidOperationException("Native Jump control missing");node=Instantiate(jump);node.name="Configured_LongJump";}
    if(node==null){node=new GameObject("Configured_"+b.id,typeof(RectTransform),typeof(Image));}
    node.SetActive(true);var rect=node.GetComponent<RectTransform>();if(rect==null)continue;rect.SetParent(ar,false);foreach(var old in node.GetComponentsInChildren<GameButton>(true))old.enabled=false;foreach(var old in node.GetComponentsInChildren<Button>(true))old.enabled=false;
    var receiver=node.GetComponent<ConfiguredActionButton>();if(receiver==null)receiver=node.AddComponent<ConfiguredActionButton>();receiver.Owner=this;receiver.Binding=b;
    foreach(var g in node.GetComponentsInChildren<Graphic>(true))g.raycastTarget=false;
    var image=node.GetComponent<Image>();if(image==null)image=node.AddComponent<Image>();image.raycastTarget=true;
    // Empty Image wrappers previously rendered white squares behind the real glyphs.
    foreach(var icon in node.GetComponentsInChildren<Image>(true))if(icon.sprite==null)icon.color=Color.clear;
    foreach(var text in node.GetComponentsInChildren<Text>(true))text.enabled=false;
    if(b.id=="LongJump"){var cue=new GameObject("LongJumpCue",typeof(RectTransform),typeof(CombatControlSymbol));var cr=cue.GetComponent<RectTransform>();cr.SetParent(rect,false);cr.anchorMin=Vector2.zero;cr.anchorMax=Vector2.one;cr.offsetMin=cr.offsetMax=Vector2.zero;cue.GetComponent<CombatControlSymbol>().raycastTarget=false;}

    widgets[b.id]=rect;
   }CreateSettingsGear(ar);ApplyLayout();
  }
  public void ApplyLayout(){foreach(var b in Profile.bindings){RectTransform r;if(!widgets.TryGetValue(b.id,out r))continue;b.Normalize();r.GetComponent<ConfiguredActionButton>().Binding=b;r.anchorMin=r.anchorMax=new Vector2(b.x,b.y);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(b.size,b.size);r.localScale=Vector3.one;r.gameObject.SetActive(b.visible||settings);var group=r.GetComponent<CanvasGroup>();if(group==null)group=r.gameObject.AddComponent<CanvasGroup>();group.alpha=b.opacity;var label=r.Find("ActionName");if(label!=null)label.GetComponent<Text>().text=Label(b.action);}}
  static bool ActionFor(string axis,out ControlAction a){switch(axis){case "Attack":a=ControlAction.Attack;return true;case "Jump":a=ControlAction.Jump;return true;case "Defence":a=ControlAction.Defence;return true;case "ChangeWeapon":a=ControlAction.ChangeWeapon;return true;case "BreakOut":a=ControlAction.BreakOut;return true;case "Crouch":a=ControlAction.Crouch;return true;default:a=ControlAction.Attack;return false;}}
  public static bool Handles(string axis){ControlAction a;return Instance!=null&&ActionFor(axis,out a);}
  public static bool Combine(string axis,bool physical,bool gamepad=false){ControlAction a;var c=Instance;if(c==null||!ActionFor(axis,out a))return physical;if(settings||c.paused||c.Reordering)return false;
   bool configured=false,down=false;foreach(var b in Profile.bindings){if((b.action==ControlAction.LongJump?ControlAction.Jump:b.action)!=a)continue;KeyCode k;if(!String.IsNullOrEmpty(b.key)&&Enum.TryParse<KeyCode>(b.key,true,out k)){configured|=!gamepad;down|=Input.GetKey(k);}if(!String.IsNullOrEmpty(b.pad)&&Enum.TryParse<KeyCode>(b.pad,true,out k)){configured|=gamepad;down|=Input.GetKey(k);}}
   return (!configured&&physical)||down||c.holds.IsHeld(a,Time.unscaledTime);
  }
  public int NewPointerSource(){return holds.NewSource();}
  public void Down(int id,ControlAction a){if(settings||paused||Reordering)return;if(a!=ControlAction.Pause&&a!=ControlAction.Unlock)holds.Down(id,a,Time.unscaledTime);if(a==ControlAction.Pause){holds.Cancel();U3D.OpenSystemWnd();}if(a==ControlAction.Unlock){holds.Up(id,Time.unscaledTime);if(FightState.Exist())FightState.Instance.OnClickChangeLock();}}
  public void Up(int id){holds.Up(id,Time.unscaledTime);}
  void Update(){bool p=CombatData.Ins.PauseAll||Main.Ins==null||Main.Ins.LocalPlayer==null||Main.Ins.LocalPlayer.Dead;paused=p;if(p||settings||Reordering){holds.Cancel();Array.Clear(keyboardHeld,0,keyboardHeld.Length);return;}
   for(int i=0;i<Profile.bindings.Length;i++){var b=Profile.bindings[i];KeyCode k;bool down=(!String.IsNullOrEmpty(b.key)&&Enum.TryParse<KeyCode>(b.key,true,out k)&&Input.GetKey(k))||(!String.IsNullOrEmpty(b.pad)&&Enum.TryParse<KeyCode>(b.pad,true,out k)&&Input.GetKey(k));if(down&&!keyboardHeld[i])Down(Int32.MinValue+i,b.action);if(!down&&keyboardHeld[i])Up(Int32.MinValue+i);keyboardHeld[i]=down;}
  }
  void Cancel(){holds.Cancel();Array.Clear(keyboardHeld,0,keyboardHeld.Length);}
  void OnApplicationFocus(bool focus){if(!focus)Cancel();}void OnApplicationPause(bool p){if(p)Cancel();}void OnDisable(){Cancel();}void OnDestroy(){if(Instance==this){Instance=null;settings=false;if(inputBlocker!=null){Destroy(inputBlocker);inputBlocker=null;}}}
  public static void OpenSettings(){settings=true;if(Instance!=null&&inputBlocker==null){inputBlocker=new GameObject("ControlSettingsInputBlocker",typeof(Canvas),typeof(GraphicRaycaster));var cv=inputBlocker.GetComponent<Canvas>();cv.renderMode=RenderMode.ScreenSpaceOverlay;cv.sortingOrder=32760;var shield=new GameObject("Shield",typeof(RectTransform),typeof(Image));var rt=shield.GetComponent<RectTransform>();rt.SetParent(inputBlocker.transform,false);rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;shield.GetComponent<Image>().color=Color.clear;}
if(Instance!=null){Instance.Cancel();Instance.ApplyLayout();}}
  public static void DrawSettings(){if(!settings)return;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.blackTexture);LauncherStyle.Begin();GUILayout.BeginArea(new Rect(50,35,1180,650),GUI.skin.box);GUILayout.Label("操作设置",LauncherStyle.Title);GUILayout.Label("选择按钮可修改功能、大小和位置；拖动右侧预览标记调整位置。防御需按住，大跳会保持跳跃输入。",LauncherStyle.Body);
   GUILayout.BeginHorizontal();for(int i=0;i<Profile.bindings.Length;i++)if(GUILayout.Toggle(selected==i,Label(Profile.bindings[i].action),GUI.skin.button))selected=i;GUILayout.EndHorizontal();
   Profile.gyroEnabled=GUILayout.Toggle(Profile.gyroEnabled,"陀螺仪调整视角"+(SystemInfo.supportsGyroscope?"":"（此设备不支持）"));
   if(Single.IsNaN(Profile.gyroSensitivity)||Single.IsInfinity(Profile.gyroSensitivity))Profile.gyroSensitivity=1;
   GUILayout.BeginHorizontal();GUILayout.Label("陀螺仪灵敏度："+Profile.gyroSensitivity.ToString("0.0")+" 倍",GUILayout.Width(310));Profile.gyroSensitivity=GUILayout.HorizontalSlider(Profile.gyroSensitivity,.1f,3f,GUILayout.Width(330));GUILayout.EndHorizontal();
   var b=Profile.bindings[Math.Min(selected,Profile.bindings.Length-1)];scroll=GUILayout.BeginScrollView(scroll,GUILayout.Height(315),GUILayout.Width(720));GUILayout.Label("按钮功能");int action=GUILayout.SelectionGrid((int)b.action,Enum.GetValues(typeof(ControlAction)).Cast<ControlAction>().Select(Label).ToArray(),5);b.action=(ControlAction)action;
   b.visible=GUILayout.Toggle(b.visible,"显示这个按钮");GUILayout.Label("水平位置");b.x=GUILayout.HorizontalSlider(b.x,.05f,.95f);GUILayout.Label("垂直位置");b.y=GUILayout.HorizontalSlider(b.y,.05f,.95f);GUILayout.Label("按钮大小");b.size=GUILayout.HorizontalSlider(b.size,48,160);GUILayout.Label("不透明度");b.opacity=GUILayout.HorizontalSlider(b.opacity,.2f,1);
   GUILayout.BeginHorizontal();if(GUILayout.Button("键盘："+(b.key==""?"默认":b.key)))capture="key";if(GUILayout.Button("手柄："+(b.pad==""?"默认":b.pad)))capture="pad";if(GUILayout.Button("清除绑定")){b.key=b.pad="";capture="";}GUILayout.EndHorizontal();
   if(capture!=""){GUILayout.Label("请按下要使用的"+(capture=="key"?"键盘按键":"手柄按钮")+"，或取消。连招仍使用正常方向与攻击输入。");var e=Event.current;if(capture=="key"&&e.type==EventType.KeyDown&&e.keyCode!=KeyCode.None){b.key=e.keyCode.ToString();capture="";e.Use();}if(capture=="pad")for(int k=0;k<20;k++){var code=(KeyCode)((int)KeyCode.JoystickButton0+k);if(Input.GetKeyDown(code)){b.pad=code.ToString();capture="";break;}}if(GUILayout.Button("取消绑定"))capture="";}
   GUILayout.EndScrollView();
   GUILayout.Label(message);GUILayout.BeginHorizontal();if(GUILayout.Button("恢复默认",GUILayout.Height(46))){profile=ControlProfile.Default();profile.Satellite(Screen.width/(float)Math.Max(1,Screen.height));selected=0;message="已恢复默认，点击保存应用";}
   if(GUILayout.Button("保存",GUILayout.Height(46))){try{foreach(var item in Profile.bindings){item.Normalize();KeyCode k;if((item.key!=""&&(!Enum.TryParse<KeyCode>(item.key,true,out k)||!Enum.IsDefined(typeof(KeyCode),k)))||(item.pad!=""&&(!Enum.TryParse<KeyCode>(item.pad,true,out k)||!Enum.IsDefined(typeof(KeyCode),k))))throw new InvalidDataException("键名无效，请使用 Space、LeftShift 或 JoystickButton0 等Unity键名");}foreach(bool pad in new[]{false,true}){var keys=Profile.bindings.Select(x=>pad?x.pad:x.key).Where(x=>x!="").ToArray();if(keys.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=keys.Length)throw new InvalidDataException("按键绑定冲突，请给不同动作选择不同按键");}Directory.CreateDirectory(Application.persistentDataPath);File.WriteAllText(ProfilePath,JsonUtility.ToJson(Profile,true));message="设置已保存";}catch(Exception e){message=e.Message;}}
   if(GUILayout.Button("返回",GUILayout.Height(46))){settings=false;capture="";if(inputBlocker!=null){Destroy(inputBlocker);inputBlocker=null;}if(Instance!=null){Instance.Cancel();Instance.ApplyLayout();}}GUILayout.EndHorizontal();GUILayout.EndArea();
   var preview=new Rect(835,215,350,230);GUI.Box(preview,"位置预览");var marker=new Rect(preview.x+b.x*preview.width-24,preview.y+(1-b.y)*preview.height-24,48,48);GUI.Box(marker,Label(b.action));
   int dragId=GUIUtility.GetControlID("MeteorLayoutPreview".GetHashCode(),FocusType.Passive);var ev=Event.current;if(ev.type==EventType.MouseDown&&marker.Contains(ev.mousePosition)){GUIUtility.hotControl=dragId;ev.Use();}if(ev.type==EventType.MouseDrag&&GUIUtility.hotControl==dragId){b.x=(ev.mousePosition.x-preview.x)/preview.width;b.y=1-(ev.mousePosition.y-preview.y)/preview.height;b.Normalize();ev.Use();}if(ev.type==EventType.MouseUp&&GUIUtility.hotControl==dragId){GUIUtility.hotControl=0;ev.Use();}
   if(Instance!=null)Instance.ApplyLayout();LauncherStyle.End();
  }
  void CreateSettingsGear(RectTransform area){
   var gear=new GameObject("CombatSettingsGear",typeof(RectTransform),typeof(CombatControlSymbol),typeof(CombatSettingsClick));var rt=gear.GetComponent<RectTransform>();rt.SetParent(area,false);rt.anchorMin=rt.anchorMax=Vector2.one;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=new Vector2(-78,-68);rt.sizeDelta=new Vector2(42,42);gear.GetComponent<CombatControlSymbol>().Gear=true;
  }
  void OnGUI(){DrawSettings();}

  public bool Reordering{get;private set;}
  public void BeginReorder(){Reordering=true;Cancel();}
  public void MoveWidget(ControlBinding b,Vector2 screen){RectTransform r;if(!widgets.TryGetValue(b.id,out r))return;Vector2 local;var canvas=r.GetComponentInParent<Canvas>();var cam=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)r.parent,screen,cam,out local))r.localPosition=new Vector3(local.x,local.y,0);}
  public void EndReorder(ControlBinding b,Vector2 screen){
   string nearest=null;float distance=Single.MaxValue;
   foreach(var item in Profile.bindings){if(item.id==b.id||item.id=="Attack"||item.id=="Pause"||!item.visible)continue;RectTransform r;if(!widgets.TryGetValue(item.id,out r))continue;var canvas=r.GetComponentInParent<Canvas>();var cam=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;float d=Vector2.Distance(RectTransformUtility.WorldToScreenPoint(cam,r.position),screen);if(d<distance){distance=d;nearest=item.id;}}
   if(nearest!=null&&distance<Screen.height*.12f&&Profile.SwapSlots(b.id,nearest))try{File.WriteAllText(ProfilePath,JsonUtility.ToJson(Profile,true));}catch(Exception e){Debug.LogError("Control layout save failed: "+e.Message);}
   Reordering=false;ApplyLayout();
  }
  public void Drag(ControlBinding b,Vector2 pos){if(!settings)return;b.x=pos.x/Math.Max(1,Screen.width);b.y=pos.y/Math.Max(1,Screen.height);b.Normalize();ApplyLayout();}
 }
 public sealed class ConfiguredActionButton:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IDragHandler {
  int layoutPointer=Int32.MinValue;float pressedAt;Vector2 origin;bool dragging;
  public CustomCombatControls Owner;public ControlBinding Binding;readonly Dictionary<int,int> ids=new Dictionary<int,int>();
  public void OnPointerDown(PointerEventData e){if(layoutPointer==Int32.MinValue){layoutPointer=e.pointerId;pressedAt=Time.unscaledTime;origin=e.position;}if(CustomCombatControls.Editing){Owner.Drag(Binding,e.position);return;}if(!ids.ContainsKey(e.pointerId)){int source=Owner.NewPointerSource();ids.Add(e.pointerId,source);Owner.Down(source,Binding.action);}}
  public void OnPointerUp(PointerEventData e){if(layoutPointer==e.pointerId){if(dragging&&Owner!=null)Owner.EndReorder(Binding,e.position);dragging=false;layoutPointer=Int32.MinValue;}int source;if(ids.TryGetValue(e.pointerId,out source)){ids.Remove(e.pointerId);if(Owner!=null)Owner.Up(source);}}
  public void OnDrag(PointerEventData e){if(CustomCombatControls.Editing){Owner.Drag(Binding,e.position);return;}if(e.pointerId!=layoutPointer||Binding.id=="Attack"||Binding.id=="Pause")return;if(!dragging&&Time.unscaledTime-pressedAt>=.7f&&Vector2.Distance(origin,e.position)>12){dragging=true;Owner.BeginReorder();}if(dragging)Owner.MoveWidget(Binding,e.position);}
  void OnDisable(){if(dragging&&Owner!=null)Owner.EndReorder(Binding,origin);dragging=false;layoutPointer=Int32.MinValue;if(Owner!=null)foreach(int source in ids.Values)Owner.Up(source);ids.Clear();}
 }
 public static class LauncherStyle {
  public static Font FixedFont;static Font font;static Matrix4x4 matrix;public static Font Font{get{if(FixedFont!=null)return FixedFont;if(font==null)font=UnityEngine.Font.CreateDynamicFontFromOSFont(new[]{"PingFang SC","Heiti SC","Arial Unicode MS","Arial"},22);return font;}}
  public static GUIStyle Title{get{return new GUIStyle(GUI.skin.label){font=Font,fontSize=32,wordWrap=true};}}
  public static GUIStyle Body{get{return new GUIStyle(GUI.skin.label){font=Font,fontSize=20,wordWrap=true};}}
  public static void Begin(){matrix=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));GUI.skin.font=Font;GUI.skin.label.fontSize=19;GUI.skin.button.fontSize=20;GUI.skin.toggle.fontSize=19;GUI.skin.textField.fontSize=20;}
  public static void End(){GUI.matrix=matrix;}
 }
}
