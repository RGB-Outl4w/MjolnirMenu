"""Check the Russian table in src/MjolnirMenu/Core/L.cs against the menu code.

Fails when a format string's {holes} differ between English and Russian (string.Format would
throw mid-frame and blank the menu), or when a menu string has no Russian entry.
Run: python scripts/check-translations.py
"""
import re, sys, pathlib

root = pathlib.Path(__file__).resolve().parent.parent / "src" / "MjolnirMenu"
table = dict(re.findall(r'\["((?:[^"\\]|\\.)*)"\] = "((?:[^"\\]|\\.)*)"', (root / "Core/L.cs").read_text(encoding="utf-8")))

holes = lambda s: sorted(re.findall(r"\{\d+(?::[^}]*)?\}", s))
bad = [k for k, v in table.items() if holes(k) != holes(v)]

# Strings handed to the translating widgets / helpers. Names, numbers and symbols are allowed through untranslated.
used = set()
hint = re.compile(r'Ui\.Toggle\(ref [\w.]+, "(?:[^"\\]|\\.)*",\s*"((?:[^"\\]|\\.)*)"')
call = re.compile(r'(?:Ui\.(?:Header|Hint|Label|Button|Toggle|LabeledSlider)\((?:ref [\w.]+, )?|L\.[TF]\(|Notify\(|Name = )"((?:[^"\\]|\\.)*)"')
for f in root.rglob("*.cs"):
    if "obj" in f.parts or "bin" in f.parts: continue
    text = f.read_text(encoding="utf-8")
    used.update(call.findall(text))
    used.update(hint.findall(text))
skip = re.compile(r"^[^A-Za-z]*$|^(ESP|Hitscan|Default)$|^\{0\}: \{1\}$|^\S+/\S+$")
missing = sorted(s for s in used if s not in table and not skip.match(s) and not re.fullmatch(r"[×+\-−%\d. ]+", s))

for k in bad: print("placeholder mismatch:", k)
for s in missing: print("no Russian entry:", s)
print(f"{len(table)} entries, {len(bad)} mismatches, {len(missing)} missing")
sys.exit(1 if bad or missing else 0)
