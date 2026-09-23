# macOS Screensaver Support Design Specification

**Date:** 2026-09-23  
**Status:** Validated & Ready for Planning  
**Target:** macOS 11.0+ (Big Sur, Monterey, Ventura, Sonoma, Sequoia) on Apple Silicon (arm64) and Intel (x86_64)

---

## 1. Overview & Goals

Expand FliqloClock from a Windows-only screensaver to a first-class, cross-platform screensaver suite by providing a native macOS screensaver bundle (`FliqloClock.saver`). 

### Core Requirements
1. **True macOS Screensaver (`.saver` bundle):** Installs into `~/Library/Screen Savers/` or `/Library/Screen Savers/` and integrates directly into macOS **System Settings > Screen Saver**.
2. **Visual & Mechanical Parity:** 1:1 reproduction of the 3D perspective folding flap, dynamic lighting gradients, hinge notches, drop shadows, and razor-sharp typography from the Windows version (`FlipCard.cs`).
3. **Non-Invasive Architecture:** Windows root files remain untouched; all macOS code resides in a dedicated `macos/` directory.
4. **Universal Binary:** Single build supporting both Apple Silicon (`arm64`) and Intel (`x86_64`) Macs.
5. **Automated CI/CD:** GitHub Actions workflow building both Windows (`.scr`) and macOS (`.saver.zip`) binaries automatically on push/release.

---

## 2. Directory Structure

```text
FliqloClock/
├── (Existing Windows files: Program.cs, FlipCard.cs, ScreensaverForm.cs, build.ps1, etc.)
├── .github/
│   └── workflows/
│       └── build.yml                     # Unified multi-platform CI
└── macos/
    ├── FliqloClock.xcodeproj/
    │   └── project.pbxproj               # Complete Xcode project definition
    ├── Info.plist                        # Screensaver bundle metadata
    ├── build.sh                          # Command-line build & packaging script
    └── Sources/
        ├── FliqloClockView.swift         # ScreenSaverView subclass, animation loop & card layout
        ├── FlipCardView.swift            # 3D flap projection, lighting & vector text renderer
        ├── Settings.swift                # ScreenSaverDefaults persistence model
        └── SettingsViewController.swift   # AppKit native configuration sheet
```

---

## 3. Component Design & Implementation Details

### 3.1 `FliqloClockView.swift` (Main Screensaver Lifecycle)
* **Base Class:** `ScreenSaverView` from `ScreenSaver.framework`.
* **Initialization:**
  * Handles both `init?(frame: NSRect, isPreview: Bool)` constructors (fullscreen display and mini-preview in System Settings).
  * Sets `animationTimeInterval = 1.0 / 60.0` (60 FPS rendering).
  * Layer-backed (`wantsLayer = true`) for optimal GPU compositing.
* **Layout Engine:**
  * Observes bounds changes (supporting arbitrary display aspect ratios, Retina 4K/5K/XDR displays, and multi-monitor setups).
  * Calculates dimensions for Hours card, Minutes card, and optional Seconds card:
    * Standard card aspect ratio: $W:H \approx 1:1.05$ (e.g. $260 \times 280$ base).
    * Seconds card aspect ratio: compact retro style ($120 \times 130$ base) aligned to the bottom baseline.
    * Centers the combined card group horizontally and vertically within the active display bounds, applying user-configured `clockScale`.
* **Animation & Clock Tick:**
  * In `animateOneFrame()`, samples current time (`Date()` / `Calendar.current`).
  * Triggers flip card transitions when hours, minutes, or seconds increment.
  * Coordinates 60 FPS interpolated flap angle updates over a 500ms flip duration with a smooth sinusoidal / cosine easing curve.
* **Configuration Sheet Hooks:**
  * Implements `hasConfigureSheet = true`.
  * Returns `SettingsViewController().window` inside `configureSheet`.

### 3.2 `FlipCardView.swift` (Split-Flap 3D Physics & Drawing Engine)
* **Visual Layers:**
  1. **Background Rounded Rect:** Deep dark card body (`#1A1A1A` to `#141414`) with rounded corners ($r \approx 14\text{pt}$).
  2. **Upper Static Flap:** Shows the current or target top half of the digit.
  3. **Lower Static Flap:** Shows the current or target bottom half of the digit.
  4. **Active Folding Flap:** Rotates around the horizontal center axis from $0^\circ$ to $180^\circ$.
     * From $0^\circ \to 90^\circ$: Displays upper half of previous digit, darkens as it rotates downward away from ambient light.
     * From $90^\circ \to 180^\circ$: Displays lower half of incoming digit, casts drop shadow on the bottom card while lightening towards the resting angle.
