#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

// Never runs automatically. Invocation alone is insufficient: explicit opt-in is required.
public static class Phase0EHUnsignedIosExport {
    [Serializable] class ReportArtifact {
        public string utc, result, platform, outputPath;
        public ulong totalSize; public int errors,warnings; public double seconds;
        public string[] steps;
    }
    static void SaveReport(BuildReport report,string output) {
        var summary=report.summary;
        var artifact=new ReportArtifact {utc=DateTime.UtcNow.ToString("o"),result=summary.result.ToString(),platform=summary.platform.ToString(),outputPath=summary.outputPath,totalSize=summary.totalSize,errors=summary.totalErrors,warnings=summary.totalWarnings,seconds=summary.totalTime.TotalSeconds,
            steps=report.steps.Select(step=>step.name+" duration="+step.duration.TotalSeconds+" messages="+String.Join(" | ",step.messages.Select(message=>message.type+": "+message.content).ToArray())).ToArray()};
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        using(var file=new FileStream(output+".build-report.json",FileMode.CreateNew,FileAccess.Write))
        using(var writer=new StreamWriter(file))writer.Write(JsonUtility.ToJson(artifact,true));
    }
    public static void Export() {
        if (Environment.GetEnvironmentVariable("METEOR_ALLOW_IOS_EXPORT") != "YES")
            throw new InvalidOperationException("Explicit export opt-in missing; no settings changed.");
        if (!UnityEditorInternal.InternalEditorUtility.GetFullUnityVersion().Contains("2020.1.0f1 (2ab9c4179772)"))
            throw new InvalidOperationException("Exact Editor/revision required.");
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS,BuildTarget.iOS))
            throw new InvalidOperationException("Exact Editor iOS Build Support missing.");
        string output=Environment.GetEnvironmentVariable("PHASE0EH_EXPORT_DIR");
        if (String.IsNullOrEmpty(output)||!Path.IsPathRooted(output))throw new InvalidOperationException("Absolute external output required.");
        string project=Path.GetFullPath(Path.Combine(Application.dataPath,"..")).TrimEnd(Path.DirectorySeparatorChar);
        output=Path.GetFullPath(output).TrimEnd(Path.DirectorySeparatorChar);
        if (output==project||output.StartsWith(project+Path.DirectorySeparatorChar,StringComparison.Ordinal)||project.StartsWith(output+Path.DirectorySeparatorChar,StringComparison.Ordinal))
            throw new InvalidOperationException("Output must not overlap the project.");
        if (File.Exists(output)||Directory.Exists(output)||File.Exists(output+".build-report.json"))throw new InvalidOperationException("Output already exists; no overwrite allowed.");
        string[] scenes=EditorBuildSettings.scenes.Where(s=>s.enabled&&File.Exists(s.path)).Select(s=>s.path).ToArray();
        if (scenes.Length==0||!scenes.Contains("Assets/Scene/Patch.unity"))throw new InvalidOperationException("Enabled Patch scene required.");
        string requested=Environment.GetEnvironmentVariable("METEOR_RESOURCE_GATE_FILE");
        // The external resource gate is a separate prerequisite, never inferred from opt-in.
        if (String.IsNullOrEmpty(requested)||!File.Exists(requested)||File.ReadAllText(requested).Trim()!="RESOURCE_GATE=PASS")
            throw new InvalidOperationException("External validated resource-gate file missing.");
        if ((DateTime.UtcNow-File.GetLastWriteTimeUtc(requested)).TotalSeconds>60)
            throw new InvalidOperationException("Resource gate is stale; repeat two healthy samples.");
        var pressure = new System.Diagnostics.ProcessStartInfo("/usr/sbin/sysctl", "-n kern.memorystatus_vm_pressure_level") {
            UseShellExecute=false, RedirectStandardOutput=true, CreateNoWindow=true
        };
        using (var check=System.Diagnostics.Process.Start(pressure)) {
            if (!check.WaitForExit(2000) || check.ExitCode!=0)
                throw new InvalidOperationException("Live memory pressure query failed; abort export.");
            string livePressure=check.StandardOutput.ReadToEnd().Trim();
            bool level2Authorized=Environment.GetEnvironmentVariable("METEOR_ALLOW_LEVEL2_BUILD")=="PHASE0N_30MIN";
            if(livePressure!="1" && !(livePressure=="2" && level2Authorized))
                throw new InvalidOperationException("Live memory pressure outside authorized Phase0N build gate.");
            Debug.Log("PHASE0N_BUILD_GATE pressure="+livePressure+" level2Authorization="+level2Authorized);
        }
        if(new DriveInfo("/").AvailableFreeSpace<10L*1024*1024*1024)
            throw new InvalidOperationException("Startup disk reserve below 10 GiB.");
        var group=BuildTargetGroup.iOS;
        var backend=PlayerSettings.GetScriptingBackend(group);int architecture=PlayerSettings.GetArchitecture(group);
        bool defaults=PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.iOS);
        var apis=PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);
        bool signing=PlayerSettings.iOS.appleEnableAutomaticSigning;
        var orientation=PlayerSettings.defaultInterfaceOrientation;
        try {
            PlayerSettings.SetScriptingBackend(group,ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(group,1); // Unity 2020 iOS architecture: ARM64.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS,new[]{GraphicsDeviceType.Metal});
            PlayerSettings.iOS.appleEnableAutomaticSigning=false;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=scenes,locationPathName=output,target=BuildTarget.iOS,options=BuildOptions.None
            });
            SaveReport(report,output);
            Debug.Log("METEOR_IOS_EXPORT_RESULT "+report.summary.result+" errors="+report.summary.totalErrors);
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("iOS export failed; retain complete Editor log.");
        } finally {
            PlayerSettings.SetScriptingBackend(group,backend);PlayerSettings.SetArchitecture(group,architecture);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS,apis);PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS,defaults);
            PlayerSettings.iOS.appleEnableAutomaticSigning=signing;PlayerSettings.defaultInterfaceOrientation=orientation;
        }
    }
}
#endif
