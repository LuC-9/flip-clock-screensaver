# macOS Screensaver Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add native macOS screensaver support (`FliqloClock.saver`) with complete feature, visual, and mechanical parity to the existing Windows version.

**Architecture:** A native AppKit screensaver bundle built with Apple's `ScreenSaver.framework` (`ScreenSaverView`). The rendering engine replicates the 3D perspective folding flap math, dynamic lighting, drop shadows, and vector typography in Swift. Settings are persisted via `ScreenSaverDefaults`, and a native configuration sheet allows customizing formats and scaling.

**Tech Stack:** Swift 5.5+, AppKit, CoreGraphics, CoreText, `ScreenSaver.framework`, Xcode PBX, GitHub Actions CI.

## Global Constraints
- Target OS: macOS 11.0+ (Big Sur through Sequoia) on Apple Silicon (`arm64`) and Intel (`x86_64`).
- Bundle Identifier: `com.fliqloclock.macos.saver`.
- Principal Class: `FliqloClockView`.
- Root Windows files must remain intact without breaking existing Windows build scripts.
- No external runtime dependencies or third-party package managers.

---

### Task 1: Screensaver Bundle Metadata & Configuration (`Info.plist` & `Settings.swift`)

**Files:**
- Create: `macos/Info.plist`
- Create: `macos/Sources/Settings.swift`

**Interfaces:**
- Produces:
  - `Settings`: Struct with `is24Hour: Bool`, `showAmPm: Bool`, `showSeconds: Bool`, `clockScale: Double`.
  - `Settings.load() -> Settings`
  - `Settings.save(settings: Settings)`

- [ ] **Step 1: Create `macos/Info.plist`**
Define bundle metadata declaring `NSPrincipalClass` as `FliqloClockView` and bundle package type `BNDL`.

- [ ] **Step 2: Create `macos/Sources/Settings.swift`**
Implement settings persistence using `ScreenSaverDefaults(forModuleWithName: "com.fliqloclock.macos.saver")`. Register default values (`is24Hour: true`, `showAmPm: true`, `showSeconds: true`, `clockScale: 1.0`).

- [ ] **Step 3: Commit**
```bash
git add -f macos/Info.plist macos/Sources/Settings.swift
git commit -m "feat(macos): add bundle metadata and ScreenSaverDefaults settings model"
```

---

### Task 2: Flip Card Engine & 3D Perspective Math (`FlipCardView.swift`)

**Files:**
- Create: `macos/Sources/FlipCardView.swift`

**Interfaces:**
- Consumes: `Settings`
- Produces: `FlipCardView: NSView`:
  - `init(frame: NSRect, isSecondsCard: Bool)`
  - `setValue(newValue: String, animated: Bool)`
  - `tick(dt: TimeInterval) -> Bool` (returns true if animating and redraw required)
  - `draw(_ dirtyRect: NSRect)`

- [ ] **Step 1: Implement card geometry and drawing layers**
Draw dark rounded background card (`#141414`), split seam groove, bevel highlight, side hinge cutouts, and vertically bisected vector typography using CoreText / `NSFont.monospacedDigitSystemFont`.

- [ ] **Step 2: Implement 3D folding perspective math & dynamic shading**
Implement flip rotation ($0^\circ \to 180^\circ$ over 500ms ease-in-out curve). Calculate perspective trapezoid projection matching `FlipCard.cs`. Render dynamic darkening gradient on upper falling flap and drop shadow cast onto lower resting flap.

- [ ] **Step 3: Commit**
```bash
git add -f macos/Sources/FlipCardView.swift
git commit -m "feat(macos): implement split-flap 3D perspective rendering and animation engine"
```

---

### Task 3: Screensaver View Lifecycle & Layout Engine (`FliqloClockView.swift`)

**Files:**
- Create: `macos/Sources/FliqloClockView.swift`

**Interfaces:**
- Consumes: `FlipCardView`, `Settings`, `SettingsViewController`
- Produces: `FliqloClockView: ScreenSaverView`:
  - `init?(frame: NSRect, isPreview: Bool)`
  - `animateOneFrame()`
  - `hasConfigureSheet: Bool { get }`
  - `configureSheet: NSWindow? { get }`

- [ ] **Step 1: Implement `ScreenSaverView` initialization and display timer**
Subclass `ScreenSaverView`, set `animationTimeInterval = 1.0 / 60.0`, configure layer backing, and initialize Hours, Minutes, and Seconds `FlipCardView` subviews.

