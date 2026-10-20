#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
// Explicit diagnostic experiment: two adjacent stepped Game View frames, not gameplay qualification.
[InitializeOnLoad]
public static class Phase0IShaderAB {
    static readonly string Output=Environment.GetEnvironmentVariable("PHASE0I_RUN_DIR");
    static Material target; static Shader saved; static int stage; static double next;
    static string a,b;
    static Phase0IShaderAB(){if(Environment.GetEnvironmentVariable("PHASE0I_AB")=="1")EditorApplication.update+=Update;}
    static void Note(string text){File.AppendAllText(Path.Combine(Output,"shader-ab.log"),DateTime.UtcNow.ToString("o")+" frame="+Time.frameCount+" "+text+"\n");}
    static void Update(){
        if(String.IsNullOrEmpty(Output)||!EditorApplication.isPlaying||stage>=3||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+.2;
        if(File.Exists(Path.Combine(Output,"stop-editor")))return;
        if(stage==0){
            foreach(var renderer in Resources.FindObjectsOfTypeAll<MeshRenderer>()){
                if(!renderer.enabled||!renderer.isVisible)continue;
                var fx=renderer.GetComponent<SFXUnit>();
                if(fx==null||fx.source==null||fx.source.BlendType!=4||!fx.source.Texture.ToLowerInvariant().EndsWith(".bmp")||renderer.bounds.size.magnitude<10)continue;
                string textureName=fx.source.Texture.ToUpperInvariant();
                if(!textureName.Contains("LIGHT")&&!textureName.Contains("HITP")&&!textureName.Contains("ZSS"))continue;
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null||filter.sharedMesh.vertexCount<4)continue;
                foreach(var material in renderer.sharedMaterials){
                    if(material==null||!String.IsNullOrEmpty(AssetDatabase.GetAssetPath(material))||!material.HasProperty("_TintColor")||material.GetColor("_TintColor").a<.1f||material.mainTexture==null)continue;
                    target=material;saved=target.shader;EditorApplication.isPaused=true;
                    Note("SFX="+fx.source.EffectName+" BlendType="+fx.source.BlendType+" target="+renderer.name+" renderer="+renderer.GetInstanceID()+" material="+target.GetInstanceID()+" queue="+target.renderQueue+" vertices="+filter.sharedMesh.vertexCount+" texture="+AssetDatabase.GetAssetPath(target.mainTexture));
                    target.shader=Shader.Find("Custom/MeteorBlend4");target.renderQueue=3010;
                    a=Path.Combine(Output,"AB-A-original-surface.png");ScreenCapture.CaptureScreenshot(a);Note("A requested shader="+target.shader.name);stage=1;EditorApplication.Step();return;
                }
            }
        } else if(stage==1&&File.Exists(a)){
            if(target==null){Note("target destroyed; comparison invalid");stage=3;EditorApplication.isPaused=false;return;}
            target.shader=Shader.Find("UnlitAdditive");target.renderQueue=3010;
            b=Path.Combine(Output,"AB-B-additive.png");ScreenCapture.CaptureScreenshot(b);Note("B requested shader="+target.shader.name);stage=2;EditorApplication.Step();
        } else if(stage==2&&File.Exists(b)){
            if(target!=null){target.shader=saved;target.renderQueue=3010;}
            Note("A/B captured; adjacent stepped frames; paused diagnostic time excluded from performance");stage=3;EditorApplication.isPaused=false;
        }
    }
}
#endif