* **Perspective Math:**
  * Applies trapezoidal polygon projection or CoreGraphics matrix mapping matching the GDI+ mathematical model in `FlipCard.cs`.
  * Depth focal length $D = 600\text{pt}$.
  * Upper flap perspective foreshortening:
    $$\text{scale} = \frac{D}{D + Y \cdot \sin(\theta)}$$
* **Hardware Detailing:**
  * **Hinge Notches:** Semicircular cutouts on left and right card edges along the split seam.
  * **Horizontal Split Groove:** Crisp 1-2px divider line (`#0A0A0A`) with a subtle 1px specular bevel highlight (`rgba(255,255,255,0.06)`).
  * **Vector Typography:** Rendered using Apple's system font (`.systemFont(ofSize:weight:design:)` or `Helvetica Neue Bold`). Digits are vertically centered across the split line so the top and bottom halves align with subpixel accuracy.
  * **AM/PM Indicator:** Positioned in the lower-left quadrant of the Hours card in 12-hour mode.

### 3.3 `Settings.swift` (Preferences Persistence)
* **Storage Backend:** `ScreenSaverDefaults(forModuleWithName: "com.fliqloclock.macos.saver")`.
  * Essential on macOS because modern screensavers run in a sandboxed auxiliary process (`legacyScreenSaver` / `ScreenSaverEngine`); standard `UserDefaults.standard` does not share state between the configuration sheet and the screensaver engine.
* **Fields & Defaults:**
  * `is24Hour: Bool = true`
  * `showAmPm: Bool = true`
  * `showSeconds: Bool = true`
  * `clockScale: Double = 1.0` (range: $0.5$ to $2.0$)

### 3.4 `SettingsViewController.swift` (Native Configuration Modal)
* **Host Window:** Lightweight modal sheet attached to the screensaver preferences panel.
* **UI Controls:**
  * Checkbox: **24-Hour Time Format**
  * Checkbox: **Show AM/PM (12-hour mode)**
  * Checkbox: **Show Seconds Card**
  * Slider & Label: **Clock Scale (50% – 200%)**
  * Buttons: **Save** & **Cancel**
* **Interactions:**
  * Disables AM/PM checkbox when 24-Hour mode is active.
  * Updates slider label dynamically during dragging.
  * Writes to `ScreenSaverDefaults` and closes sheet via `NSApp.endSheet(window)`.

---

## 4. Build, Packaging & CI/CD Pipeline

### 4.1 Local Build Script (`macos/build.sh`)
* Uses `xcodebuild` targeting both `arm64` and `x86_64`.
* Packages output into `build/FliqloClock.saver`.
* Compresses into `build/FliqloClock-macOS.saver.zip` ready for distribution.

### 4.2 GitHub Actions (`.github/workflows/build.yml`)
* **Windows Job (`windows-latest`):**
  * Compiles `FliqloClock.scr` using MSBuild / `csc.exe`.
  * Uploads `FliqloClock-Windows.zip`.
* **macOS Job (`macos-latest`):**
  * Compiles universal `FliqloClock.saver` via `xcodebuild`.
  * Codesigns with ad-hoc signature (`codesign --force --deep --sign -`).
  * Uploads `FliqloClock-macOS.saver.zip`.

---

## 5. Verification & Testing Strategy

1. **Syntax & Architecture Verification:**
   * Swift source compilation verification using `swiftc` syntax validation.
   * Xcode project PBX formatting validation.
2. **Screensaver Protocol Compatibility:**
   * Verify bundle structure: `Contents/MacOS/FliqloClock`, `Contents/Info.plist`.
   * Verify `Info.plist` key `NSPrincipalClass` matches `FliqloClockView`.
3. **Display & Scaling Validation:**
   * Verify responsiveness across standard (1080p), Retina (4K/5K), and ultrawide aspect ratios.
   * Verify multi-monitor behavior (separate `ScreenSaverView` instance initialized per screen).
4. **Preferences Persistence Validation:**
   * Verify settings changes in `SettingsViewController` persist via `ScreenSaverDefaults` and take effect on next render pass.
