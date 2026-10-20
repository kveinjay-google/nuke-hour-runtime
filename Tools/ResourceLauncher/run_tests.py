from pathlib import Path
import subprocess,json,sys,os,tempfile
here=Path(__file__).resolve().parent;project=here.parents[1]
out=Path(os.environ.get('METEOR_TEST_EVIDENCE') or tempfile.mkdtemp(prefix='meteor-launcher-tests-'));out.mkdir(parents=True,exist_ok=True)
core=project/'Assets/Plugins/MeteorLauncherCore';ui=project/'Assets/PortPreparation/Launcher'
mono=os.environ.get('UNITY_MONO_BIN','/Applications/Unity/Unity.app/Contents/MonoBleedingEdge/bin')
results=[]
for name,sources in [('LauncherTests',[core/'ContentInstall.cs',ui/'ButtonHoldState.cs',ui/'ControlProfile.cs']),('LanTests',[core/'ContentInstall.cs',ui/'LanContentServer.cs'])]:
 for command in [[str(Path(mono)/'mcs'),'-out:'+str(out/(name+'.exe')),str(here/'tests'/(name+'.cs'))]+[str(s) for s in sources],[str(Path(mono)/'mono'),str(out/(name+'.exe'))]]:
  r=subprocess.run(command,capture_output=True,text=True,timeout=45);results.append(dict(command=command,exit=r.returncode,stdout=r.stdout,stderr=r.stderr));print(r.stdout+r.stderr)
  if r.returncode:break
r=subprocess.run([sys.executable,str(here/'tests/test_release_stage.py')],capture_output=True,text=True,timeout=30);results.append(dict(test='staging',exit=r.returncode,stdout=r.stdout,stderr=r.stderr));print(r.stdout+r.stderr)
(out/'test-results.json').write_text(json.dumps(results,ensure_ascii=False,indent=2));print('Evidence:',out);sys.exit(any(x['exit'] for x in results))
