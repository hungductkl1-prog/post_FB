#!/usr/bin/env bash
# ============================================================================
# catch_blackscreen.sh — BẮT QUẢ TANG màn hình đen trên 1 device khi chạy job.
#
#   bash diag/catch_blackscreen.sh <serial>
#
# Chiến lược:
#   • Mỗi ~2s probe 6 chỉ số sống còn trong MỘT lệnh adb (rẻ, ~0.5s):
#     SF pid / system_server pid / media.codec pid / số tombstone / TaskRecord /
#     mCurrentFocus.
#   • Ghi MỌI tick vào file log đầy đủ (để đọc lại chi tiết).
#   • CHỈ in ra STDOUT (-> monitor notify) khi có SỰ KIỆN:
#       ENCODER_CRASH  media.codec đổi PID (OMX codec vừa chết)
#       SF_RESTART     surfaceflinger đổi PID
#       SS_RESTART     system_server đổi PID  (= framework restart)
#       NEW_TOMBSTONE  xuất hiện tombstone mới (kèm process + abort message)
#       LEAK           TaskRecord vượt ngưỡng 80 (DẠNG 2)
#       BLACK SCREEN   mCurrentFocus mất >= 2 tick liên tiếp -> dump logcat+tombstone
#       RECOVERED      focus trở lại sau khi đen
#       ADB_ERROR      không đọc được device (kết nối chập chờn)
#     + heartbeat 60s/lần để biết script còn sống.
# ============================================================================
set -u
ADB="${ADB:-/c/QNHelper/sdk/platform-tools/adb.exe}"
[ -x "$ADB" ] || ADB="adb"
SERIAL="${1:?cần serial. VD: bash diag/catch_blackscreen.sh 5200f478e2e3540f}"

OUTDIR="$(cd "$(dirname "$0")" && pwd)/logs"; mkdir -p "$OUTDIR"
LOG="$OUTDIR/${SERIAL}-catch-$(date +%Y%m%d-%H%M%S).log"
TASK_LEAK=80
BLACK_STREAK_NEED=2     # ~4s focus null liên tiếp mới tuyên bố đen (debounce)

probe() {
  timeout 12 "$ADB" -s "$SERIAL" shell "su -c '
    echo \"SF=\$(pidof surfaceflinger)\"
    echo \"SS=\$(pidof system_server)\"
    echo \"ENC=\$(pidof media.codec media.swcodec)\"
    echo \"TOMB=\$(ls /data/tombstones/tombstone_* 2>/dev/null | wc -l)\"
    echo \"TASK=\$(dumpsys activity activities 2>/dev/null | grep -c TaskRecord)\"
    echo \"FOC=\$(dumpsys window 2>/dev/null | grep mCurrentFocus)\"
  '" 2>/dev/null | tr -d '\r'
}
fld() { echo "$1" | sed -n "s/^$2=//p" | head -1; }
log() { echo "$@" >> "$LOG"; }
emit() { echo "$@"; log ">>> EVENT $@"; }   # stdout (notify) + log

log "==== START $(date '+%F %T') serial=$SERIAL log=$LOG ===="
emit "[$SERIAL] theo dõi bắt đầu. log=$LOG"

prev_sf=""; prev_ss=""; prev_enc=""; prev_tomb=""; prev_task=""
black_streak=0; was_black=0; last_beat=0; adb_err=0

