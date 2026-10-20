using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Meteor.Launcher {
 // Opt-in device QA: only UI pointer events. Never writes position, HP or damage.
 public sealed class CombatVisualProbe:MonoBehaviour {
  float next,releaseAt,started;int menuStep,actions,captures;GameObject pressed;bool inspected,layoutTesting;
  void Awake(){started=Time.realtimeSinceStartup;next=started+1;}
  void Update(){
   float now=Time.realtimeSinceStartup;
   if(pressed!=null&&now>=releaseAt){ExecuteEvents.Execute(pressed,Pointer(pressed),ExecuteEvents.pointerUpHandler);pressed=null;}
   if(now-started>55){Destroy(this);return;}
   if(now<next||EventSystem.current==null)return;next=now+1;
   var controls=CustomCombatControls.Instance;
   if(controls==null){
    string name=menuStep==0?"SinglePlayerMode":menuStep==1?"SinglePlayer":"Yes";
    var buttons=FindObjectsOfType<Button>().Where(b=>b.gameObject.activeInHierarchy&&b.IsInteractable()&&b.name==name).ToArray();
    if(buttons.Length==1){var b=buttons[0];ExecuteEvents.Execute(b.gameObject,Pointer(b.gameObject),ExecuteEvents.pointerClickHandler);NukeImportProbe.Record("UI_POINTER_CLICK",name);menuStep++;}
    return;
   }
   if(!inspected){inspected=true;if(NukeImportProbe.HasArgument("--nukehour-layout-test")){layoutTesting=true;StartCoroutine(CheckLayout(controls));}
    foreach(var b in controls.GetComponentsInChildren<ConfiguredActionButton>(true)){
     int squares=b.GetComponentsInChildren<Image>(true).Count(i=>i.enabled&&i.sprite==null&&i.color.a>.01f);
     int labels=b.GetComponentsInChildren<Text>(true).Count(t=>t.enabled);
     NukeImportProbe.Record("CONTROL",b.Binding.id+" opaqueEmptyImages="+squares+" labels="+labels+" receivers="+b.GetComponents<ConfiguredActionButton>().Length);
    }
   }
   if(layoutTesting)return;
   if(actions<32&&!CombatData.Ins.PauseAll){var action=actions==0?ControlAction.LongJump:actions==1?ControlAction.Jump:ControlAction.Attack;var target=controls.GetComponentsInChildren<ConfiguredActionButton>().FirstOrDefault(b=>b.Binding.action==action);
    if(target!=null){pressed=target.gameObject;ExecuteEvents.Execute(pressed,Pointer(pressed),ExecuteEvents.pointerDownHandler);releaseAt=now+.2f;actions++;NukeImportProbe.Record("CONTROL_POINTER_DOWN",action.ToString());next=now+.6f;}
   }
   foreach(var r in FindObjectsOfType<Renderer>())if(r.enabled)foreach(var m in r.sharedMaterials)if(m!=null&&(m.shader==null||!m.shader.isSupported||m.shader.name=="Hidden/InternalErrorShader"))NukeImportProbe.Record("BAD_RENDERER",r.name+" material="+m.name+" shader="+(m.shader==null?"NULL":m.shader.name));
   if(captures<8&&now-started>8+captures*5){ScreenCapture.CaptureScreenshot("combat-ui-"+captures+".png");NukeImportProbe.Record("COMBAT_CAPTURE",captures.ToString());captures++;}
  }
  System.Collections.IEnumerator CheckLayout(CustomCombatControls controls){
   var buttons=controls.GetComponentsInChildren<ConfiguredActionButton>();var jump=buttons.First(b=>b.Binding.id=="Jump");var other=buttons.First(b=>b.Binding.id=="LongJump");
   var start=Pointer(jump.gameObject);var end=Pointer(other.gameObject);end.pointerId=start.pointerId;float x=other.Binding.x,y=other.Binding.y;
   ExecuteEvents.Execute(jump.gameObject,start,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.85f);
   ExecuteEvents.Execute(jump.gameObject,end,ExecuteEvents.dragHandler);ExecuteEvents.Execute(jump.gameObject,end,ExecuteEvents.pointerUpHandler);
   bool moved=Math.Abs(jump.Binding.x-x)<.0001&&Math.Abs(jump.Binding.y-y)<.0001&&!controls.Reordering;
   NukeImportProbe.Record("LAYOUT_SWAP",moved?"PASS normal pointer hold+drag swapped Jump/LongJump":"FAIL");
   yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot("satellite-layout.png");
   if(NukeImportProbe.HasArgument("--nukehour-gyro-test")){
    var p=CustomCombatControls.Profile;bool previous=p.gyroEnabled;var gyro=controls.GetComponent<GyroLook>();int initial=gyro.Samples;p.gyroEnabled=true;
    yield return new WaitForSecondsRealtime(3);
    NukeImportProbe.Record("GYRO_HARDWARE","supported="+SystemInfo.supportsGyroscope+" sensorEnabled="+Input.gyro.enabled+" cameraSamples="+(gyro.Samples-initial)+" rate="+Input.gyro.rotationRateUnbiased+" delta="+gyro.LastDelta);
    p.gyroEnabled=false;yield return new WaitForSecondsRealtime(.1f);NukeImportProbe.Record("GYRO_OFF","sensorEnabled="+Input.gyro.enabled+" delta="+gyro.LastDelta);p.gyroEnabled=previous;
   }layoutTesting=false;
  }
  static PointerEventData Pointer(GameObject go){var rt=go.GetComponent<RectTransform>();var canvas=go.GetComponentInParent<Canvas>();var cam=canvas==null||canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;return new PointerEventData(EventSystem.current){pointerId=71,button=PointerEventData.InputButton.Left,position=rt!=null?RectTransformUtility.WorldToScreenPoint(cam,rt.TransformPoint(rt.rect.center)):Vector2.zero};}
  void OnDestroy(){if(pressed!=null&&EventSystem.current!=null)ExecuteEvents.Execute(pressed,Pointer(pressed),ExecuteEvents.pointerUpHandler);}
 }
}
