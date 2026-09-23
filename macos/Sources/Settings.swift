import Foundation
import ScreenSaver

public class Settings {
    public static let moduleIdentifier = "com.fliqloclock.macos.saver"

    private struct Keys {
        static let is24Hour = "Is24Hour"
        static let showAmPm = "ShowAmPm"
        static let showSeconds = "ShowSeconds"
        static let clockScale = "ClockScale"
    }

    public var is24Hour: Bool = true
    public var showAmPm: Bool = false
    public var showSeconds: Bool = true
    public var clockScale: CGFloat = 1.0

    private var defaults: ScreenSaverDefaults? {
        return ScreenSaverDefaults(forModuleWithName: Settings.moduleIdentifier)
    }

    public init() {
        load()
    }

    public func load() {
        guard let defs = defaults else { return }

        defs.register(defaults: [
            Keys.is24Hour: true,
            Keys.showAmPm: false,
            Keys.showSeconds: true,
            Keys.clockScale: 1.0
        ])

        if defs.object(forKey: Keys.is24Hour) != nil {
            is24Hour = defs.bool(forKey: Keys.is24Hour)
        }
        if defs.object(forKey: Keys.showAmPm) != nil {
            showAmPm = defs.bool(forKey: Keys.showAmPm)
        }
        if defs.object(forKey: Keys.showSeconds) != nil {
            showSeconds = defs.bool(forKey: Keys.showSeconds)
        }
        if defs.object(forKey: Keys.clockScale) != nil {
            let scale = CGFloat(defs.double(forKey: Keys.clockScale))
            if scale >= 0.5 && scale <= 2.0 {
                clockScale = scale
            }
        }
    }

    public func save() {
        guard let defs = defaults else { return }
        defs.set(is24Hour, forKey: Keys.is24Hour)
        defs.set(showAmPm, forKey: Keys.showAmPm)
        defs.set(showSeconds, forKey: Keys.showSeconds)
        defs.set(Double(clockScale), forKey: Keys.clockScale)
        defs.synchronize()
    }
}
