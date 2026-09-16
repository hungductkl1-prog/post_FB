"""Faithful port of FacebookHander.TryHandleMetaAdsConsentAsync (lines 682-791).
Uses adb shell + ATX HTTP dump (http://127.0.0.1:<fwd>/dump/hierarchy)."""
import re, json, time, subprocess, random, sys, os
from urllib.request import urlopen

SERIAL = "5200d988ee2c9483"
ATX_URL = "http://127.0.0.1:51126/dump/hierarchy"
OUT_DIR = r"H:\0.SRCTOOL\LamToolAutoPhonePrime"

def shell(*args, timeout=15):
    r = subprocess.run(["adb","-s",SERIAL,"shell"]+list(args),
                       capture_output=True, text=True, timeout=timeout)
    return r.returncode, (r.stdout or "") + (r.stderr or "")

def click(x, y):
    x, y = int(x), int(y)
    print(f"      adb input tap {x} {y}")
    rc, out = shell("input","tap", str(x), str(y))
    return rc == 0

def longclick(x, y, dur_ms):
    x, y = int(x), int(y)
    print(f"      adb input swipe {x} {y} {x} {y} {dur_ms}")
    rc, out = shell("input","swipe", str(x),str(y),str(x),str(y),str(dur_ms))
    return rc == 0

def swipe_up(repeat=1, duration=800, delay=500):
    W, H = 1440, 2560
    for i in range(repeat):
        x = int(W * (0.30 + random.random()*0.40))
        sy = int(H * (0.60 + random.random()*0.25))
        ey = int(H * (0.15 + random.random()*0.20))
        print(f"      adb input swipe {x} {sy} {x} {ey} {duration}")
        shell("input","swipe", str(x),str(sy),str(x),str(ey),str(duration))
        if i < repeat - 1: time.sleep(delay/1000)

def dump_xml():
    try:
        resp = urlopen(ATX_URL, timeout=15)
        return json.loads(resp.read()).get("result","")
    except Exception as e:
        print(f"    dump_xml fail: {e}"); return ""

def save_png(tag):
    p = os.path.join(OUT_DIR, f"sc_9483_{tag}.png")
    subprocess.run(["adb","-s",SERIAL,"exec-out","screencap","-p"],
                   stdout=open(p,"wb"), check=True)
    print(f"    saved {p}")

def parse_nodes(xml):
    nodes=[]; max_r=max_b=0
    for m in re.finditer(r'<node[^>]*?>', xml):
        s = m.group(0)
        ba = re.search(r'bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"', s)
        if not ba: continue
        l,t,r,b = map(int, ba.groups())
        cd = (re.search(r'content-desc="([^"]*)"', s).group(1).strip() if re.search(r'content-desc="([^"]*)"', s) else "")
        tx = (re.search(r'text="([^"]*)"', s).group(1).strip() if re.search(r'text="([^"]*)"', s) else "")
        cl = re.search(r'clickable="([^"]*)"', s)
        en = re.search(r'enabled="([^"]*)"', s)
        nodes.append(dict(l=l,t=t,r=r,b=b,cd=cd,tx=tx,
                          click=(cl.group(1) if cl else ""),
                          enable=(en.group(1) if en else "")))
        if r > max_r: max_r = r
        if b > max_b: max_b = b
    return nodes, max_r, max_b

def gate_hit(xml):
    needles = [
        "Want to subscribe or continue","Subscribe to use without ads",
        "Use free of charge with ads","Use for free with",
        "Continue with personalized ads","Switch to less-personalized ads",
        "You can manage your ad experience","Your current experience",
    ]
    for n in needles:
        if n in xml: return n
    for n in parse_nodes(xml)[0]:
        if n["cd"].strip() == "Agree" or n["tx"].strip() == "Agree":
            return "Agree"
    return None

