import os
import re
import sys

ROOT = r"D:\AA_Mahjong\Git_Mahjong"
FONT_REF = "{fileID: 12800000, guid: 8e8f304955380a942bf4b4d27b2e6d7e, type: 3}"

TMP_TEXT_UGUI = "f4688fdb7df04437aeb418b961361dc5"
TMP_TEXT_WORLD = "9541d86e2fd84c1d9990edf0852d74ab"
TMP_INPUTFIELD = "2da0c512f12947e489f739169773d7ca"
TMP_DROPDOWN = "7b743370ac3e4ec2a1668f5455a8ef8a"
LEGACY_TEXT = "5f7201a12d95ffc409449d95f23cf332"
LEGACY_INPUTFIELD = "d199490a83bb2b844b9695cbf13b01ef"

PREFABS = [a for a in sys.argv[1:] if not a.startswith("--")] or [
    r"Assets\ABRes\Tx\UI\Panel\AcCandyPop.prefab",
    r"Assets\ABRes\Tx\UI\Panel\BottomNotice.prefab",
    r"Assets\ABRes\Tx\UI\Panel\CandyPanel.prefab",
    r"Assets\ABRes\Tx\UI\Panel\ConfirmAcPop.prefab",
    r"Assets\ABRes\Tx\UI\Panel\GuidePop.prefab",
    r"Assets\ABRes\Tx\UI\Panel\InProgressPop.prefab",
    r"Assets\ABRes\Tx\UI\Panel\LevelPopup.prefab",
    r"Assets\ABRes\Tx\UI\Panel\LevelTaskTip.prefab",
    r"Assets\ABRes\Tx\UI\Panel\ProcessCompletePop.prefab",
    r"Assets\ABRes\Tx\UI\Panel\RecordPop.prefab",
    r"Assets\ABRes\Tx\UI\Panel\SpecialBonusPop.prefab",
    r"Assets\ABRes\Tx\UI\Panel\TopNotice.prefab",
    r"Assets\ABRes\Tx\UI\Panel\TxHudSliders.prefab",
    r"Assets\ABRes\Tx\UI\Panel\TxTipsRoot.prefab",
    r"Assets\ABRes\Tx\UI\Panel\TxWithdrawBar.prefab",
    r"Assets\Resources\UI\Panel\RewardTip.prefab",
]

APPLY = "--apply" in sys.argv


def parse_top_level(body):
    """把组件块 body 解析成 [(key, raw_value_lines)]，保留原始行。"""
    items = []
    lines = body.split("\n")
    i = 0
    while i < len(lines):
        line = lines[i]
        m = re.match(r"^  ([A-Za-z_][A-Za-z0-9_]*):(.*)$", line)
        if m:
            key = m.group(1)
            val = m.group(2)
            raw = [line]
            # 处理跨行标量（单引号/双引号未闭合）
            while val.count("'") % 2 == 1 or val.count('"') % 2 == 1:
                i += 1
                if i >= len(lines):
                    break
                raw.append(lines[i])
                val += "\n" + lines[i]
            items.append((key, "\n".join(raw), val.strip()))
            i += 1
            continue
        # 缩进的子块（如 m_FontData / m_Navigation）归属上一个 key
        if items and line.startswith("    "):
            key, raw, val = items[-1]
            items[-1] = (key, raw + "\n" + line, val)
        i += 1
    return items


def get(items, key, default=None):
    for k, raw, val in items:
        if k == key:
            return val
    return default


def raw_block(items, key):
    for k, raw, val in items:
        if k == key:
            return raw
    return None


def num(s, default=0.0):
    try:
        return float(s)
    except (TypeError, ValueError):
        return default


def fmt(v):
    if abs(v - round(v)) < 1e-6:
        return str(int(round(v)))
    return ("%.6f" % v).rstrip("0")


def color_of(items, key, default=(1, 1, 1, 1)):
    val = get(items, key)
    if not val:
        return default
    m = re.match(r"\{r: ([-\d.eE]+), g: ([-\d.eE]+), b: ([-\d.eE]+), a: ([-\d.eE]+)\}", val)
    if not m:
        return default
    return tuple(float(x) for x in m.groups())


def color_line(rgba):
    return "  m_Color: {r: %s, g: %s, b: %s, a: %s}" % tuple(fmt(c) for c in rgba)


def map_font_style(tmp_style):
    v = int(num(tmp_style, 0))
    bold = bool(v & 1)
    italic = bool(v & 2)
    underline = bool(v & 4)
    if bold and italic:
        return 3
    if bold:
        return 1
    if italic:
        return 2
    if underline:
        return 4
    return 0


