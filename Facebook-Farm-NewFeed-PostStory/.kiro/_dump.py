import os
p=os.path.join(os.environ["TEMP"],"recon_MainService.cs")
lines=open(p,encoding="utf-8").read().split("\n")
for n in [126,132,135,142,703,816,837,845,864,945,949,964]:
    s=lines[n-1]
    safe="".join(c if ord(c)<128 else "." for c in s)
    print(n,"|",safe)
