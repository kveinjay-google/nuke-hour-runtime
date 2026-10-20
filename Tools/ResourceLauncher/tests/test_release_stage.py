import importlib.util,tempfile,unittest
from pathlib import Path
spec=importlib.util.spec_from_file_location('stage',Path(__file__).parents[1]/'stage_release.py')
class StageTests(unittest.TestCase):
 def test_policy(self):
  self.assertIsNotNone(spec.loader);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
  for p in ['Assets/Resources/map.png','Assets/LauncherArt/another-game.png','Assets/Scene/Patch.unity','Assets/Resources/Level.json','Assets/Meteor/music.mp3','Assets/Materials/a.mat','Assets/Resources/a.bytes']:
   self.assertFalse(m.allowed(Path(p)),p)
  for p in ['Assets/PortPreparation/Launcher/MeteorLauncher.cs','Assets/Plugins/iOS/libslua.a','Assets/PortPreparation/link.xml','Assets/LauncherArt/volcanic-night.png']:
   self.assertTrue(m.allowed(Path(p)),p)
 def test_copy_preserves_identity_excludes_assets(self):
  m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
  with tempfile.TemporaryDirectory() as d:
   root=Path(d);source=root/'source';target=root/'stage'
   for p,data in {'Assets/Test.cs':'class Test{}','Assets/Test.cs.meta':'guid: abc','Assets/Resources/map.png':'game','ProjectSettings/ProjectVersion.txt':'version','Packages/manifest.json':'{}'}.items():
    f=source/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_text(data)
   m.stage(source,target)
   self.assertEqual((target/'Assets/Test.cs.meta').read_text(),'guid: abc');self.assertFalse((target/'Assets/Resources/map.png').exists())
   with self.assertRaises(FileExistsError):m.stage(source,target)
if __name__=='__main__':unittest.main()
