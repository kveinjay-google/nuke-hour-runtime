using System;
using System.IO;
using System.Collections.Generic;
namespace Meteor.Launcher {
 // Owns input sources, not gameplay state. Times are unscaled monotonic seconds.
 public sealed class ButtonHoldState {
  struct Hold {public ControlAction Action;public double Until;public bool Released,Observed;}
  readonly Dictionary<int,Hold> sources=new Dictionary<int,Hold>();
  readonly List<int> expired=new List<int>(32);
  readonly List<KeyValuePair<int,Hold>> observed=new List<KeyValuePair<int,Hold>>(32);
  int nextSource;
  public int NewSource(){for(int i=0;i<33;i++){nextSource=nextSource==Int32.MaxValue?1:nextSource+1;if(!sources.ContainsKey(nextSource))return nextSource;}throw new InvalidOperationException("Input source limit reached");}
  public void Down(int source,ControlAction action,double now){if(!Enum.IsDefined(typeof(ControlAction),action))throw new InvalidDataException("Unknown control action");if(sources.ContainsKey(source)||sources.Count>=32)return;sources.Add(source,new Hold{Action=action,Until=now+.18});}
  public void Up(int source,double now){Hold h;if(!sources.TryGetValue(source,out h))return;if(!h.Observed||(h.Action==ControlAction.LongJump&&now<h.Until)){h.Released=true;sources[source]=h;}else sources.Remove(source);}
  public bool IsHeld(ControlAction action,double now){
   expired.Clear();observed.Clear();bool result=false;
   foreach(var pair in sources){var h=pair.Value;if(h.Released&&h.Observed&&(h.Action!=ControlAction.LongJump||now>=h.Until)){expired.Add(pair.Key);continue;}
    var mapped=h.Action==ControlAction.LongJump?ControlAction.Jump:h.Action;if(mapped!=action)continue;result=true;
    if(!h.Observed){h.Observed=true;if(h.Released&&h.Action==ControlAction.LongJump&&now>=h.Until)h.Until=now+.18;observed.Add(new KeyValuePair<int,Hold>(pair.Key,h));}
   }
   foreach(var item in observed)sources[item.Key]=item.Value;foreach(int id in expired)sources.Remove(id);return result;
  }
  public void Cancel(){sources.Clear();}
 }
}
