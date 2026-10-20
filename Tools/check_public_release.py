"""Fail closed on non-source payloads, credentials and large files in public source."""
from pathlib import Path
import re,sys,json
root=Path(__file__).resolve().parents[1];errors=[];count=0
allowed={'.cs','.c','.cpp','.h','.mm','.shader','.cginc','.asmdef','.asmref','.meta','.xml','.rsp','.py','.md','.json','.txt','.asset'}
for f in root.rglob('*'):
 if not f.is_file() or '.git' in f.relative_to(root).parts:continue
 p=f.relative_to(root);count+=1
 if '__pycache__' in p.parts:continue
 if f.is_symlink():errors.append(str(p)+': symlink')
 if f.stat().st_size>50*1024**2:errors.append(str(p)+': oversized')
 if f.suffix not in allowed and f.name not in {'.gitignore','LICENSE','NOTICE'}:errors.append(str(p)+': non-source payload')
 if any(s in p.parts for s in ['PrivateUserContent','ThirdParty','Library','Temp']):errors.append(str(p)+': excluded directory')
 data=f.read_bytes()
 if re.search(rb'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|ghp_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{50,}',data):errors.append(str(p)+': credential signature')
print(json.dumps({'files_checked':count,'errors':errors,'scope':'source-only payload/credential checks; not a full clean-checkout Unity build'},indent=2));sys.exit(bool(errors))
