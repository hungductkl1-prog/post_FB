---
name: winforms-uiux-designer
description: design, audit, and refactor ui/ux for desktop winforms applications using antdui. use when working with winforms ui, redesigning layouts, improving usability, fixing visual issues, or converting figma designs into winforms interfaces.
---

# 🎯 Core Purpose
Design, audit, and refactor UI/UX for **WinForms desktop applications** with:
- Modern UI (SSA style)
- Clear hierarchy
- High readability (desktop optimized)
- AntdUI-based components only

---

# 📥 Supported Inputs
This skill works with:

- UI screenshots
- Figma links / design references
- `.cs` / `.Designer.cs` code
- Text description of UI
- UX audit notes

---

# 📤 Outputs
Always produce a combination of:

1. **UI/UX Analysis**
2. **Redesign proposal (layout + visual)**
3. **Prompt for AI/dev**
4. **WinForms refactor guidance**
5. **Code patch (if applicable)**
6. **Checklist validation**

---

# ⚠️ Hard Rules (MANDATORY)

- WinForms only (no web/mobile patterns)
- Use **AntdUI 100%** for UI components
- DO NOT modify business logic
- Prefer **non-Designer patch**
- Reuse existing:
  - ColorPalette
  - FontScale
  - Spacing
  - GridStyleHelper
  - AntdHelper
- Maintain backward compatibility

---

# 🧩 Workflow

## 1. Analyze UI
Identify:
- Layout issues (dense, misaligned, cluttered)
- Typography problems (small, unclear, jagged)
- Color/contrast problems
- UX flow inefficiencies
- Component inconsistency

---

## 2. Redesign Structure

Always convert layout into:

```text
[Header]
[Toolbar / Actions]
[Main Content (Card/Table)]
[Footer / Status]