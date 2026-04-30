---
name: metech-task-manager
description: >
  Use when managing tasks in Metech Work Manager via API.
  Triggers: "tạo task", "thêm task", "create task", "update task", "cập nhật task",
  "chuyển status", "bắt đầu task", "start task", "done task", "hoàn thành task",
  "thêm checklist", "add checklist", "xem task", "list tasks", "task của tôi", "my tasks",
  "task manager", "quản lý task", "metech task",
  "việc của tôi", "công việc", "tôi cần làm gì", "danh sách việc", "what should I do",
  "cần làm gì", "assign cho tôi", "giao việc", "tiến độ", "progress",
  "kanban", "board", "sprint", "backlog", "todo", "to do", "to-do",
  "wm", "work manager", "metech wm".
---

# Metech Task Manager

> **This skill is for AI agents (Claude Code), not humans.**
> Manage tasks in Metech Work Manager directly via REST API.
> **ALWAYS ask for user confirmation before any write action (create, update, delete, status change).**
> Read actions (list, view) can be executed without confirmation.

---

## 1. Environment Check (Run First)

Before any API call, load env vars from `.env.metech-wm` in the project root:

```bash
_f=".env.metech-wm"; _d="$PWD"; while [ "$_d" != "/" ]; do [ -f "$_d/$_f" ] && export $(grep -v '^#' "$_d/$_f" | xargs) && break; _d="$(dirname "$_d")"; done; echo "URL: ${METECH_WM_URL:-NOT SET}" && echo "TOKEN: ${METECH_WM_TOKEN:+SET}" && echo "PROJECT: ${METECH_WM_PROJECT_ID:-NOT SET (optional)}"
```

**If NOT SET:** Tell the user:
> "Cần tạo file `.env.metech-wm` tại root project:
> ```
> METECH_WM_URL=https://wm-dev.metech.vn
> METECH_WM_TOKEN=eyJhbG...
> METECH_WM_PROJECT_ID=uuid
> ```
> File này đã có trong `.gitignore`, token sẽ không bị commit."

**Token retrieval:** Login at the web UI → open browser DevTools → Console → `localStorage.getItem('token')` → copy the value.

---

## 2. API Base Pattern

All API calls use this pattern:

```bash
curl -s -X METHOD "$METECH_WM_URL/api/ENDPOINT" \
  -H "Authorization: Bearer $METECH_WM_TOKEN" \
  -H "Content-Type: application/json" \
  -d 'JSON_BODY'
```

For GET requests, omit `-d`. Parse response as JSON inline.

---

## 3. Read Actions (No Confirmation Needed)

### List my tasks
```bash
curl -s "$METECH_WM_URL/api/tasks/my" \
  -H "Authorization: Bearer $METECH_WM_TOKEN"
```
Returns: `{ overdue: Task[], today: Task[], assigned: Task[] }`

### List my kanban
```bash
curl -s "$METECH_WM_URL/api/tasks/my/kanban" \
  -H "Authorization: Bearer $METECH_WM_TOKEN"
```
Returns: Tasks grouped by status columns.

### My stats
```bash
curl -s "$METECH_WM_URL/api/tasks/my/stats" \
  -H "Authorization: Bearer $METECH_WM_TOKEN"
```

### List project tasks
```bash
curl -s "$METECH_WM_URL/api/projects/PROJECT_ID/tasks" \
  -H "Authorization: Bearer $METECH_WM_TOKEN"
```
Use `$METECH_WM_PROJECT_ID` as default, or ask user which project.

### View task detail
```bash
curl -s "$METECH_WM_URL/api/tasks/TASK_ID" \
  -H "Authorization: Bearer $METECH_WM_TOKEN"
```

### View task activities
```bash
curl -s "$METECH_WM_URL/api/tasks/TASK_ID/activities" \
  -H "Authorization: Bearer $METECH_WM_TOKEN"
```

### View checklists
```bash
curl -s "$METECH_WM_URL/api/tasks/TASK_ID/checklists" \
  -H "Authorization: Bearer $METECH_WM_TOKEN"
```

---

## 4. Write Actions (MUST Confirm Before Executing)

**RULE:** Before every write action, describe what you will do and ask "OK?". Only proceed after user confirms.

### Create task

**Confirm format:**
> "Mình sẽ tạo task mới:
> - **Title:** [title]
> - **Project:** [project name/ID]
> - **Priority:** [low|medium|high|urgent]
> - **Description:** [if any]
> - **Due date:** [if any]
> - **Estimated time:** [if any]
> OK?"

```bash
curl -s -X POST "$METECH_WM_URL/api/projects/PROJECT_ID/tasks" \
  -H "Authorization: Bearer $METECH_WM_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Task title",
    "description": "Optional description",
    "priority": "medium",
    "dueDate": "2026-03-30",
    "estimatedMinutes": 60
  }'
```

