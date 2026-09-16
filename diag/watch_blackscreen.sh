#!/usr/bin/env bash
# ============================================================================
# watch_blackscreen.sh — theo dõi NGUYÊN NHÂN màn hình đen khi change device/proxy
#
# Cách dùng:
#   bash diag/watch_blackscreen.sh                # tự lấy device đầu tiên
#   bash diag/watch_blackscreen.sh 52004f8ffe4eb4b1   # chỉ định serial
#
# Nó làm gì:
#   Mỗi 2 giây chụp một "khung trạng thái" gồm 6 chỉ số sống còn (PID của
#   surfaceflinger / system_server / media.codec, số TaskRecord, mCurrentFocus,
#   tombstone mới) và ghi vào diag/logs/<serial>-<giờ>.log. Khi phát hiện
#   mCurrentFocus=null (MÀN ĐEN) nó tự động dump logcat + tombstone ĐẦY ĐỦ
#   ngay lúc đó để biết CÁI GÌ vừa crash.
#
#   Cứ để nó chạy NỀN trong lúc bạn chạy job ngắn (change device/proxy liên
#   tục). Khi phone bị đen, mở file log -> tìm dòng "!!!! BLACK SCREEN" ->
#   đọc phần dump bên dưới -> biết thủ phạm.
# ============================================================================
set -u

ADB="${ADB:-/c/QNHelper/sdk/platform-tools/adb.exe}"
[ -x "$ADB" ] || ADB="adb"

SERIAL="${1:-}"
if [ -z "$SERIAL" ]; then
  SERIAL=$("$ADB" devices | awk 'NR>1 && $2=="device"{print $1; exit}')
fi
if [ -z "$SERIAL" ]; then
  echo "KHÔNG tìm thấy device nào đang kết nối. Cắm phone + bật USB debugging rồi chạy lại."
  "$ADB" devices
  exit 1
fi

OUTDIR="$(cd "$(dirname "$0")" && pwd)/logs"
mkdir -p "$OUTDIR"
LOG="$OUTDIR/${SERIAL}-$(date +%Y%m%d-%H%M%S).log"

sh() { "$ADB" -s "$SERIAL" shell "$@" 2>/dev/null; }

echo "Đang theo dõi device: $SERIAL"
echo "Log ghi vào:          $LOG"
echo "Nhấn Ctrl+C để dừng."
echo ""

# Ghi baseline ban đầu
{
  echo "==== START $(date '+%F %T') serial=$SERIAL ===="
  echo "model:        $(sh getprop ro.product.model | tr -d '\r')"
  echo "android:      $(sh getprop ro.build.version.release | tr -d '\r')"
  echo "ram:          $(sh cat /proc/meminfo | grep MemTotal | tr -d '\r')"
  echo ""
} | tee -a "$LOG"

# Trạng thái trước đó để phát hiện PID ĐỔI (=进程 vừa crash + respawn)
prev_sf=""; prev_ss=""; prev_mc=""; prev_tomb=""

while true; do
  ts=$(date '+%F %T')

  # 1) PID các tiến trình sống còn
  sf=$(sh pidof surfaceflinger | tr -d '\r')
  ss=$(sh pidof system_server | tr -d '\r')
  mc=$(sh pidof media.codec media.swcodec | tr -d '\r')

  # 2) Số TaskRecord (rò rỉ = DẠNG 2). Máy khỏe 6-9, đen đo được 1460.
  tasks=$(sh "dumpsys activity activities 2>/dev/null | grep -c TaskRecord" | tr -d '\r')

  # 3) mCurrentFocus (null = không cửa sổ nào vẽ được = MÀN ĐEN)
  focus=$(sh "dumpsys window 2>/dev/null | grep mCurrentFocus" | tr -d '\r')

  # 4) tombstone mới nhất (bằng chứng crash native: SIGSEGV surfaceflinger...)
  tomb=$(sh "ls -t /data/tombstones/ 2>/dev/null | head -1" | tr -d '\r')

  # Phát hiện sự kiện
  flags=""
  [ -n "$prev_sf" ] && [ "$sf" != "$prev_sf" ] && flags="$flags SF_RESTART($prev_sf->$sf)"
  [ -n "$prev_ss" ] && [ "$ss" != "$prev_ss" ] && flags="$flags SYSTEM_SERVER_RESTART($prev_ss->$ss)"
  [ -n "$prev_mc" ] && [ "$mc" != "$prev_mc" ] && flags="$flags ENCODER_CRASH($prev_mc->$mc)"
  [ -n "$prev_tomb" ] && [ "$tomb" != "$prev_tomb" ] && flags="$flags NEW_TOMBSTONE($tomb)"

  # Dòng trạng thái một khung
  line="$ts | SF=${sf:-DEAD} SS=${ss:-DEAD} ENC=${mc:-none} TaskRec=${tasks:-?} | $focus"
  [ -n "$flags" ] && line="$line  <<<$flags"

  echo "$line" | tee -a "$LOG"

  # ===== BLACK SCREEN TRIGGER =====
  case "$focus" in
    *"mCurrentFocus=Window"*) : ;;   # có cửa sổ thật = OK
    *)
      # focus null/rỗng = nghi ngờ đen. Dump đầy đủ NGAY lúc này.
      {
        echo ""
        echo "!!!!!!!! BLACK SCREEN @ $ts !!!!!!!!"
        echo "focus = '${focus:-<rỗng>}'  TaskRecord=$tasks"
        echo ""
        echo "----- dumpsys SurfaceFlinger (số layer) -----"
        sh "dumpsys SurfaceFlinger --list 2>/dev/null | head -40"
        echo ""
        echo "----- logcat 400 dòng, lọc crash signature -----"
        sh "logcat -d -t 400 2>/dev/null | grep -iE 'FATAL|SIGSEGV|tombstone|Failed to transact|OutOfResourcesException|InputChannel is not initialized|crashed too many times|surfaceflinger|died|killing' | tail -80"
        echo ""
        echo "----- dropbox crash gần nhất -----"
        sh "dumpsys dropbox --print 2>/dev/null | grep -iE 'crash|anr' | tail -30"
        echo ""
        echo "----- tombstone mới nhất (nếu có) -----"
        [ -n "$tomb" ] && sh "su -c 'cat /data/tombstones/$tomb' 2>/dev/null | head -40"
        echo "!!!!!!!! END DUMP @ $(date '+%T') !!!!!!!!"
        echo ""
      } | tee -a "$LOG"
      # Tránh dump lặp liên tục mỗi 2s: nghỉ 15s sau mỗi lần trigger
      sleep 15
      ;;
  esac

  prev_sf="$sf"; prev_ss="$ss"; prev_mc="$mc"; prev_tomb="$tomb"
  sleep 2
done
