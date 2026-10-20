using System;

namespace Meteor.PortPreparation
{
    // Prototype only: no scene registration or replacement of existing keyboard input.
    public enum PortAction { Attack, Jump, Defence, ChangeWeapon, BreakOut, Crouch, Unlock, Pause }
    public interface IPortInputSink {
        void ButtonDown(PortAction action);
        void ButtonUp(PortAction action);
        void ButtonHeld(PortAction action);
        void Move(float x, float y);
        void Look(float x, float y);
    }
    public struct Axis {
        public float X, Y;
        public Axis(float x, float y) { X=x; Y=y; }
    }
    public static class InputMerge {
        public static bool Finite(float v) { return !Single.IsNaN(v) && !Single.IsInfinity(v); }
        public static Axis Clamp(float x, float y) {
            if (!Finite(x) || !Finite(y)) return new Axis();
            double length=Math.Sqrt((double)x*x+(double)y*y);
            return length>1 ? new Axis((float)(x/length),(float)(y/length)) : new Axis(x,y);
        }
        // Keyboard priority is explicit; do not sum vectors into diagonal speed boosts.
        public static Axis Move(float keyboardX,float keyboardY,float fallbackX,float fallbackY) {
            Axis key=Clamp(keyboardX,keyboardY);
            return key.X!=0 || key.Y!=0 ? key : Clamp(fallbackX,fallbackY);
        }
    }
    public struct SafeArea {
        public readonly float X,Y,Width,Height;
        public SafeArea(float x,float y,float width,float height) {
            if (!InputMerge.Finite(x)||!InputMerge.Finite(y)||!InputMerge.Finite(width)||!InputMerge.Finite(height)||width<=0||height<=0)
                throw new ArgumentOutOfRangeException("width","Safe area must be finite and positive");
            X=x;Y=y;Width=width;Height=height;
        }
        public void Project(float nx,float ny,out float x,out float y) {
            if (!InputMerge.Finite(nx)||!InputMerge.Finite(ny)) throw new ArgumentOutOfRangeException("nx");
            x=X+Math.Max(0,Math.Min(1,nx))*Width; y=Y+Math.Max(0,Math.Min(1,ny))*Height;
        }
    }
    public sealed class TouchInputRouter {
        const int MaxPointers=10;
        struct Capture { public bool Active; public int Id,Kind; public PortAction Action; public float X,Y,Scale; }
        readonly Capture[] pointers=new Capture[MaxPointers];
        readonly int[] held=new int[8];
        readonly IPortInputSink sink;
        float moveX,moveY,lookX,lookY;
        public int ActivePointers { get; private set; }
        public TouchInputRouter(IPortInputSink sink) { if(sink==null)throw new ArgumentNullException("sink");this.sink=sink; }
        int Find(int id) { for(int i=0;i<MaxPointers;i++)if(pointers[i].Active&&pointers[i].Id==id)return i;return -1; }
        int Slot(int id,int kind) {
            if(Find(id)>=0||ActivePointers>=MaxPointers)return -1;
            if(kind!=0)for(int i=0;i<MaxPointers;i++)if(pointers[i].Active&&pointers[i].Kind==kind)return -1;
            for(int i=0;i<MaxPointers;i++)if(!pointers[i].Active)return i;
            return -1;
        }
        public bool BeginButton(int id,PortAction action) {
            int a=(int)action;if(a<0||a>=held.Length)return false;
            int i=Slot(id,0);if(i<0)return false;
            pointers[i]=new Capture{Active=true,Id=id,Kind=0,Action=action};ActivePointers++;
            if(held[a]++==0)sink.ButtonDown(action);return true;
        }
        bool BeginAxis(int id,int kind,float x,float y,float scale) {
            if(!InputMerge.Finite(x)||!InputMerge.Finite(y)||!InputMerge.Finite(scale)||scale<=0)return false;
            int i=Slot(id,kind);if(i<0)return false;
            pointers[i]=new Capture{Active=true,Id=id,Kind=kind,X=x,Y=y,Scale=scale};ActivePointers++;return true;
        }
        public bool BeginMove(int id,float centerX,float centerY,float radius) { return BeginAxis(id,1,centerX,centerY,radius); }
        public bool BeginLook(int id,float x,float y,float pixelsPerUnit) { return BeginAxis(id,2,x,y,pixelsPerUnit); }
        public void MovePointer(int id,float x,float y) {
            int i=Find(id);if(i<0||!InputMerge.Finite(x)||!InputMerge.Finite(y))return;
            Capture c=pointers[i];
            Axis a=InputMerge.Clamp((x-c.X)/c.Scale,(y-c.Y)/c.Scale);
            if(c.Kind==1) { moveX=a.X;moveY=a.Y;if((double)moveX*moveX+(double)moveY*moveY<.12*.12){moveX=0;moveY=0;} }
            if(c.Kind==2) { Axis delta=InputMerge.Clamp(lookX+a.X,lookY+a.Y);lookX=delta.X;lookY=delta.Y;c.X=x;c.Y=y;pointers[i]=c; }
        }
        public void End(int id) {
            int i=Find(id);if(i<0)return;Capture c=pointers[i];pointers[i]=new Capture();ActivePointers--;
            if(c.Kind==0&&--held[(int)c.Action]==0)sink.ButtonUp(c.Action);
            if(c.Kind==1){moveX=0;moveY=0;}
            if(c.Kind==2){lookX=0;lookY=0;}
        }
        public void CancelAll() { for(int i=0;i<MaxPointers;i++)if(pointers[i].Active)End(pointers[i].Id); }
        public void Tick() {
            sink.Move(moveX,moveY);sink.Look(lookX,lookY);lookX=0;lookY=0;
            for(int i=0;i<held.Length;i++)if(held[i]>0)sink.ButtonHeld((PortAction)i);
        }
    }
}