def find_radio_circle(xml, targets):
    nodes, max_r, max_b = parse_nodes(xml)
    if not nodes: return None
    r_min = max(20, max_r*2//100); r_max = max(60, max_r*9//100)
    skew = max(12, r_max//5); row_tol = max(24, max_b*3//100)
    for tgt in targets:
        # title: smallest exact
        tbox = None; tArea = 1<<62
        for n in nodes:
            if n["cd"].lower()==tgt.lower() or n["tx"].lower()==tgt.lower():
                a = (n["r"]-n["l"])*(n["b"]-n["t"])
                if 0 < a < tArea: tArea=a; tbox=n
        if not tbox: continue
        tcy = (tbox["t"]+tbox["b"])//2
        # card: largest with tgt + "Radio button"
        cbox = None; cArea = 0
        for n in nodes:
            has = (tgt.lower() in n["cd"].lower() or tgt.lower() in n["tx"].lower())
            rad = ("Radio button" in n["cd"] or "Radio button" in n["tx"])
            if has and rad:
                a = (n["r"]-n["l"])*(n["b"]-n["t"])
                if a > cArea: cArea=a; cbox=n
        cb = cbox or tbox
        best = None; bArea = 0
        for n in nodes:
            if n["cd"] or n["tx"]: continue
            w, h = n["r"]-n["l"], n["b"]-n["t"]
            if w < r_min or w > r_max or h < r_min or h > r_max: continue
            if abs(w-h) > skew: continue
            cx, cy = (n["l"]+n["r"])//2, (n["t"]+n["b"])//2
            if abs(cy-tcy) > row_tol: continue
            if n["l"] < cb["l"] or n["r"] > cb["r"]: continue
            a = w*h
            if a > bArea: bArea=a; best=(cx,cy)
        if best: return best
    return None

def find_center(nodes, max_r, max_b, patterns, prefer_clickable=False):
    bestA=0; best=None; bestCl=False
    for needle, mode, src in patterns:
        for n in nodes:
            val = n["cd"] if src=="cd" else (n["tx"] if src=="tx" else "")
            if src=="any": match = (needle.lower()==n["cd"].lower() or needle.lower()==n["tx"].lower()) if mode=="exact" else (needle.lower() in n["cd"].lower() or needle.lower() in n["tx"].lower())
            else: match = (needle.lower()==val.lower()) if mode=="exact" else (needle.lower() in val.lower())
            if not match: continue
            a = (n["r"]-n["l"])*(n["b"]-n["t"]); cl = n["click"]=="true"
            if prefer_clickable:
                if not best: bestA,best,bestCl = a,n,cl
                elif cl and not bestCl: bestA,best,bestCl = a,n,cl
                elif cl==bestCl and a>bestA: bestA,best,bestCl = a,n,cl
            else:
                if a > bestA: bestA,best,bestCl = a,n,cl
    if best:
        return ((best["l"]+best["r"])//2, (best["t"]+best["b"])//2, bestCl, bestA)
    return None

EXACT = [("Continue","exact","cd"),("Agree","exact","cd"),("OK","exact","cd"),("OK","exact","tx")]
LOOSE = [("Continue","contains","cd"),("Continue","contains","tx"),
         ("Agree","contains","cd"),("Agree","contains","tx"),
         ("OK","contains","cd"),("OK","contains","tx")]
OK_EXP = [("OK","exact","cd"),("OK","exact","tx")]
FREE_OPT = [("Use for free with","contains","cd"),
            ("Use free of charge with ads","contains","cd"),
            ("Use for free with ads","contains","cd"),
            ("Use for free with","contains","tx"),
            ("Use free of charge with ads","contains","tx"),
            ("Use for free with ads","contains","tx")]

def is_exp(xml):
    return "Your current experience" in xml

def report(label, xml):
    g = gate_hit(xml)
    nodes,mr,mb = parse_nodes(xml)
    rc = find_radio_circle(xml, ["Use free of charge with ads","Use for free with ads"])
    exp = is_exp(xml)
    c = find_center(nodes,mr,mb, OK_EXP if exp else EXACT)
    if not c: c = find_center(nodes,mr,mb, LOOSE, prefer_clickable=True)
    if exp and not c: c = find_center(nodes,mr,mb, EXACT)
    def p(pt):
        if not pt: return "None"
        return f"({pt[0]},{pt[1]})" + (f" click={pt[2]} area={pt[3]}" if len(pt)>2 else "")
    print(f"  [{label}] gate={'YES:'+g if g else 'NO'} exp={exp} radio={p(rc)} btn={p(c)}")
    # dump summary: enabled state of footer
    for n in nodes:
        if n["b"]>=2180 and n["t"]<=2340 and (n["r"]-n["l"])>500:
            print(f"    footer node y[{n['t']}..{n['b']}] cls=? enable={n['enable']} click={n['click']} desc=\"{n['cd'][:30]}\"")

def run():
    print("="*70)
    print(f"TryHandleMetaAdsConsentAsync on {SERIAL} (port=51126)")
    print("="*70)
    xml = dump_xml()
    if not xml: print("FAIL empty"); return
    g = gate_hit(xml)
    if not g:
        print(f"FAIL: MetaAdsConsent gate MISS"); return
    print(f"PASS: gate hit '{g}'")
    report("BEFORE", xml)
    save_png("0_before")

    rc = find_radio_circle(xml, ["Use free of charge with ads","Use for free with ads"])
    tapped_radio_circle = False
    tapped_option_card = False
    if rc:
        print(f"\n[STEP1] Radio circle at {rc} -> Click")
        click(*rc)
        tapped_radio_circle = True
        time.sleep(0.7)
    else:
        print("\n[STEP1] Radio circle NOT FOUND -> LongClick card fallback")
        nodes,mr,mb = parse_nodes(xml)
        oc = find_center(nodes,mr,mb,FREE_OPT)
        if oc:
            longclick(oc[0], oc[1], 200)
            tapped_option_card = True
            time.sleep(0.7)
        else:
            print("  FAIL no card"); return

    if tapped_option_card:
        print("[STEP1.5] SwipeUp(2,500,350) for card variant")
        swipe_up(2, 500, 0.35)
        time.sleep(0.6)

    save_png("1_after_radio")
    xml = dump_xml()
    report("AFTER_RADIO", xml)

    for attempt in range(4):
        print(f"\n[ATTEMPT {attempt}]")
        time.sleep(0.2)
        xml = dump_xml()
        if not xml: time.sleep(0.5); continue
        report(f"a={attempt}", xml)
        if not gate_hit(xml):
            print("  gate MISS -> screen left -> SUCCESS")
            save_png(f"2_done_a{attempt}")
            break
        exp = is_exp(xml)
        nodes,mr,mb = parse_nodes(xml)
        c = find_center(nodes,mr,mb, OK_EXP if exp else EXACT)
        if not c: c = find_center(nodes,mr,mb, LOOSE, prefer_clickable=True)
        if exp and not c: c = find_center(nodes,mr,mb, EXACT)
        if c:
            if attempt == 0:
                print(f"  Click btn @ ({c[0]},{c[1]}) click={c[2]} area={c[3]}")
                click(c[0], c[1])
            else:
                print(f"  LongClick btn @ ({c[0]},{c[1]}) 180ms")
                longclick(c[0], c[1], 180)
        else:
            print("  no btn -> SwipeUp(1,500,350)")
            swipe_up(1, 500, 0.35)
        time.sleep(1.4)
        if not c and attempt < 3:
            swipe_up(1, 500, 0.35); time.sleep(0.5)
    else:
        save_png("2_after_loop")

    time.sleep(0.8)
    print("\n[FINAL]")
    xml = dump_xml()
    if xml:
        report("FINAL", xml)
        save_png("3_final")
        rc, out = shell("dumpsys","activity","top")
        for ln in out.splitlines():
            if "ACTIVITY com.facebook" in ln or "mResumedActivity" in ln:
                print("    "+ln.strip())

if __name__=="__main__":
    run()