- [ ] **Step 2: Implement responsive centering layout engine**
Calculate proportional card dimensions based on `bounds` and `settings.clockScale`. Center the group horizontally and vertically. Handle window resize and multi-monitor bounds.

- [ ] **Step 3: Implement time-check and flap trigger loop**
In `animateOneFrame()`, query `Calendar.current` and update Hours, Minutes, and Seconds cards when values change.

- [ ] **Step 4: Commit**
```bash
git add -f macos/Sources/FliqloClockView.swift
git commit -m "feat(macos): implement FliqloClockView screensaver lifecycle and layout engine"
```

---

### Task 4: Native AppKit Configuration Sheet (`SettingsViewController.swift`)

**Files:**
- Create: `macos/Sources/SettingsViewController.swift`

**Interfaces:**
- Consumes: `Settings`
- Produces: `SettingsWindowController: NSWindowController`:
  - `var window: NSWindow?`
  - Modal sheet with toggles for 24-Hour, AM/PM, Seconds, and Clock Scale slider.

- [ ] **Step 1: Construct AppKit settings window sheet programmatically**
Create `NSPanel` sheet containing checkboxes for 24-Hour mode, AM/PM indicator, Show Seconds, an `NSSlider` for Clock Scale (50% to 200%), and OK/Cancel buttons.

- [ ] **Step 2: Implement user interaction handlers & persistence**
Load current values from `Settings.load()`. Disable AM/PM checkbox when 24-Hour mode is selected. Save changes on OK, restore on Cancel, and close sheet via `NSApp.endSheet(window)`.

- [ ] **Step 3: Commit**
```bash
git add -f macos/Sources/SettingsViewController.swift
git commit -m "feat(macos): implement native AppKit configuration sheet"
```

---

### Task 5: Xcode Project Definition & Build Script (`project.pbxproj` & `build.sh`)

**Files:**
- Create: `macos/FliqloClock.xcodeproj/project.pbxproj`
- Create: `macos/build.sh`

**Interfaces:**
- Produces:
  - Universal binary build target for `FliqloClock.saver`.
  - `build.sh` script to compile and package `FliqloClock-macOS.saver.zip`.

- [ ] **Step 1: Create `macos/FliqloClock.xcodeproj/project.pbxproj`**
Configure Xcode project bundle target linking `AppKit.framework` and `ScreenSaver.framework`. Set deployment target to macOS 11.0, architectures `arm64` and `x86_64`, code signing identity `-` (ad-hoc).

- [ ] **Step 2: Create `macos/build.sh`**
Write a shell script executing `xcodebuild` with Release configuration, producing `build/FliqloClock.saver`, and archiving into `build/FliqloClock-macOS.saver.zip`. Make executable.

- [ ] **Step 3: Commit**
```bash
git add -f macos/FliqloClock.xcodeproj/project.pbxproj macos/build.sh
git commit -m "feat(macos): add Xcode project definition and command-line build script"
```

---

### Task 6: Unified Multi-Platform GitHub Actions CI (`.github/workflows/build.yml`)

**Files:**
- Create: `.github/workflows/build.yml`

**Interfaces:**
- Produces: Automated matrix workflow building both Windows (`.scr`) and macOS (`.saver.zip`) artifacts.

- [ ] **Step 1: Create `.github/workflows/build.yml`**
Define matrix build:
  - Windows job: runs on `windows-latest`, compiles `FliqloClock.scr` via MSBuild/`csc.exe`, uploads `FliqloClock-Windows.zip`.
  - macOS job: runs on `macos-latest`, compiles universal `FliqloClock.saver` via `xcodebuild`, uploads `FliqloClock-macOS.saver.zip`.
  - Release trigger: attaches artifacts to GitHub Releases on tag pushes.

- [ ] **Step 2: Commit**
```bash
git add .github/workflows/build.yml
git commit -m "ci: add multi-platform GitHub Actions build workflow for Windows and macOS"
```

---

### Task 7: Documentation & Multi-Platform README Updates (`README.md`)

**Files:**
- Modify: `README.md`

**Interfaces:**
- Produces: Updated cross-platform documentation with badges, installation guides, and architecture reference.

- [ ] **Step 1: Update `README.md`**
Add macOS badges, describe macOS `.saver` installation steps (Double-click or copy to `~/Library/Screen Savers`), add macOS configuration instructions, and document building on Mac.

- [ ] **Step 2: Commit**
```bash
git add README.md
git commit -m "docs: update README with macOS installation, configuration, and build instructions"
```
