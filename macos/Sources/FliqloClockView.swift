import Cocoa
import ScreenSaver

public class FliqloClockView: ScreenSaverView {
    public override var isFlipped: Bool { return true }

    public let settings = Settings()

    private var hoursCard: FlipCardView!
    private var minutesCard: FlipCardView!
    private var secondsCard: FlipCardView!

    private var currentHourValue: String = ""
    private var targetHourValue: String = ""
    private var hourProgress: CGFloat = 1.0

    private var currentMinuteValue: String = ""
    private var targetMinuteValue: String = ""
    private var minuteProgress: CGFloat = 1.0

    private var currentSecondValue: String = ""
    private var targetSecondValue: String = ""
    private var secondProgress: CGFloat = 1.0

    private let flipDuration: Double = 0.50 // 500ms animation duration
    private var settingsSheetController: SettingsViewController?

    public override init?(frame: NSRect, isPreview: Bool) {
        super.init(frame: frame, isPreview: isPreview)
        commonInit()
    }

    public required init?(coder: NSCoder) {
        super.init(coder: coder)
        commonInit()
    }

    private func commonInit() {
        self.animationTimeInterval = 1.0 / 60.0
        self.wantsLayer = true
        self.layer?.backgroundColor = CGColor(red: 15/255.0, green: 16/255.0, blue: 18/255.0, alpha: 1.0)

        hoursCard = FlipCardView(frame: .zero, isSecondsCard: false)
        minutesCard = FlipCardView(frame: .zero, isSecondsCard: false)
        secondsCard = FlipCardView(frame: .zero, isSecondsCard: true)

        addSubview(hoursCard)
        addSubview(minutesCard)
        addSubview(secondsCard)

        settings.load()
        initializeTime()
        layoutCards()
    }

    public override func startAnimation() {
        super.startAnimation()
        settings.load()
        layoutCards()
    }

    public override func resizeSubviews(withOldSize oldSize: NSSize) {
        super.resizeSubviews(withOldSize: oldSize)
        layoutCards()
    }

    public func layoutCards() {
        let clientW = bounds.width
        let clientH = bounds.height
        guard clientW > 0, clientH > 0 else { return }

        let maxH = clientH * 0.56
        let widthFactor: CGFloat = settings.showSeconds ? 2.28 : 1.75
        let maxW = (clientW * 0.88) / widthFactor
        let baseScale = min(maxH, maxW)

        let cardHeight = max(8.0, baseScale * settings.clockScale)
        let cardWidth = max(8.0, cardHeight * 0.84)
        let gap = max(2.0, cardWidth * 0.075)

        var totalWidth: CGFloat = 0
        var secWidth: CGFloat = 0
        var secHeight: CGFloat = 0
        var secGap: CGFloat = 0

        if settings.showSeconds {
            secHeight = cardHeight * 0.58
            secWidth = cardWidth * 0.58
            secGap = max(2.0, cardWidth * 0.06)
            totalWidth = 2 * cardWidth + gap + secGap + secWidth
        } else {
            totalWidth = 2 * cardWidth + gap
        }

        let startX = floor((clientW - totalWidth) / 2.0)
        let startY = floor((clientH - cardHeight) / 2.0)

        hoursCard.frame = CGRect(x: startX, y: startY, width: cardWidth, height: cardHeight)
        minutesCard.frame = CGRect(x: startX + cardWidth + gap, y: startY, width: cardWidth, height: cardHeight)

        if settings.showSeconds {
            let secX = startX + 2 * cardWidth + gap + secGap
            let secY = startY + (cardHeight - secHeight)
            secondsCard.frame = CGRect(x: secX, y: secY, width: secWidth, height: secHeight)
            secondsCard.isHidden = false
        } else {
            secondsCard.isHidden = true
        }

        updateCardsDisplay()
    }

