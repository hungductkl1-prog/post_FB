# AutoGoLike — Brand assets

Bộ logo + asset social chính thức của AutoGoLike. Dùng cho web, OG image, Telegram, Facebook, Zalo, email signature, in ấn.

## Cấu trúc

```
public/brand/
├── *.svg          ← logo gốc (mark, wordmark, full)
├── src/*.svg      ← SVG composed cho avatar/banner (sửa file ở đây rồi re-export PNG)
└── png/*.png      ← PNG export sẵn để dùng
```

## Logo SVG (transparent / brand color)

| File | Khi nào dùng |
| --- | --- |
| `logo-mark.svg` | Mark vuông (GL) — nền sáng. Match `PublicNav`. |
| `logo-mark-inverse.svg` | Mark vuông (GL) — nền tối. Match `PublicFooter`. |
| `logo-mark-mono-dark.svg` | Mark đơn sắc đen + chữ trắng. |
| `logo-mark-mono-light.svg` | Mark đơn sắc trắng + chữ đen. |
| `logo-full.svg` | Mark + wordmark "AutoGoLike / BY GOLIKE · OFFICIAL" — nền sáng. |
| `logo-full-inverse.svg` | Mark + wordmark — nền tối. |
| `logo-wordmark.svg` | Chỉ wordmark, không có mark. |

## PNG (sẵn sàng dùng)

Tất cả ở [`png/`](./png/):

### Avatar vuông — Telegram, FB profile, Zalo OA

| File | Kích thước | Dùng cho |
| --- | --- | --- |
| `avatar-lime-1024.png` | 1024 | Mặc định cho avatar. Telegram channel/group, Facebook page profile, Zalo OA. |
| `avatar-lime-512.png` | 512 | Telegram (min 512 cho channel photo). |
| `avatar-lime-256.png` | 256 | Avatar nhỏ, app icon nhẹ. |
| `avatar-ink-{1024,512,256}.png` | — | Variant nền đen — dùng nếu kênh có theme tối. |

### Mark trong suốt — overlay lên ảnh khác

| File | Khi nào dùng |
| --- | --- |
| `logo-mark-{1024,512}.png` | Mark đen-lime, nền trong suốt. Watermark trên ảnh sáng. |
| `logo-mark-inverse-{1024,512}.png` | Mark lime-đen, nền trong suốt. Watermark trên ảnh tối. |
| `logo-full-1280.png` | Mark + wordmark, nền trong suốt — header email, banner web. |
| `logo-full-inverse-1280.png` | Variant cho nền tối. |

### Banner / cover

| File | Kích thước | Dùng cho |
| --- | --- | --- |
| `og-1200x630.png` | 1200×630 | **Open Graph** — share lên Facebook / Zalo / Telegram preview. Tỷ lệ 1.91:1 chuẩn. |
| `og-1200x630-dark.png` | 1200×630 | OG variant nền tối. |
| `fb-cover-820x312.png` | 820×312 | **Facebook Page cover** (desktop). Mobile crop ở giữa nên nội dung quan trọng đã canh giữa. |
| `telegram-1280x720.png` | 1280×720 | **Telegram channel/group banner** (kéo dài header), hoặc dùng làm cover ảnh giới thiệu. |

## Re-export PNG

Sửa file SVG trong `src/` (hoặc logo gốc) → chạy:

```bash
./scripts/export-brand-png.sh
```

Script dùng `pnpm dlx sharp-cli` (không cần cài global). Lần đầu hơi lâu vì pull package.

## Design tokens

- **Ink:** `#0a0a0a`
- **Lime:** `#d3ff3c`
- **Paper:** `#ffffff`
- **Mark:** ô vuông bo `rx ≈ 14–16` (mark nhỏ) / `rx ≈ 44–120` (scale lên cho avatar/banner), **xoay -4°**, chữ "GL" canh giữa, font-weight 800.
- **Display font:** Space Grotesk → fallback Inter / system-ui.
- **Mono font:** JetBrains Mono (cho tagline `BY GOLIKE · OFFICIAL`).

## Lưu ý

- Mark **luôn xoay -4°** — đây là dấu hiệu nhận diện. Đừng dựng thẳng.
- Tagline `BY GOLIKE · OFFICIAL` viết HOA, letter-spacing rộng.
- PNG render bằng `sharp` (libvips) → KHÔNG load Space Grotesk từ Google Fonts. Fallback sang system font (macOS: Helvetica). Vẫn giữ vibe bold sans-serif. Nếu cần render đúng Space Grotesk:
  - Mở SVG trong Figma/Illustrator → export PNG.
  - Hoặc convert text sang outline path trong SVG rồi re-export.
- Cần kích thước khác (icon 192 cho favicon, banner Twitter 1500×500…)? Sửa `scripts/export-brand-png.sh` thêm dòng tương ứng.