while true; do
  out="$(probe)"; ts=$(date '+%T')

  # ---- ADB chập chờn: probe rỗng ----
  if [ -z "$out" ]; then
    adb_err=$((adb_err+1))
    if [ "$adb_err" -eq 3 ]; then
      emit "[$SERIAL] $ts ADB_ERROR: không đọc được device 3 lần liên tiếp (kết nối chập chờn?)"
    fi
    log "$ts ADB_ERROR (rỗng, streak=$adb_err)"
    sleep 2; continue
  fi
  [ "$adb_err" -ge 3 ] && emit "[$SERIAL] $ts ADB_OK: đọc lại được device."
  adb_err=0

  sf=$(fld "$out" SF); ss=$(fld "$out" SS); enc=$(fld "$out" ENC)
  tomb=$(fld "$out" TOMB); task=$(fld "$out" TASK); foc=$(fld "$out" FOC)

  # ---- ghi log đầy đủ mỗi tick ----
  log "$ts SF=${sf:-?} SS=${ss:-?} ENC=${enc:-none} TOMB=${tomb:-?} TASK=${task:-?} FOC=${foc:-<null>}"

  # ---- phát hiện PID đổi ----
  if [ -n "$prev_enc" ] && [ "$enc" != "$prev_enc" ]; then
    emit "[$SERIAL] $ts ENCODER_CRASH: media.codec $prev_enc -> ${enc:-MẤT} (OMX codec vừa chết)"
  fi
  if [ -n "$prev_sf" ] && [ "$sf" != "$prev_sf" ]; then
    emit "[$SERIAL] $ts SF_RESTART: surfaceflinger $prev_sf -> ${sf:-MẤT}"
  fi
  if [ -n "$prev_ss" ] && [ "$ss" != "$prev_ss" ]; then
    emit "[$SERIAL] $ts SS_RESTART: system_server $prev_ss -> ${ss:-MẤT} (framework restart!)"
  fi
  if [ -n "$prev_tomb" ] && [ "${tomb:-0}" -gt "${prev_tomb:-0}" ] 2>/dev/null; then
    newest=$(timeout 8 "$ADB" -s "$SERIAL" shell "su -c 'f=\$(ls -t /data/tombstones/tombstone_* 2>/dev/null|head -1); grep -hE \">>>|signal|Abort message\" \$f 2>/dev/null | head -3'" 2>/dev/null | tr -d '\r')
    emit "[$SERIAL] $ts NEW_TOMBSTONE ($prev_tomb->$tomb): $newest"
  fi
  if [ -n "$task" ] && [ "$task" -gt "$TASK_LEAK" ] 2>/dev/null && { [ -z "$prev_task" ] || [ "$prev_task" -le "$TASK_LEAK" ] 2>/dev/null; }; then
    emit "[$SERIAL] $ts LEAK: TaskRecord=$task > $TASK_LEAK (DẠNG 2 — sắp cạn surface)"
  fi

  # ---- BLACK SCREEN (debounce) ----
  case "$foc" in
    *"mCurrentFocus=Window"*)
      black_streak=0
      if [ "$was_black" -eq 1 ]; then
        emit "[$SERIAL] $ts RECOVERED: focus trở lại = $foc"
        was_black=0
      fi
      ;;
    *)
      black_streak=$((black_streak+1))
      log "$ts BLACK? streak=$black_streak foc='${foc:-<null>}'"
      if [ "$black_streak" -ge "$BLACK_STREAK_NEED" ] && [ "$was_black" -eq 0 ]; then
        was_black=1
        emit "[$SERIAL] $ts !!!! BLACK SCREEN !!!! focus='${foc:-<null>}' TaskRecord=${task:-?} SF=${sf:-DEAD} SS=${ss:-DEAD} ENC=${enc:-none} — đang dump..."
        {
          echo ""; echo "########## BLACK DUMP @ $ts ##########"
          echo "focus='$foc' task=$task sf=$sf ss=$ss enc=$enc"
          echo "--- SurfaceFlinger --list (số layer) ---"
          timeout 10 "$ADB" -s "$SERIAL" shell "su -c 'dumpsys SurfaceFlinger --list 2>/dev/null | head -30'" 2>/dev/null | tr -d '\r'
          echo "--- logcat 600 dòng lọc crash ---"
          timeout 12 "$ADB" -s "$SERIAL" shell "su -c 'logcat -d -t 600 2>/dev/null | grep -iE \"FATAL|SIGSEGV|SIGABRT|Failed to transact|OutOfResources|InputChannel is not initialized|crashed too many times|surfaceflinger|died|EGL_BAD|tombstoned\" | tail -90'" 2>/dev/null | tr -d '\r'
          echo "--- tombstone MỚI NHẤT (header) ---"
          timeout 10 "$ADB" -s "$SERIAL" shell "su -c 'f=\$(ls -t /data/tombstones/tombstone_* 2>/dev/null|head -1); echo \$f; head -25 \$f 2>/dev/null'" 2>/dev/null | tr -d '\r'
          echo "########## END DUMP @ $(date '+%T') ##########"; echo ""
        } >> "$LOG"
        emit "[$SERIAL] $ts đã dump xong chi tiết vào log. đọc: $LOG"
      fi
      ;;
  esac

  # ---- heartbeat 60s ----
  now=$(date +%s)
  if [ $((now - last_beat)) -ge 60 ]; then
    emit "[$SERIAL] $ts heartbeat: SF=$sf SS=$ss ENC=$enc TASK=$task TOMB=$tomb focus=${foc:0:30}"
    last_beat=$now
  fi

  prev_sf="$sf"; prev_ss="$ss"; prev_enc="$enc"; prev_tomb="$tomb"; prev_task="$task"
  sleep 2
done
