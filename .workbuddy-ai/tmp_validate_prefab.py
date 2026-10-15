import re, io, sys
from collections import Counter

path = sys.argv[1]
t = io.open(path, encoding='utf-8', errors='replace', newline='').read().replace('\r\n', '\n')
NL = chr(10)

# 1) block header ids
ids = re.findall(r'^--- !u!(\d+) &(\d+)( stripped)?', t, re.M)
cnt = Counter(i[1] for i in ids)
dups = {k: v for k, v in cnt.items() if v > 1}
print('blocks:', len(ids), '| duplicate fileIDs:', dups or 'none')

defined = set(cnt.keys())
# 2) all {fileID: N} refs (exclude 0 and builtin guids lines)
refs = re.findall(r'\{fileID: (\d+)(?:, guid: ([0-9a-f]{32}), type: \d+)?\}', t)
missing = []
for fid, guid in refs:
    if fid == '0':
        continue
    if guid:      # external asset ref -> not defined locally
        continue
    if fid not in defined:
        missing.append(fid)
missing = sorted(set(missing))
print('dangling local refs:', missing or 'none')

# 3) m_Children refs must point to defined RT/Transform
print()
print('=== m_Children 引用检查 ===')
for m in re.finditer(r'^  m_Children:' + NL + r'((?:  - \{fileID: \d+\}' + NL + r')+)', t, re.M):
    kids = re.findall(r'fileID: (\d+)', m.group(1))
    bad = [k for k in kids if k not in defined]
    if bad:
        print('  BAD children:', bad)

# 4) hierarchy
GO_PAT = r'--- !u!1 &(\d+)( stripped)?' + NL + r'GameObject:' + NL + r'(.*?)(?=' + NL + r'--- !u!)'
RT_PAT = r'--- !u!(?:4|224) &(\d+)( stripped)?' + NL + r'(?:Transform|RectTransform):' + NL + r'(.*?)(?=' + NL + r'--- !u!)'
PI_PAT = r'--- !u!1001 &(\d+)' + NL + r'PrefabInstance:' + NL + r'(.*?)(?=' + NL + r'--- !u!)'

gos, rts, pis = {}, {}, {}
for m in re.finditer(GO_PAT, t, re.S):
    n = re.search(r'm_Name: (.*)', m.group(3))
    gos[m.group(1)] = (n.group(1).strip() if n else '?')
for m in re.finditer(RT_PAT, t, re.S):
    b = m.group(3)
    g = re.search(r'm_GameObject: \{fileID: (\d+)\}', b)
    ch = re.findall(r'^\s+- \{fileID: (\d+)\}', b, re.M)
    fa = re.search(r'm_Father: \{fileID: (\d+)\}', b)
    src = re.search(r'm_CorrespondingSourceObject: \{fileID: (\d+), guid: ([0-9a-f]+)', b)
    rts[m.group(1)] = dict(go=(g.group(1) if g else None), ch=ch,
                           fa=(fa.group(1) if fa else None),
                           src=(src.group(1), src.group(2)) if src else None)
for m in re.finditer(PI_PAT, t, re.S):
    b = m.group(2)
    tp = re.search(r'm_TransformParent: \{fileID: (\d+)\}', b)
    sp = re.search(r'm_SourcePrefab: \{fileID: \d+, guid: ([0-9a-f]+)', b)
    pis[m.group(1)] = (tp.group(1) if tp else None, sp.group(1) if sp else None)

def nm(rt):
    e = rts.get(rt)
    if not e: return '?' + rt
    if e['go'] and e['go'] in gos: return gos[e['go']]
    if e['src']: return 'nested@' + e['src'][1][:8]
    return '?' + rt

roots = [rt for rt, v in rts.items() if v['fa'] == '0']
out = []
def walk(rt, d=0):
    if d > 8 or len(out) > 100: return
    e = rts.get(rt, {})
    out.append('  ' * d + nm(rt) + ' [' + rt + ']')
    for c in e.get('ch', []): walk(c, d + 1)
for r in roots: walk(r)
print()
print('=== 层级 ===')
print(NL.join(out))
print()
print('=== PrefabInstance ===')
for k, v in pis.items():
    print('  PI', k, '| parent=', v[0], '(', nm(v[0]) if v[0] else '-', ') | src=', v[1])