def map_alignment(h, v):
    h = int(num(h, 1))
    v = int(num(v, 256))
    if h & 0x2:
        col = 1
    elif h & 0x4:
        col = 2
    else:
        col = 0  # Left / Justified / Flush 都退化为左对齐
    if v & 0x400:
        row = 2
    elif v & 0x200:
        row = 1
    else:
        row = 0  # Top / Baseline / Capline 都按顶对齐
    return row * 3 + col


def build_text_block(anchor, items, stats):
    go = get(items, "m_GameObject", "{fileID: 0}")
    enabled = get(items, "m_Enabled", "1")
    graphic_color = color_of(items, "m_Color")
    font_color = color_of(items, "m_fontColor")
    rgba = tuple(a * b for a, b in zip(graphic_color, font_color))

    auto_size = int(num(get(items, "m_enableAutoSizing"), 0))
    font_size = int(round(num(get(items, "m_fontSizeBase"), num(get(items, "m_fontSize"), 36))))
    min_size = int(round(num(get(items, "m_fontSizeMin"), 10)))
    max_size = int(round(num(get(items, "m_fontSizeMax"), 40)))
    if max_size < min_size:
        max_size = min_size
    if font_size <= 0:
        font_size = 36
    if min_size <= 0:
        min_size = 1
    if max_size <= 0:
        max_size = font_size

    style = map_font_style(get(items, "m_fontStyle"))
    align = map_alignment(get(items, "m_HorizontalAlignment"), get(items, "m_VerticalAlignment"))
    rich = int(num(get(items, "m_isRichText"), 1))
    wrap = int(num(get(items, "m_enableWordWrapping"), 1))
    overflow_mode = int(num(get(items, "m_overflowMode"), 0))
    raycast = int(num(get(items, "m_RaycastTarget"), 1))
    maskable = int(num(get(items, "m_Maskable"), 1))
    text_raw = get(items, "m_text", "")

    stats["style"].add(style)
    stats["align"].add(align)
    stats["bestfit"].add(auto_size)
    stats["size"].add(font_size)
    stats["spacing_nonzero"] += 1 if abs(num(get(items, "m_characterSpacing"), 0)) > 0.001 else 0

    lines = [
        anchor,
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: %s" % go,
        "  m_Enabled: %s" % enabled,
        "  m_EditorHideFlags: 0",
        "  m_Script: {fileID: 11500000, guid: %s, type: 3}" % LEGACY_TEXT,
        "  m_Name: ",
        "  m_EditorClassIdentifier: ",
        "  m_Material: {fileID: 0}",
        color_line(rgba),
        "  m_RaycastTarget: %d" % raycast,
        "  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}",
        "  m_Maskable: %d" % maskable,
        "  m_OnCullStateChanged:",
        "    m_PersistentCalls:",
        "      m_Calls: []",
        "  m_FontData:",
        "    m_Font: %s" % FONT_REF,
        "    m_FontSize: %d" % font_size,
        "    m_FontStyle: %d" % style,
        "    m_BestFit: %d" % auto_size,
        "    m_MinSize: %d" % min_size,
        "    m_MaxSize: %d" % max_size,
        "    m_Alignment: %d" % align,
        "    m_AlignByGeometry: 0",
        "    m_RichText: %d" % rich,
        "    m_HorizontalOverflow: %d" % (0 if wrap else 1),
        "    m_VerticalOverflow: 0",
        "    m_LineSpacing: 1",
        "  m_Text:%s" % ((" " + text_raw) if text_raw != "" else " "),
        "",
    ]
    return "\n".join(lines)


