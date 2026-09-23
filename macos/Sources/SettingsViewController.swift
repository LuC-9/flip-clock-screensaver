import Cocoa
import ScreenSaver

public class SettingsViewController: NSWindowController {
    private weak var clockView: FliqloClockView?

    private var chk24Hour: NSButton!
    private var chkShowAmPm: NSButton!
    private var chkShowSeconds: NSButton!
    private var scaleSlider: NSSlider!
    private var scaleLabel: NSTextField!

    private let settings = Settings()

    public init(clockView: FliqloClockView? = nil) {
        self.clockView = clockView

        let panel = NSPanel(
            contentRect: NSRect(x: 0, y: 0, width: 340, height: 260),
            styleMask: [.titled],
            backing: .buffered,
            defer: false
        )
        panel.title = "Fliqlo Flip Clock Settings"
        panel.isReleasedWhenClosed = false

        super.init(window: panel)

        setupUI(in: panel)
        loadSettingsIntoUI()
    }

    public required init?(coder: NSCoder) {
        super.init(coder: coder)
    }

    private func setupUI(in panel: NSPanel) {
        guard let contentView = panel.contentView else { return }

        let marginX: CGFloat = 30
        var currentY: CGFloat = 200

        // 1. 24-Hour Checkbox
        chk24Hour = NSButton(checkboxWithTitle: "24-Hour Time Format", target: self, action: #selector(didToggle24Hour(_:)))
        chk24Hour.frame = NSRect(x: marginX, y: currentY, width: 280, height: 22)
        contentView.addSubview(chk24Hour)

        // 2. AM/PM Checkbox
        currentY -= 30
        chkShowAmPm = NSButton(checkboxWithTitle: "Show AM/PM Label (12-Hour)", target: nil, action: nil)
        chkShowAmPm.frame = NSRect(x: marginX, y: currentY, width: 280, height: 22)
        contentView.addSubview(chkShowAmPm)

        // 3. Seconds Checkbox
        currentY -= 30
        chkShowSeconds = NSButton(checkboxWithTitle: "Show Seconds Card", target: nil, action: nil)
        chkShowSeconds.frame = NSRect(x: marginX, y: currentY, width: 280, height: 22)
        contentView.addSubview(chkShowSeconds)

        // 4. Scale Label
        currentY -= 34
        scaleLabel = NSTextField(labelWithString: "Clock Scale: 1.0x")
        scaleLabel.frame = NSRect(x: marginX, y: currentY, width: 280, height: 20)
        scaleLabel.font = NSFont.systemFont(ofSize: 12)
        contentView.addSubview(scaleLabel)

        // 5. Scale Slider
        currentY -= 26
        scaleSlider = NSSlider(value: 1.0, minValue: 0.5, maxValue: 1.5, target: self, action: #selector(didChangeScaleSlider(_:)))
        scaleSlider.frame = NSRect(x: marginX, y: currentY, width: 280, height: 24)
        scaleSlider.numberOfTickMarks = 11
        scaleSlider.allowsTickMarkValuesOnly = false
        contentView.addSubview(scaleSlider)

        // 6. Action Buttons (Cancel & Save)
        currentY -= 45
        let btnCancel = NSButton(title: "Cancel", target: self, action: #selector(didClickCancel(_:)))
        btnCancel.frame = NSRect(x: 140, y: currentY, width: 80, height: 32)
        btnCancel.keyEquivalent = "\u{1b}" // Escape
        contentView.addSubview(btnCancel)

        let btnSave = NSButton(title: "OK", target: self, action: #selector(didClickSave(_:)))
        btnSave.frame = NSRect(x: 230, y: currentY, width: 80, height: 32)
        btnSave.keyEquivalent = "\r" // Enter
        contentView.addSubview(btnSave)
    }

    private func loadSettingsIntoUI() {
        settings.load()
        chk24Hour.state = settings.is24Hour ? .on : .off
        chkShowAmPm.state = settings.showAmPm ? .on : .off
        chkShowSeconds.state = settings.showSeconds ? .on : .off
        scaleSlider.doubleValue = Double(settings.clockScale)
        scaleLabel.stringValue = String(format: "Clock Scale: %.1fx", settings.clockScale)
        chkShowAmPm.isEnabled = !settings.is24Hour
    }

    @objc private func didToggle24Hour(_ sender: NSButton) {
        chkShowAmPm.isEnabled = (sender.state != .on)
    }

    @objc private func didChangeScaleSlider(_ sender: NSSlider) {
        scaleLabel.stringValue = String(format: "Clock Scale: %.1fx", sender.doubleValue)
    }

    @objc private func didClickSave(_ sender: Any?) {
        settings.is24Hour = (chk24Hour.state == .on)
        settings.showAmPm = (chkShowAmPm.state == .on)
        settings.showSeconds = (chkShowSeconds.state == .on)
        settings.clockScale = CGFloat(scaleSlider.doubleValue)
        settings.save()

        if let view = clockView {
            view.settings.load()
            view.layoutCards()
        }

        closeSheet()
    }

    @objc private func didClickCancel(_ sender: Any?) {
        closeSheet()
    }

    private func closeSheet() {
        if let window = self.window, let parent = window.sheetParent {
            parent.endSheet(window)
        } else {
            self.window?.close()
        }
    }
}
