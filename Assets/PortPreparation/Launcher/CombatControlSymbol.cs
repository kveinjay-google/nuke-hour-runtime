using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Meteor.Launcher {
 // Geometry only: no font dependency, square texture, or bundled game artwork.
 public sealed class CombatControlSymbol : MaskableGraphic {
  public bool Gear;
  protected override void OnPopulateMesh(VertexHelper vh){
   vh.Clear();var r=GetPixelAdjustedRect();var c=r.center;float scale=Mathf.Min(r.width,r.height);
   if(Gear){
    Ring(vh,c,scale*.25f,scale*.36f,48);
    for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var n=new Vector2(-d.y,d.x);Quad(vh,c+d*scale*.32f-n*scale*.07f,c+d*scale*.46f-n*scale*.07f,c+d*scale*.46f+n*scale*.07f,c+d*scale*.32f+n*scale*.07f);}
   }else{
    // Double upward chevron distinguishes long jump from the native jump silhouette.
    for(int i=0;i<2;i++){float y=c.y+scale*(.24f+i*.11f);Line(vh,new Vector2(c.x-scale*.11f,y-scale*.06f),new Vector2(c.x,y),scale*.024f);Line(vh,new Vector2(c.x,y),new Vector2(c.x+scale*.11f,y-scale*.06f),scale*.024f);}
   }
  }
  void Line(VertexHelper v,Vector2 a,Vector2 b,float width){var n=new Vector2(-(b-a).y,(b-a).x).normalized*width;Quad(v,a-n,b-n,b+n,a+n);}
  void Ring(VertexHelper v,Vector2 c,float inner,float outer,int segments){for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;var x=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var y=new Vector2(Mathf.Cos(b),Mathf.Sin(b));Quad(v,c+x*inner,c+x*outer,c+y*outer,c+y*inner);}}
  void Quad(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Vector2 d){int i=v.currentVertCount;v.AddVert(a,color,Vector2.zero);v.AddVert(b,color,Vector2.zero);v.AddVert(c,color,Vector2.zero);v.AddVert(d,color,Vector2.zero);v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);}
 }
 public sealed class CombatSettingsClick:MonoBehaviour,IPointerClickHandler {
  public void OnPointerClick(PointerEventData e){if(!CombatData.Ins.PauseAll)U3D.OpenSystemWnd();CustomCombatControls.OpenSettings();}
 }
}
