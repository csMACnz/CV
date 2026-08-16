# CV App — ADR 012: UI Design & Layout System Snapshot

**Date:** August 16, 2026
**Status:** Approved
**Target Baseline:** Refactoring live application at https://csmac.nz/CV/

---

## 1. Context & Purpose
This document serves as the authoritative visual design and UI layout snapshot for refactoring the WASM CV application. It synthesizes all settled design decisions, responsive layout rules, typography hierarchies, print export constraints, and scope boundaries.

---

## 2. Core Visual & Theme Architecture
* **Primary Default Theme:** Light Theme (#FAFAFA base canvas background, crisp dark #121212 typography) to maintain natural alignment with traditional printed paper CVs.
* **Theme Switching Engine:** CSS variable design token abstraction supporting runtime theme toggling.
* **Defined Secondary Themes:**
  * **Dark Mode:** Deep slate (#121212 background, #1F1F1F card containers, light slate text).
  * **Future Theme Extension Tokens:** Flexible token design supporting future retro/geek themes (e.g., Geocities, Matrix, VS2010).

---

## 3. Structural Layout & SPA Navigation
* **Page Model:** Single-Page Application (SPA) with a strict top-to-bottom vertical scrolling flow.
* **Eliminated Components:** No sidebar panels, fixed left/right navigation bars, or top hamburger menus.
* **Modals & Overlays:** Secondary detail views or sub-screens render via modal dialogs, which collapse into full-screen overlays on mobile viewports.
* **Primary Global Action:** "Print" button positioned in the header context to trigger PDF generation / browser print loop.

---

## 4. Header & Profile Section
* **Container Styling:** Top white section housing all core personal details and links.
* **Content Stack:** Name (Mark Clearwater), Title, Bio summary, and external link buttons.
* **Excluded Assets:** Strictly NO profile photo and NO prominent logo graphics.
* **Background Art:** Non-intrusive, subtle SVG line-art vector accents integrated into the background canvas.

---

## 5. Main Content Structure & Data Cards

### 5.1. Professional Timeline (Primary Section)
* **Ordering:** Strictly chronological, newest entries first to oldest.
* **Timeline Rail Flair:** A subtle vertical CSS left chronology rail (`2px solid #E0E0E0` in Light Mode, `#333333` in Dark Mode) with circular node indicators marking position/date entries. Purely CSS-driven without HTML layout disruption.
* **Card Container Styling:** Clean card panels with light grey 1px borders (#E5E7EB) and subtle rounded corners.

### 5.2. Projects Section (Secondary Section)
* **Positioning:** Placed vertically below the main Timeline section.
* **Responsive Column Layout:**
  * **Mobile Viewports (< 768px):** Single linear vertical column stack.
  * **Desktop Viewports (≥ 768px):** Two-column responsive grid layout.

### 5.3. Card Expansion Behavior
* **Placement:** Positioned in the top-right corner inside each individual Timeline and Project card.
* **Responsive Control Rendering:**
  * **Wide/Desktop Viewports:** Rendered as full text label (`Expand / Deep Dive`).
  * **Narrow/Mobile Viewports:** Collapses to a compact plus (`+`) icon.
* **Action:** Expands inline content narrative without triggering a modal or page navigation.

---

## 6. Print & PDF Generation Overrides (@media print)
* **Skill Badges:** Web view skill pills (#0976F3 blue badges) collapse into a clean, space-saving comma-separated text list under `@media print` rules.
* **Typography Scaling:** Web fluid `rem` font sizes switch to rigid `pt` values (`10pt` body text, `14pt` headings, `1.3` line-height) to strictly enforce 1- or 2-page A4 PDF output limits.
* **UI Stripping:** Strips interactive buttons, theme toggles, print triggers, and expand controls; forces all expandable narratives into full view if configured.

---

## 7. Explicitly Out of Scope (What We Won't Do)
* **No Side Navigation:** No persistent left/right drawer menus.
* **No Profile Photos:** No headshot image assets in the header.
* **No Heavy Graphic Logos:** Branding relies on clean typography rather than heavy graphic banners.
* **No Full Search UI (Descoped for Initial Release):** Interactive LINQ search bar descoped for initial visual refactor.

