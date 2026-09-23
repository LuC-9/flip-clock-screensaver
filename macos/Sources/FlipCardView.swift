import Cocoa
import CoreGraphics
import CoreText

public class FlipCardView: NSView {
    public override var isFlipped: Bool { return true }

    private var currentValue: String = ""
    private var targetValue: String = ""
    private var progress: CGFloat = 1.0 // 1.0 = resting / no animation
    private var amPmText: String? = nil

    private var currentImage: CGImage?
    private var targetImage: CGImage?

    private var cachedCurrentVal: String?
    private var cachedTargetVal: String?
    private var cachedAmPm: String?
    private var cachedWidth: CGFloat = 0
    private var cachedHeight: CGFloat = 0

    public var isSecondsCard: Bool = false

    public init(frame: NSRect, isSecondsCard: Bool = false) {
        self.isSecondsCard = isSecondsCard
        super.init(frame: frame)
        self.wantsLayer = true
        self.layerContentsRedrawPolicy = .onSetNeedsDisplay
    }

    public required init?(coder: NSCoder) {
        super.init(coder: coder)
        self.wantsLayer = true
    }

    public func setValues(current: String, target: String, progress: CGFloat, amPmText: String? = nil) {
        let needsRedraw = (self.currentValue != current || self.targetValue != target || self.progress != progress || self.amPmText != amPmText)
        self.currentValue = current
        self.targetValue = target
        self.progress = progress
        self.amPmText = amPmText
        if needsRedraw {
            self.needsDisplay = true
        }
    }

    public override func draw(_ dirtyRect: NSRect) {
        guard let context = NSGraphicsContext.current?.cgContext else { return }
        let width = bounds.width
        let height = bounds.height
        if width <= 0 || height <= 0 { return }

        updateCache(width: width, height: height, amPmText: amPmText)

        let radius = max(2.0, width * 0.065)
        let halfH = floor(height / 2.0)

        let cardRect = CGRect(x: 0, y: 0, width: width, height: height)
        let rectTop = CGRect(x: 0, y: 0, width: width, height: halfH)
        let rectBottom = CGRect(x: 0, y: halfH, width: width, height: height - halfH)

        context.saveGState()

        // Clip all rendering to rounded card contour
        let cardPath = CGPath(roundedRect: cardRect, cornerWidth: radius, cornerHeight: radius, transform: nil)
        context.addPath(cardPath)
        context.clip()

        if progress >= 1.0 || currentValue == targetValue {
            // 1. Static resting card
            if let targetImg = targetImage {
                drawSubImage(image: targetImg, in: cardRect, from: cardRect, context: context)
            } else if let currentImg = currentImage {
                drawSubImage(image: currentImg, in: cardRect, from: cardRect, context: context)
            }
        } else {
            // 2. Active 3D Flip animation
            // Static top half reveals the target digit
            if let targetImg = targetImage {
                drawSubImage(image: targetImg, in: rectTop, from: rectTop, context: context)
            }

            // Static bottom half shows current digit waiting to be covered
            if let currentImg = currentImage {
                drawSubImage(image: currentImg, in: rectBottom, from: rectBottom, context: context)
            }

            let angleRad = Double(progress) * Double.pi
            let cosVal = CGFloat(cos(angleRad))

            if progress < 0.5 {
                // First half of flip: Old top flap folds downward toward horizontal center line
                let flapHeight = halfH * cosVal
                if flapHeight > 0.5 {
                    let rectFlip = CGRect(x: 0, y: halfH - flapHeight, width: width, height: flapHeight)
                    if let currentImg = currentImage {
                        drawSubImage(image: currentImg, in: rectFlip, from: rectTop, context: context)
                    }

                    // Darkening shadow over folding flap as it rotates away from light
                    let flapAlpha = min(0.70, max(0.0, Double(progress * 2.0 * 0.70)))
                    if flapAlpha > 0 {
                        drawVerticalGradient(in: rectFlip,
                                             topColor: CGColor(red: 0, green: 0, blue: 0, alpha: CGFloat(flapAlpha)),
                                             bottomColor: CGColor(red: 0, green: 0, blue: 0, alpha: CGFloat(flapAlpha * 0.2)),
                                             context: context)
                    }
                }

                // Cast drop shadow onto static bottom card
                let shadowDepth = halfH * 0.60
                let dropAlpha = min(0.60, max(0.0, Double((1.0 - cosVal) * 0.50)))
                if dropAlpha > 0 {
                    let dropRect = CGRect(x: 0, y: halfH, width: width, height: shadowDepth)
                    drawVerticalGradient(in: dropRect,
                                         topColor: CGColor(red: 0, green: 0, blue: 0, alpha: CGFloat(dropAlpha)),
                                         bottomColor: CGColor(red: 0, green: 0, blue: 0, alpha: 0.0),
                                         context: context)
                }
            } else {
                // Second half of flip: New bottom flap unfolds downward from horizontal center line
                let flapHeight = halfH * (-cosVal)
                if flapHeight > 0.5 {
                    let rectFlip = CGRect(x: 0, y: halfH, width: width, height: flapHeight)
                    if let targetImg = targetImage {
                        drawSubImage(image: targetImg, in: rectFlip, from: rectBottom, context: context)
                    }

                    // Flap unfolds from shadow to full light
                    let flapAlpha = min(0.70, max(0.0, Double((1.0 - progress) * 2.0 * 0.70)))
                    if flapAlpha > 0 {
                        drawVerticalGradient(in: rectFlip,
                                             topColor: CGColor(red: 0, green: 0, blue: 0, alpha: CGFloat(flapAlpha * 0.25)),
                                             bottomColor: CGColor(red: 0, green: 0, blue: 0, alpha: CGFloat(flapAlpha)),
                                             context: context)
                    }
                }

                // Top card subtle shadow near hinge
                let shadowDepth = halfH * 0.40
                let topAlpha = min(0.50, max(0.0, Double((-cosVal) * 0.40)))
                if topAlpha > 0 {
                    let topShadowRect = CGRect(x: 0, y: halfH - shadowDepth, width: width, height: shadowDepth)
                    drawVerticalGradient(in: topShadowRect,
                                         topColor: CGColor(red: 0, green: 0, blue: 0, alpha: 0.0),
                                         bottomColor: CGColor(red: 0, green: 0, blue: 0, alpha: CGFloat(topAlpha)),
                                         context: context)
                }
            }
        }

        // 3. Fliqlo mechanical details:
        // A. Horizontal split groove line
        let dividerY = halfH
        context.setStrokeColor(CGColor(red: 10/255.0, green: 11/255.0, blue: 13/255.0, alpha: 1.0))
        context.setLineWidth(2.0)
        context.beginPath()
        context.move(to: CGPoint(x: 0, y: dividerY))
        context.addLine(to: CGPoint(x: width, y: dividerY))
        context.strokePath()

        // B. Bevel highlight just below the split line
        context.setStrokeColor(CGColor(red: 52/255.0, green: 55/255.0, blue: 62/255.0, alpha: 1.0))
        context.setLineWidth(1.0)
        context.beginPath()
        context.move(to: CGPoint(x: 2, y: dividerY + 1.0))
        context.addLine(to: CGPoint(x: width - 2, y: dividerY + 1.0))
        context.strokePath()

        // C. Physical hinge notches on left and right borders where flaps rotate on axle
        let notchW = max(3.0, width * 0.024)
        let notchH = max(5.0, height * 0.038)
        let notchY = dividerY - notchH / 2.0

        context.setFillColor(CGColor(red: 12/255.0, green: 12/255.0, blue: 14/255.0, alpha: 1.0))
        context.fill(CGRect(x: 0, y: notchY, width: notchW, height: notchH))
        context.fill(CGRect(x: width - notchW, y: notchY, width: notchW, height: notchH))

        context.restoreGState()
    }

