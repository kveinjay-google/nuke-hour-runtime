using System;
namespace Meteor.PortPreparation {
 // Values consumed at the existing MeteorInput polling boundary, not damage/action calls.
 public static class PortInputState {
  static readonly bool[] Buttons=new bool[8];static Axis move;
  static readonly string[] Names={"Attack","Jump","Defence","ChangeWeapon","BreakOut","Crouch","Unlock","Pause"};
  public static bool Enabled {get;private set;}
  public static void Enable(bool enabled){Enabled=enabled;Array.Clear(Buttons,0,Buttons.Length);move=new Axis();}
  public static void SetButton(PortAction action,bool value){int index=(int)action;if(Enabled&&index>=0&&index<Buttons.Length)Buttons[index]=value;}
  public static bool Pressed(string axis,bool physical){if(!Enabled||String.IsNullOrEmpty(axis))return physical;if(axis=="W")return physical||move.Y>.25f;if(axis=="S")return physical||move.Y<-.25f;if(axis=="D")return physical||move.X>.25f;if(axis=="A")return physical||move.X<-.25f;for(int i=0;i<Names.Length;i++)if(Names[i]==axis)return physical||Buttons[i];return physical;}
  public static void SetMove(float x,float y){move=InputMerge.Clamp(x,y);}
  public static Axis MergeMove(float x,float y){return !Enabled||x!=0||y!=0?new Axis(x,y):move;}
 }
}
