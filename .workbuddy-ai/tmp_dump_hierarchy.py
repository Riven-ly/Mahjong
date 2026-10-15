import re, io, sys

path = sys.argv[1]
t = io.open(path, encoding='utf-8', errors='replace', newline='').read().replace('\r\n', '\n')
NL = chr(10)

GO_PAT = r'--- !u!1 &(\d+)( stripped)?' + NL + r'GameObject:' + NL + r'(.*?)(?=' + NL + r'--- !u!)'
RT_PAT = r'--- !u!(?:4|224) &(\d+)( stripped)?' + NL + r'(?:Transform|RectTransform):' + NL + r'(.*?)(?=' + NL + r'--- !u!)'
PI_PAT = r'--- !u!1001 &(\d+)' + NL + r'PrefabInstance:' + NL + r'(.*?)(?=' + NL + r'--- !u!)'

gos, rts, pis = {}, {}, {}
for m in re.finditer(GO_PAT, t, re.S):
    n = re.search(r'm_Name: (.*)', m.group(3))
    gos[m.group(1)] = (n.group(1).strip() if n else '?', bool(m.group(2)))
for m in re.finditer(RT_PAT, t, re.S):
    b = m.group(3)
    g = re.search(r'm_GameObject: \{fileID: (\d+)\}', b)
    ch = re.findall(r'^\s+- \{fileID: (\d+)\}', b, re.M)
    fa = re.search(r'm_Father: \{fileID: (\d+)\}', b)
    src = re.search(r'm_CorrespondingSourceObject: \{fileID: (\d+), guid: ([0-9a-f]+)', b)
    rts[m.group(1)] = dict(go=(g.group(1) if g else None), ch=ch,
                           fa=(fa.group(1) if fa else None),
                           stripped=bool(m.group(2)),
                           src=(src.group(1), src.group(2)) if src else None)
for m in re.finditer(PI_PAT, t, re.S):
    b = m.group(2)
    tp = re.search(r'm_TransformParent: \{fileID: (\d+)\}', b)
    sp = re.search(r'm_SourcePrefab: \{fileID: \d+, guid: ([0-9a-f]+)', b)
    nm = re.search(r'propertyPath: m_Name' + NL + r'\s+value: (.*)', b)
    pis[m.group(1)] = dict(parent=(tp.group(1) if tp else None),
                           src=(sp.group(1) if sp else None),
                           name=(nm.group(1).strip() if nm else None))

def nm(rt):
    e = rts.get(rt)
    if not e:
        return '?' + rt
    g = e['go']
    if g and g in gos:
        return gos[g][0]
    if e['src']:
        return 'nested@' + e['src'][1][:8]
    return '?' + rt

roots = [rt for rt, v in rts.items() if v['fa'] == '0']
print('GO:', len(gos), ' RT:', len(rts), ' PrefabInstance:', len(pis), ' roots:', len(roots))
print()

out = []
def walk(rt, d=0):
    if d > 8 or len(out) > 300:
        return
    e = rts.get(rt, {})
    extra = ''
    if e.get('stripped'):
        extra += '  <stripped src=%s>' % (e['src'][1][:8] if e.get('src') else '?')
    out.append('  ' * d + nm(rt) + '  [' + rt + ']' + extra)
    for c in e.get('ch', []):
        walk(c, d + 1)
for r in roots:
    walk(r)
print(NL.join(out))
print()
print('=== PrefabInstance 列表 ===')
for k, v in pis.items():
    print('  PI', k, '| parent=', v['parent'], '(', nm(v['parent']) if v['parent'] else '-', ') | src=', v['src'], '| name=', v['name'])
print()
print('=== 未被树覆盖的 RT ===')
seen = set()
def mark(rt):
    if rt in seen: return
    seen.add(rt)
    for c in rts.get(rt, {}).get('ch', []): mark(c)
for r in roots: mark(r)
for rt, v in rts.items():
    if rt not in seen:
        print('  ', nm(rt), rt, 'father=', v['fa'], '(', nm(v['fa']) if v['fa'] else '-', ')')
