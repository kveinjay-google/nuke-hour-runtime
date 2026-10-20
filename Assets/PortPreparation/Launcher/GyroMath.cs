using System;
namespace Meteor.Launcher {
 public static class GyroMath {
  public static float Sensitivity(float value){return Single.IsNaN(value)||Single.IsInfinity(value)?1:Math.Max(.1f,Math.Min(3,value));}
  public static float Delta(float radians,float seconds,float sensitivity){if(Single.IsNaN(radians)||Single.IsInfinity(radians)||Single.IsNaN(seconds)||Single.IsInfinity(seconds)||seconds<=0)return 0;if(Math.Abs(radians)<.003f)return 0;return Math.Max(-8,Math.Min(8,radians))*Math.Min(seconds,.05f)*57.29578f*Sensitivity(sensitivity);}
 }
}
