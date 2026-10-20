using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Meteor.PortPreparation {
 [DefaultExecutionOrder(-150)]
 public sealed class MeteorTouchOverlay:MonoBehaviour,IPortInputSink {
  public static MeteorTouchOverlay Instance {get;private set;}
  public TouchInputRouter Router {get;private set;}
  Canvas canvas;RectTransform safe;bool pauseRequested;float width,height;
  public bool TestMode {get;set;}
  public int LookSamples {get;private set;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void StartPort(){
#if UNITY_IOS && !UNITY_EDITOR
   // The original FightWnd/UGUIJoystick already owns iOS touch input.
   // Do not cover it or enable prototype polling, which releases native button holds.
   bool activate=false;
#else
   bool activate=Environment.GetEnvironmentVariable("METEOR_TOUCH_UI")=="1";
#endif
   if(activate&&Instance==null){var root=new GameObject("MeteorTouchOverlay");root.AddComponent<MeteorTouchOverlay>();DontDestroyOnLoad(root);}
  }
  void Awake(){Instance=this;Router=new TouchInputRouter(this);PortInputState.Enable(true);Build();}
  void Build(){
   canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=250;
   var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
   gameObject.AddComponent<GraphicRaycaster>();var box=new GameObject("SafeArea",typeof(RectTransform));safe=box.GetComponent<RectTransform>();safe.SetParent(transform,false);Layout();
   Add("Move",new Vector2(.14f,.23f),new Vector2(150,150),0,PortAction.Attack);
   var look=Add("Look",new Vector2(.60f,.57f),new Vector2(300,280),1,PortAction.Attack);look.GetComponent<Image>().color=new Color(1,1,1,.015f);
   Add("Attack",new Vector2(.87f,.23f),new Vector2(82,82),2,PortAction.Attack);
   Add("Jump",new Vector2(.78f,.13f),new Vector2(68,68),2,PortAction.Jump);
   Add("Defence",new Vector2(.78f,.34f),new Vector2(68,68),2,PortAction.Defence);
   Add("ChangeWeapon",new Vector2(.90f,.43f),new Vector2(68,68),2,PortAction.ChangeWeapon);
   Add("Pause",new Vector2(.94f,.91f),new Vector2(64,48),2,PortAction.Pause);
  }
  GameObject Add(string name,Vector2 anchor,Vector2 size,int kind,PortAction action){
   var go=new GameObject("MeteorTouch_"+name,typeof(RectTransform),typeof(Image),typeof(MeteorTouchWidget));var rect=go.GetComponent<RectTransform>();rect.SetParent(safe,false);rect.anchorMin=rect.anchorMax=anchor;rect.sizeDelta=size;
   go.GetComponent<Image>().color=new Color(.1f,.15f,.18f,.45f);var widget=go.GetComponent<MeteorTouchWidget>();widget.Owner=this;widget.Kind=kind;widget.Action=action;
   var label=new GameObject("Label",typeof(RectTransform),typeof(Text));var tr=label.GetComponent<RectTransform>();tr.SetParent(rect,false);tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
   var text=label.GetComponent<Text>();text.text=name=="ChangeWeapon"?"Weapon":name;text.font=Resources.GetBuiltinResource<Font>("Arial.ttf");text.fontSize=16;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;return go;
  }
  void Layout(){var area=Screen.safeArea;safe.anchorMin=new Vector2(area.x/Screen.width,area.y/Screen.height);safe.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;width=Screen.width;height=Screen.height;}
  void Update(){
   if(width!=Screen.width||height!=Screen.height)Layout();
   bool play=TestMode||(Main.Ins!=null&&Main.Ins.LocalPlayer!=null&&!Main.Ins.LocalPlayer.Dead&&!CombatData.Ins.PauseAll);
   canvas.enabled=play;if(safe.gameObject.activeSelf!=play)safe.gameObject.SetActive(play);if(!play){Router.CancelAll();PortInputState.SetMove(0,0);return;}
   Router.Tick();if(pauseRequested){pauseRequested=false;Router.CancelAll();if(!TestMode)U3D.OpenSystemWnd();}
  }
  public void ButtonDown(PortAction action){if(action==PortAction.Pause)pauseRequested=true;else PortInputState.SetButton(action,true);}
  public void ButtonUp(PortAction action){PortInputState.SetButton(action,false);}
  public void ButtonHeld(PortAction action){}
  public void Move(float x,float y){PortInputState.SetMove(x,y);}
  public void Look(float x,float y){if(x!=0||y!=0)LookSamples++;if(!TestMode&&Main.Ins!=null&&Main.Ins.LocalPlayer!=null&&!CombatData.Ins.PauseAll&&(x!=0||y!=0))Main.Ins.LocalPlayer.OnPlayerMouseDelta(-y*12,x*12);}
  void OnApplicationFocus(bool focus){if(!focus){Router.CancelAll();PortInputState.SetMove(0,0);}}
  void OnDisable(){if(Router!=null)Router.CancelAll();PortInputState.Enable(false);}
  void OnEnable(){if(Router!=null)PortInputState.Enable(true);}
  void OnDestroy(){if(Instance==this){Instance=null;PortInputState.Enable(false);}}
 }
 public sealed class MeteorTouchWidget:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IBeginDragHandler,IDragHandler,IEndDragHandler {
  public MeteorTouchOverlay Owner;public int Kind;public PortAction Action;readonly HashSet<int> ids=new HashSet<int>();
  public void OnPointerDown(PointerEventData data){if(Owner==null)return;var rect=GetComponent<RectTransform>();Vector2 center=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));float radius=rect.rect.width*Owner.GetComponent<Canvas>().scaleFactor*.5f;
   bool accepted=Kind==0?Owner.Router.BeginMove(data.pointerId,center.x,center.y,Mathf.Max(1,radius)):(Kind==1?Owner.Router.BeginLook(data.pointerId,data.position.x,data.position.y,100):Owner.Router.BeginButton(data.pointerId,Action));if(accepted){ids.Add(data.pointerId);Owner.Router.MovePointer(data.pointerId,data.position.x,data.position.y);}
  }
  public void OnPointerUp(PointerEventData data){if(Owner!=null&&ids.Remove(data.pointerId))Owner.Router.End(data.pointerId);}
  public void OnBeginDrag(PointerEventData data){}
  public void OnDrag(PointerEventData data){if(Owner!=null&&ids.Contains(data.pointerId))Owner.Router.MovePointer(data.pointerId,data.position.x,data.position.y);}
  public void OnEndDrag(PointerEventData data){OnPointerUp(data);}
  void OnDisable(){if(Owner!=null&&Owner.Router!=null)foreach(int id in ids)Owner.Router.End(id);ids.Clear();}
 }
}
