# usage: python3 -I pack_artifact.py <unity Builds/Web dir> <out dir>
# Repackages a Unity web build for a claude.ai artifact: data split under 15 MB per file,
# framework decompressed and loaded by URL (no blob: script), loader patched to join the chunks.
import gzip, os, shutil, sys
src, out = sys.argv[1], sys.argv[2]
b = os.path.join(src, 'Build')
shutil.rmtree(out, ignore_errors=True); os.makedirs(os.path.join(out, 'game'))
import base64
CH = 10 * 1024 * 1024   # raw bytes per file; base64 makes it about 13.4 MB, under the 15 MB limit
files = {}
def split(key, raw):
    names = []
    for i in range(0, len(raw), CH):
        name = 'game/%s%d.txt' % (key, len(names) + 1)
        open(os.path.join(out, name), 'w').write(base64.b64encode(raw[i:i + CH]).decode()); names.append(name)
    files['game/' + key] = names
split('data', open(os.path.join(b, 'Web.data.unityweb'), 'rb').read())
split('code', open(os.path.join(b, 'Web.wasm.unityweb'), 'rb').read())
open(os.path.join(out, 'game/framework.js'), 'wb').write(gzip.decompress(open(os.path.join(b, 'Web.framework.js.unityweb'), 'rb').read()))
L = open(os.path.join(b, 'Web.loader.js')).read()
a = 'var s=URL.createObjectURL(new Blob([e],{type:"application/javascript"}));'
assert L.count(a) == 1
L = L.replace(a, 'var s=b.frameworkDirect?b.frameworkUrl:URL.createObjectURL(new Blob([e],{type:"application/javascript"}));')
a = 'r=/file:\\/\\//.exec(n)?"same-origin":void 0;'
assert L.count(a) == 1, 'fetch hook'
L = L.replace(a, a + 'if(b.textParts&&b.textParts[n])t=function(){return Promise.all(b.textParts[n].map(function(c){return fetch(c).then(function(x){if(!x.ok)throw new Error("Could not load "+c);return x.text()}).then(function(x){var s=atob(x),u=new Uint8Array(s.length);for(var i=0;i<s.length;i++)u[i]=s.charCodeAt(i);b.onPart&&b.onPart(u.length);return u})})).then(function(p){var len=0;p.forEach(function(x){len+=x.length});var o=new Uint8Array(len),k=0;p.forEach(function(x){o.set(x,k);k+=x.length});return{parsedBody:o}})};')
open(os.path.join(out, 'game/loader.js'), 'w').write(L)
print(files)
