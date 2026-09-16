import sys, subprocess, unicodedata, difflib, io, os
sys.stdout.reconfigure(encoding="utf-8")

BASE = r"E:\LamToolAutoPhonePrime"

def head_blob(rel):
    out = subprocess.run(["git","-C",BASE,"cat-file","blob","HEAD:"+rel],
                         capture_output=True)
    return out.stdout.decode("utf-8", errors="replace")

def read_work(rel):
    with open(os.path.join(BASE, rel.replace("/","\\")), "rb") as f:
        return f.read().decode("utf-8", errors="replace")

VN_MAP = {ord('đ'):'d', ord('Đ'):'D'}
def norm(s):
    s = s.translate(VN_MAP)
    s = unicodedata.normalize("NFKD", s)
    s = "".join(c for c in s if not unicodedata.combining(c))
    s = s.replace("?","").replace("\uFFFD","").lower()
    return "".join(c for c in s if c.isalnum())

def corrupted(s):
    return ("\uFFFD" in s) or ("?" in s)

def has_nonascii(s):
    return any(ord(c) > 0x7F for c in s)

def reconstruct(rel):
    head = head_blob(rel).split("\n")
    work = read_work(rel).split("\n")
    hk = [norm(l) for l in head]
    wk = [norm(l) for l in work]
    sm = difflib.SequenceMatcher(None, hk, wk, autojunk=False)
    out = []
    restored = 0
    residual = []  # (work_line_index_in_output, text)
    for tag, i1, i2, j1, j2 in sm.get_opcodes():
        if tag == "equal":
            # same skeleton -> restore HEAD (correct Vietnamese), keep alignment
            for k in range(i2 - i1):
                hl = head[i1 + k]
                wl = work[j1 + k]
                if hl != wl and has_nonascii(hl):
                    restored += 1
                    out.append(hl)
                else:
                    out.append(wl)
        else:
            # replace / insert / delete
            hblock = head[i1:i2]
            wblock = work[j1:j2]
            # try to pair each work line with best head line by fuzzy ratio
            used = set()
            for wi, wl in enumerate(wblock):
                if corrupted(wl):
                    best_r, best_h = 0.0, None
                    for hi, hl in enumerate(hblock):
                        if hi in used or not has_nonascii(hl):
                            continue
                        r = difflib.SequenceMatcher(None, norm(wl), norm(hl)).ratio()
                        if r > best_r:
                            best_r, best_h, best_hi = r, hl, hi
                    if best_h is not None and best_r >= 0.6:
                        used.add(best_hi)
                        restored += 1
                        out.append(best_h)
                    else:
                        residual.append((len(out), wl))
                        out.append(wl)
                else:
                    out.append(wl)
            # head-only lines in replace/delete are dropped (user changed/removed)
    return head, work, out, restored, residual

rel = "Sunny.Subd.Core/Services/MainService.cs"
head, work, out, restored, residual = reconstruct(rel)
print("HEAD lines:", len(head), "WORK lines:", len(work), "OUT lines:", len(out))
print("Restored Vietnamese lines:", restored)
print("Residual corrupted lines (new code, need manual):", len(residual))
out_text = "\n".join(out)
print("OUT FFFD:", out_text.count("\uFFFD"), " OUT '?':", out_text.count("?"))
# write dry-run output for inspection
with open(os.path.join(BASE, ".kiro_recon_MainService.cs") if False else os.path.join(os.environ["TEMP"],"recon_MainService.cs"), "w", encoding="utf-8") as f:
    f.write(out_text)
print("---- RESIDUAL (idx : ascii-safe) ----")
for idx, r in residual:
    safe = "".join(c if ord(c) < 128 else "." for c in r)
    print(idx, "|", safe)