**Required fields:** `title` only. Others are optional.
**Priority values:** `low`, `medium` (default), `high`, `urgent`

After creating, offer: "Task đã tạo. Chuyển sang In Progress luôn không?"

### Update task
```bash
curl -s -X PATCH "$METECH_WM_URL/api/tasks/TASK_ID" \
  -H "Authorization: Bearer $METECH_WM_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Updated title",
    "description": "Updated description",
    "priority": "high",
    "dueDate": "2026-04-01",
    "estimatedMinutes": 120
  }'
```
All fields optional — only send what changed.

### Change status

**Status transitions (enforced by API):**
```
todo → in_progress, blocked
in_progress → in_review, blocked
blocked → in_progress
in_review → done, in_progress, blocked
done → (no transitions allowed)
```

```bash
curl -s -X PATCH "$METECH_WM_URL/api/tasks/TASK_ID/status" \
  -H "Authorization: Bearer $METECH_WM_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"status": "in_progress"}'
```

### Block task (requires reason)
```bash
curl -s -X POST "$METECH_WM_URL/api/tasks/TASK_ID/block" \
  -H "Authorization: Bearer $METECH_WM_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Waiting for API from backend team"}'
```

### Unblock task
```bash
curl -s -X POST "$METECH_WM_URL/api/tasks/TASK_ID/unblock" \
  -H "Authorization: Bearer $METECH_WM_TOKEN"
```

### Add checklist item
```bash
curl -s -X POST "$METECH_WM_URL/api/tasks/TASK_ID/checklists" \
  -H "Authorization: Bearer $METECH_WM_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title": "Checklist item text"}'
```

To add multiple checklist items, call this endpoint once per item.

### Toggle checklist item
```bash
curl -s -X PATCH "$METECH_WM_URL/api/checklists/CHECKLIST_ID" \
  -H "Authorization: Bearer $METECH_WM_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"isCompleted": true}'
```

### Delete checklist item
```bash
curl -s -X DELETE "$METECH_WM_URL/api/checklists/CHECKLIST_ID" \
  -H "Authorization: Bearer $METECH_WM_TOKEN"
```

### Add comment
```bash
curl -s -X POST "$METECH_WM_URL/api/tasks/TASK_ID/comments" \
  -H "Authorization: Bearer $METECH_WM_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content": "Comment text here", "mentionedUserIds": []}'
```
`mentionedUserIds` is optional — array of user UUIDs to notify.

### Start timer
```bash
curl -s -X POST "$METECH_WM_URL/api/tasks/TASK_ID/time/start" \
  -H "Authorization: Bearer $METECH_WM_TOKEN"
```

### Stop timer
```bash
curl -s -X POST "$METECH_WM_URL/api/tasks/TASK_ID/time/stop" \
  -H "Authorization: Bearer $METECH_WM_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"description": "What was done"}'
```

### Log manual time entry
```bash
curl -s -X POST "$METECH_WM_URL/api/tasks/TASK_ID/time" \
  -H "Authorization: Bearer $METECH_WM_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"duration": 3600, "description": "Implemented feature X", "date": "2026-03-26"}'
```
Duration is in seconds. Description and date are optional.

---

## 5. Error Handling

| HTTP Status | Response |
|-------------|----------|
| 200/201 | Success — report result to user |
| 400 | Show validation error from response `message` field |
| 401 | "Token hết hạn hoặc không hợp lệ. Lấy token mới: login web UI → DevTools Console → `localStorage.getItem('token')`" |
| 403 | "Không có quyền thực hiện action này. Kiểm tra role hoặc project membership." |
| 404 | "Task/Project không tồn tại. Kiểm tra lại ID." |
| Network/curl error | "Không kết nối được server. Kiểm tra `METECH_WM_URL` ($METECH_WM_URL) và network." |

---

## 6. Workflow Examples

### Example 1: Dev starts working on a feature
```
User: "tạo task implement user profile page"
Claude: "Mình sẽ tạo task mới:
  - Title: Implement user profile page
  - Project: [default project]
  - Priority: medium
  OK?"
User: "ok, priority high"
Claude: [creates task with priority high]
Claude: "Task #42 đã tạo. Thêm checklist cho task này không?"
User: "ok, thêm: design layout, fetch user data, add edit form"
Claude: [confirms checklist items, then creates 3 checklist items]
Claude: "Done. Chuyển task sang In Progress?"
```

### Example 2: Dev finishes and commits
```
User: "done task 42"
Claude: "Mình sẽ chuyển task #42 sang Done. OK?"
User: "chuyển in_review trước"
Claude: [changes status to in_review]
```

### Example 3: Check tasks before starting work
```
User: "task của tôi"
Claude: [calls GET /api/tasks/my, displays summary]
```