def build_inputfield_block(anchor, items, stats):
    go = get(items, "m_GameObject", "{fileID: 0}")
    enabled = get(items, "m_Enabled", "1")

    def val(key, default):
        v = get(items, key)
        return v if v not in (None, "") else default

    out = [
        anchor,
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: %s" % go,
        "  m_Enabled: %s" % enabled,
        "  m_EditorHideFlags: 0",
        "  m_Script: {fileID: 11500000, guid: %s, type: 3}" % LEGACY_INPUTFIELD,
        "  m_Name: ",
        "  m_EditorClassIdentifier: ",
    ]
    for key in ("m_Navigation", "m_Transition", "m_Colors", "m_SpriteState", "m_AnimationTriggers"):
        block = raw_block(items, key)
        if block:
            out.extend(block.split("\n"))
    out.append("  m_Interactable: %s" % val("m_Interactable", "1"))
    out.append("  m_TargetGraphic: %s" % val("m_TargetGraphic", "{fileID: 0}"))
    out.append("  m_TextComponent: %s" % val("m_TextComponent", "{fileID: 0}"))
    out.append("  m_Placeholder: %s" % val("m_Placeholder", "{fileID: 0}"))
    out.append("  m_ContentType: %s" % val("m_ContentType", "0"))
    out.append("  m_InputType: %s" % val("m_InputType", "0"))
    out.append("  m_AsteriskChar: %s" % val("m_AsteriskChar", "42"))
    out.append("  m_KeyboardType: %s" % val("m_KeyboardType", "0"))
    out.append("  m_LineType: %s" % val("m_LineType", "0"))
    out.append("  m_HideMobileInput: %s" % val("m_HideMobileInput", "0"))
    out.append("  m_CharacterValidation: %s" % val("m_CharacterValidation", "0"))
    out.append("  m_CharacterLimit: %s" % val("m_CharacterLimit", "0"))
    for new_key, old_key in (("m_OnSubmit", "m_OnSubmit"), ("m_OnDidEndEdit", "m_OnEndEdit"), ("m_OnValueChanged", "m_OnValueChanged")):
        block = raw_block(items, old_key)
        if block:
            out.append("  %s:" % new_key)
            out.extend(block.split("\n")[1:])
        else:
            out.append("  %s:" % new_key)
            out.append("    m_PersistentCalls:")
            out.append("      m_Calls: []")
    out.append("  m_CaretColor: %s" % val("m_CaretColor", "{r: 0.19607843, g: 0.19607843, b: 0.19607843, a: 1}"))
    out.append("  m_CustomCaretColor: %s" % val("m_CustomCaretColor", "0"))
    out.append("  m_SelectionColor: %s" % val("m_SelectionColor", "{r: 0.65882355, g: 0.80784315, b: 1, a: 0.7529412}"))
    out.append("  m_Text:%s" % ((" " + val("m_Text", "")) if val("m_Text", "") != "" else " "))
    out.append("  m_CaretBlinkRate: %s" % val("m_CaretBlinkRate", "0.85"))
    out.append("  m_CaretWidth: %s" % val("m_CaretWidth", "1"))
    out.append("  m_ReadOnly: %s" % val("m_ReadOnly", "0"))
    out.append("  m_ShouldActivateOnSelect: %s" % val("m_ShouldActivateOnSelect", "1"))
    out.append("")
    stats["inputfield"] += 1
    return "\n".join(out)


def process(path):
    full = os.path.join(ROOT, path)
    txt = open(full, encoding="utf-8").read()
    blocks = re.split(r"(?m)^(?=--- )", txt)
    out = []
    stats = {"text": 0, "inputfield": 0, "style": set(), "align": set(), "bestfit": set(),
             "size": set(), "spacing_nonzero": 0, "leftover": []}
    for b in blocks:
        m = re.match(r"(--- !u!114 &-?\d+\n)", b)
        if not m:
            out.append(b)
            continue
        anchor = m.group(1).rstrip("\n")
        body = b[len(m.group(1)):]
        gm = re.search(r"m_Script: \{fileID: 11500000, guid: ([0-9a-f]+)", body)
        guid = gm.group(1) if gm else None
        if guid in (TMP_TEXT_UGUI, TMP_TEXT_WORLD):
            items = parse_top_level(body)
            out.append(build_text_block(anchor, items, stats))
            stats["text"] += 1
        elif guid == TMP_INPUTFIELD:
            items = parse_top_level(body)
            out.append(build_inputfield_block(anchor, items, stats))
        elif guid == TMP_DROPDOWN:
            stats["leftover"].append("TMP_Dropdown")
            out.append(b)
        else:
            out.append(b)
    new_txt = "".join(out)
    changed = new_txt != txt
    if changed and APPLY:
        open(full, "w", encoding="utf-8", newline="").write(new_txt)
    return stats, changed


total = {"text": 0, "inputfield": 0, "style": set(), "align": set(), "bestfit": set(),
         "size": set(), "spacing_nonzero": 0, "leftover": []}
for p in PREFABS:
    stats, changed = process(p)
    if stats["text"] or stats["inputfield"] or stats["leftover"]:
        print("%-60s TMP文本=%d 输入框=%d 残留=%s" % (p, stats["text"], stats["inputfield"], stats["leftover"]))
    for k in ("style", "align", "bestfit", "size"):
        total[k] |= stats[k]
    total["text"] += stats["text"]
    total["inputfield"] += stats["inputfield"]
    total["spacing_nonzero"] += stats["spacing_nonzero"]
    total["leftover"] += stats["leftover"]

print()
print("合计: TMP文本=%d, 输入框=%d, 残留=%s" % (total["text"], total["inputfield"], total["leftover"]))
print("映射后的 FontStyle 取值:", sorted(total["style"]))
print("映射后的 Alignment 取值:", sorted(total["align"]))
print("BestFit 取值:", sorted(total["bestfit"]))
print("字号范围:", min(total["size"]), "~", max(total["size"]))
print("原 TMP 带字距(characterSpacing!=0)的组件数:", total["spacing_nonzero"])
print("模式:", "APPLY" if APPLY else "DRY-RUN")