    private func updateCache(width: CGFloat, height: CGFloat, amPmText: String?) {
        let sizeChanged = (width != cachedWidth || height != cachedHeight)
        let amPmChanged = (amPmText != cachedAmPm)

        if sizeChanged || amPmChanged {
            cachedWidth = width
            cachedHeight = height
            cachedAmPm = amPmText
            cachedCurrentVal = nil
            cachedTargetVal = nil
            currentImage = nil
            targetImage = nil
        }

        if currentImage == nil || cachedCurrentVal != currentValue {
            currentImage = !currentValue.isEmpty ? createCardBitmap(value: currentValue, width: width, height: height, amPmText: amPmText) : nil
            cachedCurrentVal = currentValue
        }

        if targetImage == nil || cachedTargetVal != targetValue {
            targetImage = !targetValue.isEmpty ? createCardBitmap(value: targetValue, width: width, height: height, amPmText: amPmText) : nil
            cachedTargetVal = targetValue
        }
    }

    private func createCardBitmap(value: String, width: CGFloat, height: CGFloat, amPmText: String?) -> CGImage? {
        let colorSpace = CGColorSpaceCreateDeviceRGB()
        let bitmapInfo = CGBitmapInfo(rawValue: CGImageAlphaInfo.premultipliedLast.rawValue)

        let w = Int(width)
        let h = Int(height)
        guard w > 0, h > 0 else { return nil }

        guard let ctx = CGContext(data: nil,
                                  width: w,
                                  height: h,
                                  bitsPerComponent: 8,
                                  bytesPerRow: w * 4,
                                  space: colorSpace,
                                  bitmapInfo: bitmapInfo.rawValue) else {
            return nil
        }

        // Flipped context so (0,0) is top-left
        ctx.translateBy(x: 0, y: CGFloat(h))
        ctx.scaleBy(x: 1.0, y: -1.0)

        let cardRect = CGRect(x: 0, y: 0, width: width, height: height)
        let radius = max(2.0, width * 0.065)

        // 1. Draw card background with subtle vertical gradient for physical depth
        let path = CGPath(roundedRect: cardRect, cornerWidth: radius, cornerHeight: radius, transform: nil)
        ctx.addPath(path)
        ctx.clip()

        drawVerticalGradient(in: cardRect,
                             topColor: CGColor(red: 34/255.0, green: 36/255.0, blue: 40/255.0, alpha: 1.0),
                             bottomColor: CGColor(red: 24/255.0, green: 25/255.0, blue: 28/255.0, alpha: 1.0),
                             context: ctx)

        // 2. Draw digits text with exact vertical bisection
        if !value.isEmpty {
            let targetFontSize = height * 0.60
            let font = NSFont.monospacedDigitSystemFont(ofSize: targetFontSize, weight: .bold)

            let attributes: [NSAttributedString.Key: Any] = [
                .font: font,
                .foregroundColor: NSColor(calibratedRed: 236/255.0, green: 239/255.0, blue: 244/255.0, alpha: 1.0)
            ]

            let attrString = NSAttributedString(string: value, attributes: attributes)
            let line = CTLineCreateWithAttributedString(attrString)

            var ascent: CGFloat = 0
            var descent: CGFloat = 0
            var leading: CGFloat = 0
            let lineWidth = CGFloat(CTLineGetTypographicBounds(line, &ascent, &descent, &leading))
            let textHeight = ascent + descent

            let maxAllowedWidth = width * 0.84
            var scale: CGFloat = 1.0
            if lineWidth > maxAllowedWidth && lineWidth > 0 {
                scale = maxAllowedWidth / lineWidth
            }

            // In our flipped context, targetCenterY is height / 2.0
            // Baseline position:
            let targetCenterY = height / 2.0
            let dx = (width - (lineWidth * scale)) / 2.0
            let dy = targetCenterY + ((ascent - descent) * scale) / 2.0

            ctx.saveGState()
            ctx.translateBy(x: dx, y: dy)
            if scale != 1.0 {
                ctx.scaleBy(x: scale, y: scale)
            }
            // CoreText draws with Y-up, so flip Y for the text run
            ctx.scaleBy(x: 1.0, y: -1.0)
            ctx.textPosition = CGPoint.zero
            CTLineDraw(line, ctx)
            ctx.restoreGState()
        }

        // 3. Draw AM/PM indicator in bottom-left corner of the card if present
        if let amPm = amPmText, !amPm.isEmpty {
            let amPmSize = height * 0.088
            let amPmFont = NSFont.systemFont(ofSize: amPmSize, weight: .bold)
            let amPmAttrs: [NSAttributedString.Key: Any] = [
                .font: amPmFont,
                .foregroundColor: NSColor(calibratedRed: 145/255.0, green: 149/255.0, blue: 158/255.0, alpha: 1.0)
            ]
            let amPmStr = NSAttributedString(string: amPm, attributes: amPmAttrs)
            let amPmLine = CTLineCreateWithAttributedString(amPmStr)

            var amAscent: CGFloat = 0
            var amDescent: CGFloat = 0
            var amLeading: CGFloat = 0
            _ = CTLineGetTypographicBounds(amPmLine, &amAscent, &amDescent, &amLeading)

            let xPos = width * 0.085
            let yPos = height * 0.81 + amAscent

            ctx.saveGState()
            ctx.translateBy(x: xPos, y: yPos)
            ctx.scaleBy(x: 1.0, y: -1.0)
            ctx.textPosition = CGPoint.zero
            CTLineDraw(amPmLine, ctx)
            ctx.restoreGState()
        }

        return ctx.makeImage()
    }

    private func drawSubImage(image: CGImage, in destRect: CGRect, from srcRect: CGRect, context: CGContext) {
        guard let subImg = image.cropping(to: srcRect) else { return }
        context.saveGState()
        // CoreGraphics draws images upright when context is flipped
        context.translateBy(x: destRect.minX, y: destRect.maxY)
        context.scaleBy(x: 1.0, y: -1.0)
        context.draw(subImg, in: CGRect(x: 0, y: 0, width: destRect.width, height: destRect.height))
        context.restoreGState()
    }

    private func drawVerticalGradient(in rect: CGRect, topColor: CGColor, bottomColor: CGColor, context: CGContext) {
        let colorSpace = CGColorSpaceCreateDeviceRGB()
        let colors = [topColor, bottomColor] as CFArray
        guard let gradient = CGGradient(colorsSpace: colorSpace, colors: colors, locations: [0.0, 1.0]) else { return }
        context.saveGState()
        context.clip(to: rect)
        context.drawLinearGradient(gradient,
                                   start: CGPoint(x: rect.midX, y: rect.minY),
                                   end: CGPoint(x: rect.midX, y: rect.maxY),
                                   options: [])
        context.restoreGState()
    }
}