    private func initializeTime() {
        let now = Date()
        let calendar = Calendar.current
        var hour = calendar.component(.hour, from: now)
        let minute = calendar.component(.minute, from: now)
        let second = calendar.component(.second, from: now)

        if !settings.is24Hour {
            hour = hour % 12
            if hour == 0 { hour = 12 }
        }

        let hourStr = settings.is24Hour ? String(format: "%02d", hour) : "\(hour)"
        let minStr = String(format: "%02d", minute)
        let secStr = String(format: "%02d", second)

        currentHourValue = hourStr
        targetHourValue = hourStr
        hourProgress = 1.0

        currentMinuteValue = minStr
        targetMinuteValue = minStr
        minuteProgress = 1.0

        currentSecondValue = secStr
        targetSecondValue = secStr
        secondProgress = 1.0

        updateCardsDisplay()
    }

    public override func animateOneFrame() {
        let now = Date()
        let calendar = Calendar.current
        var hour = calendar.component(.hour, from: now)
        let minute = calendar.component(.minute, from: now)
        let second = calendar.component(.second, from: now)

        if !settings.is24Hour {
            hour = hour % 12
            if hour == 0 { hour = 12 }
        }

        let nextHourStr = settings.is24Hour ? String(format: "%02d", hour) : "\(hour)"
        let nextMinStr = String(format: "%02d", minute)
        let nextSecStr = String(format: "%02d", second)

        // Seconds rollover
        if nextSecStr != targetSecondValue {
            currentSecondValue = targetSecondValue
            targetSecondValue = nextSecStr
            secondProgress = 0.0
        }

        // Minutes rollover
        if nextMinStr != targetMinuteValue {
            currentMinuteValue = targetMinuteValue
            targetMinuteValue = nextMinStr
            minuteProgress = 0.0
        }

        // Hours rollover
        if nextHourStr != targetHourValue {
            currentHourValue = targetHourValue
            targetHourValue = nextHourStr
            hourProgress = 0.0
        }

        // Advance animation progress steps (60 FPS = ~0.0166s per frame)
        let step = CGFloat(1.0 / (60.0 * flipDuration))

        if secondProgress < 1.0 {
            secondProgress = min(1.0, secondProgress + step)
        }
        if minuteProgress < 1.0 {
            minuteProgress = min(1.0, minuteProgress + step)
        }
        if hourProgress < 1.0 {
            hourProgress = min(1.0, hourProgress + step)
        }

        updateCardsDisplay()
    }

    private func updateCardsDisplay() {
        var amPmText: String? = nil
        if !settings.is24Hour && settings.showAmPm {
            let hour24 = Calendar.current.component(.hour, from: Date())
            amPmText = hour24 >= 12 ? "PM" : "AM"
        }

        let easedHour = easeInOut(hourProgress)
        let easedMin = easeInOut(minuteProgress)
        let easedSec = easeInOut(secondProgress)

        hoursCard?.setValues(current: currentHourValue, target: targetHourValue, progress: easedHour, amPmText: amPmText)
        minutesCard?.setValues(current: currentMinuteValue, target: targetMinuteValue, progress: easedMin, amPmText: nil)
        if settings.showSeconds {
            secondsCard?.setValues(current: currentSecondValue, target: targetSecondValue, progress: easedSec, amPmText: nil)
        }
    }

    private func easeInOut(_ t: CGFloat) -> CGFloat {
        if t >= 1.0 { return 1.0 }
        if t <= 0.0 { return 0.0 }
        return CGFloat((1.0 - cos(Double(t) * Double.pi)) / 2.0)
    }

    public override func draw(_ dirtyRect: NSRect) {
        NSColor(calibratedRed: 15/255.0, green: 16/255.0, blue: 18/255.0, alpha: 1.0).setFill()
        dirtyRect.fill()
        super.draw(dirtyRect)
    }

    // MARK: - Configuration Sheet Support

    public override var hasConfigureSheet: Bool {
        return true
    }

    public override var configureSheet: NSWindow? {
        if settingsSheetController == nil {
            settingsSheetController = SettingsViewController(clockView: self)
        }
        return settingsSheetController?.window
    }
}
