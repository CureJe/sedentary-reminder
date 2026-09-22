import AppKit
import QuartzCore

final class ReminderWindow: NSWindow {
    override var canBecomeKey: Bool { true }
    override var canBecomeMain: Bool { true }
}

final class ReminderWindowController: NSWindowController {
    weak var reminderDelegate: ReminderWindowControllerDelegate?

    private let character: CharacterKind
    private let popupSeconds: Int
    private let preview: Bool
    private let soundEnabled: Bool
    private var dismissTimer: Timer?
    private var sound: NSSound?
    private var hasDismissed = false

    init(character: CharacterKind, popupSeconds: Int, preview: Bool, soundEnabled: Bool) {
        self.character = character
        self.popupSeconds = popupSeconds
        self.preview = preview
        self.soundEnabled = soundEnabled

        let screen = Self.activeScreen()
        let visible = screen.visibleFrame
        let stageWidth = visible.width * 0.64
        let stageHeight = visible.height * 0.90
        let originX = character.anchorsRight ? visible.maxX - stageWidth : visible.minX
        let frame = NSRect(x: originX, y: visible.minY, width: stageWidth, height: stageHeight)

        let window = ReminderWindow(
            contentRect: frame,
            styleMask: [.borderless],
            backing: .buffered,
            defer: false,
            screen: screen
        )
        window.isOpaque = false
        window.backgroundColor = .clear
        window.hasShadow = false
        window.level = .floating
        window.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary, .stationary]
        window.isReleasedWhenClosed = false
        window.hidesOnDeactivate = false
        window.animationBehavior = .none

        super.init(window: window)

        let reminderView = ReminderView(character: character, preview: preview)
        reminderView.onDismiss = { [weak self] result in
            self?.dismiss(result: result)
        }
        window.contentView = reminderView
    }

    required init?(coder: NSCoder) {
        nil
    }

    deinit {
        dismissTimer?.invalidate()
        sound?.stop()
    }

    func show() {
        guard let window, let reminderView = window.contentView as? ReminderView else { return }
        NSApp.activate(ignoringOtherApps: true)
        window.orderFrontRegardless()
        window.makeKey()
        window.makeFirstResponder(reminderView)

        DispatchQueue.main.async { [weak self, weak reminderView] in
            guard let self, let reminderView else { return }
            reminderView.startEntranceAnimation()
            self.playSoundOnce()
        }

        if !preview {
            let timer = Timer(timeInterval: TimeInterval(popupSeconds), repeats: false) { [weak self] _ in
                self?.dismiss(result: .timedOut)
            }
            RunLoop.main.add(timer, forMode: .common)
            dismissTimer = timer
        }
    }

    func dismiss(result: ReminderResult) {
        guard !hasDismissed else { return }
        hasDismissed = true
        dismissTimer?.invalidate()
        dismissTimer = nil
        sound?.stop()
        sound = nil
        window?.orderOut(nil)
        close()
        reminderDelegate?.reminderWindow(self, didFinish: result, preview: preview)
    }

    private func playSoundOnce() {
        guard soundEnabled,
              let url = ResourceLocator.url(folder: "Audio", filename: character.soundFilename),
              let sound = NSSound(contentsOf: url, byReference: true) else { return }
        sound.loops = false
        self.sound = sound
        sound.play()
    }

    private static func activeScreen() -> NSScreen {
        let pointer = NSEvent.mouseLocation
        return NSScreen.screens.first(where: { NSMouseInRect(pointer, $0.frame, false) })
            ?? NSScreen.main
            ?? NSScreen.screens[0]
    }
}

final class ReminderView: NSView {
    var onDismiss: ((ReminderResult) -> Void)?

    private let character: CharacterKind
    private let preview: Bool
    private let sceneLayer = CALayer()
    private let characterLayer = CALayer()
    private let messageLayer = CALayer()
    private var accentLayers: [CALayer] = []
    private var sceneBuilt = false
    private var entranceStarted = false

    init(character: CharacterKind, preview: Bool) {
        self.character = character
        self.preview = preview
        super.init(frame: .zero)
        wantsLayer = true
        layer = sceneLayer
        sceneLayer.backgroundColor = NSColor.clear.cgColor
        toolTip = preview
            ? "Click the character to close preview"
            : "Click to dismiss; right-click to snooze for 5 minutes"
        setAccessibilityElement(true)
        setAccessibilityRole(.button)
        setAccessibilityLabel("\(character.displayName) reminds you to stand up and move")
    }

    required init?(coder: NSCoder) {
        nil
    }

    override var acceptsFirstResponder: Bool { true }
    override var isFlipped: Bool { false }

    override func layout() {
        super.layout()
        sceneLayer.frame = bounds
        guard !sceneBuilt, bounds.width > 0, bounds.height > 0 else { return }
        sceneBuilt = true
        buildScene()
    }

    override func mouseDown(with event: NSEvent) {
        onDismiss?(.completed)
    }

    override func rightMouseDown(with event: NSEvent) {
        onDismiss?(preview ? .completed : .snoozed)
    }

    override func keyDown(with event: NSEvent) {
        switch event.keyCode {
        case 36, 49, 53:
            onDismiss?(.completed)
        case 1 where !preview:
            onDismiss?(.snoozed)
        default:
            super.keyDown(with: event)
        }
    }

    func startEntranceAnimation() {
        if !sceneBuilt {
            layoutSubtreeIfNeeded()
        }
        guard sceneBuilt, !entranceStarted else { return }
        entranceStarted = true

        if NSWorkspace.shared.accessibilityDisplayShouldReduceMotion {
            fadeInWithoutMotion()
            return
        }

        switch character {
        case .orangeCat: animateCatClimb()
        case .corgi: animateCorgiRun()
        case .redPanda: animateRedPandaTumble()
        case .mechGuardian: animateMechLanding()
        case .webRanger: animateWebRangerLeap()
        }
    }

    private func buildScene() {
        guard let imageURL = ResourceLocator.url(folder: "Characters", filename: character.imageFilename),
              let image = NSImage(contentsOf: imageURL),
              let cgImage = image.cgImage(forProposedRect: nil, context: nil, hints: nil) else { return }

        let maximum = CGSize(width: bounds.width * 0.54, height: bounds.height * 0.78)
        let imageSize = aspectFit(source: CGSize(width: cgImage.width, height: cgImage.height), inside: maximum)
        let characterX: CGFloat
        if character == .webRanger {
            characterX = bounds.width * 0.025
        } else if character.anchorsRight {
            characterX = bounds.width - imageSize.width - bounds.width * 0.035
        } else {
            characterX = bounds.width * 0.035
        }
        let characterY = bounds.height * 0.035

        characterLayer.frame = CGRect(origin: CGPoint(x: characterX, y: characterY), size: imageSize)
        characterLayer.contents = cgImage
        characterLayer.contentsGravity = .resizeAspect
        characterLayer.contentsScale = window?.backingScaleFactor ?? 2
        characterLayer.shadowColor = NSColor.black.cgColor
        characterLayer.shadowOpacity = 0.28
        characterLayer.shadowRadius = 18
        characterLayer.shadowOffset = CGSize(width: 0, height: -7)
        sceneLayer.addSublayer(characterLayer)

        let cardSize = CGSize(width: min(390, bounds.width * 0.48), height: 132)
        let cardX = character == .webRanger
            ? bounds.width - cardSize.width - 28
            : min(bounds.width - cardSize.width - 24, characterX + imageSize.width * 0.70)
        let cardY = character == .webRanger
            ? min(bounds.height - cardSize.height - 26, bounds.height * 0.68)
            : min(bounds.height - cardSize.height - 26, bounds.height * 0.47)
        messageLayer.frame = CGRect(origin: CGPoint(x: cardX, y: cardY), size: cardSize)
        styleMessageLayer(messageLayer)
        addMessageText(to: messageLayer)
        sceneLayer.addSublayer(messageLayer)

        buildAccents(characterFrame: characterLayer.frame, messageFrame: messageLayer.frame)
    }

    private func aspectFit(source: CGSize, inside maximum: CGSize) -> CGSize {
        guard source.width > 0, source.height > 0 else { return maximum }
        let scale = min(maximum.width / source.width, maximum.height / source.height)
        return CGSize(width: source.width * scale, height: source.height * scale)
    }

    private func styleMessageLayer(_ card: CALayer) {
        card.cornerRadius = 24
        card.shadowColor = NSColor.black.cgColor
        card.shadowOpacity = 0.22
        card.shadowRadius = 16
        card.shadowOffset = CGSize(width: 0, height: -5)

        switch character {
        case .orangeCat:
            card.backgroundColor = NSColor(calibratedRed: 1.0, green: 0.96, blue: 0.88, alpha: 0.96).cgColor
            card.borderColor = NSColor(calibratedRed: 0.93, green: 0.55, blue: 0.18, alpha: 0.65).cgColor
            card.borderWidth = 2
        case .corgi:
            card.backgroundColor = NSColor(calibratedRed: 0.12, green: 0.15, blue: 0.18, alpha: 0.94).cgColor
            card.borderColor = NSColor(calibratedRed: 0.96, green: 0.67, blue: 0.24, alpha: 0.9).cgColor
            card.borderWidth = 2
        case .redPanda:
            card.backgroundColor = NSColor(calibratedRed: 0.30, green: 0.15, blue: 0.08, alpha: 0.94).cgColor
            card.borderColor = NSColor(calibratedRed: 0.86, green: 0.49, blue: 0.23, alpha: 0.95).cgColor
            card.borderWidth = 3
        case .mechGuardian:
            card.backgroundColor = NSColor(calibratedRed: 0.03, green: 0.14, blue: 0.19, alpha: 0.88).cgColor
            card.borderColor = NSColor(calibratedRed: 0.12, green: 0.88, blue: 1.0, alpha: 0.95).cgColor
            card.borderWidth = 2
        case .webRanger:
            card.backgroundColor = NSColor(calibratedWhite: 0.98, alpha: 0.97).cgColor
            card.borderColor = NSColor(calibratedRed: 0.10, green: 0.72, blue: 0.82, alpha: 0.90).cgColor
            card.borderWidth = 2
        }
    }

    private func addMessageText(to card: CALayer) {
        let message: (title: String, subtitle: String, lightText: Bool)
        switch character {
        case .orangeCat:
            message = ("Time for a stretch", "Take a walk and relax your shoulders", false)
        case .corgi:
            message = ("Every step counts", "Grab some water, then come back fresh", true)
        case .redPanda:
            message = ("Move and recharge", "Look into the distance and rest your eyes", true)
        case .mechGuardian:
            message = ("Stand for 2 minutes", "Movement mode activated", true)
        case .webRanger:
            message = ("Take a break", "Stretch and take a short walk", false)
        }

        let title = CATextLayer()
        title.frame = CGRect(x: 18, y: 66, width: card.bounds.width - 36, height: 42)
        title.contentsScale = window?.backingScaleFactor ?? 2
        title.font = NSFont.systemFont(ofSize: 25, weight: .bold)
        title.fontSize = 25
        title.alignmentMode = .center
        title.foregroundColor = (message.lightText ? NSColor.white : NSColor(calibratedWhite: 0.12, alpha: 1)).cgColor
        title.string = message.title
        card.addSublayer(title)

        let subtitle = CATextLayer()
        subtitle.frame = CGRect(x: 18, y: 28, width: card.bounds.width - 36, height: 28)
        subtitle.contentsScale = window?.backingScaleFactor ?? 2
        subtitle.font = NSFont.systemFont(ofSize: 14, weight: .medium)
        subtitle.fontSize = 14
        subtitle.alignmentMode = .center
        subtitle.foregroundColor = (message.lightText ? NSColor.white.withAlphaComponent(0.76) : NSColor(calibratedWhite: 0.28, alpha: 1)).cgColor
        subtitle.string = message.subtitle
        card.addSublayer(subtitle)
    }

    private func buildAccents(characterFrame: CGRect, messageFrame: CGRect) {
        switch character {
        case .orangeCat:
            let tail = CAShapeLayer()
            let path = CGMutablePath()
            path.move(to: CGPoint(x: messageFrame.minX + 32, y: messageFrame.minY + 8))
            path.addLine(to: CGPoint(x: messageFrame.minX - 24, y: messageFrame.minY - 18))
            path.addLine(to: CGPoint(x: messageFrame.minX + 72, y: messageFrame.minY + 2))
            path.closeSubpath()
            tail.path = path
            tail.fillColor = NSColor(calibratedRed: 1.0, green: 0.96, blue: 0.88, alpha: 0.96).cgColor
            sceneLayer.insertSublayer(tail, below: messageLayer)
            accentLayers.append(tail)
        case .corgi:
            for index in 0..<3 {
                let streak = CALayer()
                streak.backgroundColor = NSColor(calibratedRed: 0.96, green: 0.67, blue: 0.24, alpha: 0.72).cgColor
                streak.cornerRadius = 2
                streak.frame = CGRect(x: characterFrame.minX - CGFloat(54 + index * 18), y: characterFrame.midY + CGFloat(index * 22), width: CGFloat(58 + index * 14), height: 4)
                sceneLayer.insertSublayer(streak, below: characterLayer)
                accentLayers.append(streak)
            }
        case .redPanda:
            for index in 0..<3 {
                let leaf = CAShapeLayer()
                leaf.path = CGPath(ellipseIn: CGRect(x: 0, y: 0, width: 18, height: 9), transform: nil)
                leaf.fillColor = NSColor(calibratedRed: 0.35, green: 0.65, blue: 0.26, alpha: 0.85).cgColor
                leaf.frame = CGRect(x: messageFrame.maxX - CGFloat(35 + index * 26), y: messageFrame.maxY + CGFloat(index * 12), width: 18, height: 9)
                leaf.setAffineTransform(CGAffineTransform(rotationAngle: CGFloat(index - 1) * 0.48))
                sceneLayer.addSublayer(leaf)
                accentLayers.append(leaf)
            }
        case .mechGuardian:
            let ring = CAShapeLayer()
            ring.path = CGPath(ellipseIn: CGRect(x: 0, y: 0, width: messageFrame.width + 22, height: messageFrame.height + 18), transform: nil)
            ring.frame = messageFrame.insetBy(dx: -11, dy: -9)
            ring.fillColor = NSColor.clear.cgColor
            ring.strokeColor = NSColor(calibratedRed: 0.12, green: 0.88, blue: 1.0, alpha: 0.68).cgColor
            ring.lineWidth = 2
            sceneLayer.insertSublayer(ring, below: messageLayer)
            accentLayers.append(ring)
        case .webRanger:
            let tether = CAShapeLayer()
            let tetherPath = CGMutablePath()
            let launcher = CGPoint(
                x: characterFrame.minX + characterFrame.width * 0.78,
                y: characterFrame.minY + characterFrame.height * 0.77
            )
            tetherPath.move(to: launcher)
            tetherPath.addLine(to: CGPoint(x: messageFrame.midX, y: messageFrame.midY))
            tether.path = tetherPath
            tether.fillColor = NSColor.clear.cgColor
            tether.strokeColor = NSColor.white.withAlphaComponent(0.92).cgColor
            tether.lineWidth = 3
            tether.lineCap = .round
            tether.shadowColor = NSColor.black.cgColor
            tether.shadowOpacity = 0.24
            tether.shadowRadius = 4
            tether.strokeEnd = 1
            sceneLayer.insertSublayer(tether, below: messageLayer)
            accentLayers.append(tether)

            let web = makeWebLayer(frame: messageFrame.insetBy(dx: -72, dy: -72))
            sceneLayer.insertSublayer(web, below: messageLayer)
            accentLayers.append(web)
        }
    }

    private func makeWebLayer(frame: CGRect) -> CAShapeLayer {
        let web = CAShapeLayer()
        web.frame = frame
        let center = CGPoint(x: frame.width / 2, y: frame.height / 2)
        let radius = min(frame.width, frame.height) / 2
        let path = CGMutablePath()
        for spoke in 0..<12 {
            let angle = CGFloat(spoke) * .pi / 6
            path.move(to: center)
            path.addLine(to: CGPoint(x: center.x + cos(angle) * radius, y: center.y + sin(angle) * radius))
        }
        for ring in 1...4 {
            let inset = radius * CGFloat(ring) / 4
            path.addEllipse(in: CGRect(x: center.x - inset, y: center.y - inset, width: inset * 2, height: inset * 2))
        }
        web.path = path
        web.fillColor = NSColor.clear.cgColor
        web.strokeColor = NSColor.white.withAlphaComponent(0.84).cgColor
        web.lineWidth = 2
        web.shadowColor = NSColor.black.cgColor
        web.shadowOpacity = 0.25
        web.shadowRadius = 4
        web.strokeEnd = 1
        return web
    }

    private func fadeInWithoutMotion() {
        [characterLayer, messageLayer].forEach { layer in
            let fade = CABasicAnimation(keyPath: "opacity")
            fade.fromValue = 0
            fade.toValue = 1
            fade.duration = 0.18
            fade.timingFunction = CAMediaTimingFunction(name: .easeOut)
            layer.add(fade, forKey: "reduced-motion-fade")
        }
    }

    private func animateCatClimb() {
        let animation = CAKeyframeAnimation(keyPath: "transform")
        let transforms = [
            CATransform3DConcat(CATransform3DMakeTranslation(0, -bounds.height * 0.90, 0), CATransform3DMakeScale(0.94, 1.08, 1)),
            CATransform3DConcat(CATransform3DMakeTranslation(0, 22, 0), CATransform3DMakeScale(1.04, 0.97, 1)),
            CATransform3DMakeTranslation(0, -5, 0),
            CATransform3DIdentity
        ]
        animation.values = transforms.map { NSValue(caTransform3D: $0) }
        animation.keyTimes = [0, 0.70, 0.86, 1]
        animation.duration = 0.95
        animation.timingFunctions = springTimingFunctions()
        characterLayer.add(animation, forKey: "cat-climb")
        revealMessage(delay: 0.48, duration: 0.42)
    }

    private func animateCorgiRun() {
        let final = characterLayer.position
        let run = CAKeyframeAnimation(keyPath: "position")
        run.values = [
            NSValue(point: CGPoint(x: -characterLayer.bounds.width, y: final.y - 10)),
            NSValue(point: CGPoint(x: final.x - 34, y: final.y + 14)),
            NSValue(point: CGPoint(x: final.x + 12, y: final.y - 3)),
            NSValue(point: final)
        ]
        run.keyTimes = [0, 0.66, 0.84, 1]
        run.duration = 0.82
        run.timingFunctions = springTimingFunctions()
        characterLayer.add(run, forKey: "corgi-run")

        let squash = CAKeyframeAnimation(keyPath: "transform.scale")
        squash.values = [0.86, 1.08, 0.97, 1]
        squash.keyTimes = run.keyTimes
        squash.duration = run.duration
        characterLayer.add(squash, forKey: "corgi-squash")
        revealMessage(delay: 0.34, duration: 0.36)
        streakAccents(delay: 0)
    }

    private func animateRedPandaTumble() {
        let final = characterLayer.position
        let move = CAKeyframeAnimation(keyPath: "position")
        move.values = [
            NSValue(point: CGPoint(x: -characterLayer.bounds.width * 0.45, y: final.y - 100)),
            NSValue(point: CGPoint(x: final.x - 38, y: final.y + 30)),
            NSValue(point: final)
        ]
        move.keyTimes = [0, 0.78, 1]
        move.duration = 1.05
        move.timingFunctions = [
            CAMediaTimingFunction(controlPoints: 0.18, 0.72, 0.28, 1),
            CAMediaTimingFunction(controlPoints: 0.30, 0.84, 0.42, 1)
        ]
        characterLayer.add(move, forKey: "panda-roll-position")

        let spin = CAKeyframeAnimation(keyPath: "transform.rotation.z")
        spin.values = [-Double.pi * 1.55, 0.10, 0]
        spin.keyTimes = move.keyTimes
        spin.duration = move.duration
        characterLayer.add(spin, forKey: "panda-roll-spin")
        revealMessage(delay: 0.62, duration: 0.38)
        floatAccents(delay: 0.72)
    }

    private func animateMechLanding() {
        let final = characterLayer.position
        let landing = CAKeyframeAnimation(keyPath: "position")
        landing.values = [
            NSValue(point: CGPoint(x: final.x, y: bounds.height + characterLayer.bounds.height * 0.60)),
            NSValue(point: CGPoint(x: final.x, y: final.y - 18)),
            NSValue(point: CGPoint(x: final.x, y: final.y + 7)),
            NSValue(point: final)
        ]
        landing.keyTimes = [0, 0.72, 0.88, 1]
        landing.duration = 0.92
        landing.timingFunctions = springTimingFunctions()
        characterLayer.add(landing, forKey: "mech-landing")

        let glow = CABasicAnimation(keyPath: "shadowOpacity")
        glow.fromValue = 0.9
        glow.toValue = 0.28
        glow.duration = 0.65
        characterLayer.shadowColor = NSColor.systemCyan.cgColor
        characterLayer.add(glow, forKey: "mech-glow")
        revealMessage(delay: 0.48, duration: 0.30)
        pulseAccents(delay: 0.48)
    }

    private func animateWebRangerLeap() {
        let final = characterLayer.position
        let leap = CAKeyframeAnimation(keyPath: "position")
        leap.values = [
            NSValue(point: CGPoint(x: bounds.width + characterLayer.bounds.width * 0.65, y: bounds.height + 80)),
            NSValue(point: CGPoint(x: final.x - 28, y: final.y + 24)),
            NSValue(point: final)
        ]
        leap.keyTimes = [0, 0.76, 1]
        leap.duration = 0.80
        leap.timingFunctions = [
            CAMediaTimingFunction(controlPoints: 0.20, 0.78, 0.24, 1),
            CAMediaTimingFunction(controlPoints: 0.32, 0.88, 0.40, 1)
        ]
        characterLayer.add(leap, forKey: "web-leap")

        let pose = CAKeyframeAnimation(keyPath: "transform.rotation.z")
        pose.values = [0.32, -0.05, 0]
        pose.keyTimes = leap.keyTimes
        pose.duration = leap.duration
        characterLayer.add(pose, forKey: "web-pose")

        for (index, shape) in accentLayers.compactMap({ $0 as? CAShapeLayer }).enumerated() {
            let draw = CABasicAnimation(keyPath: "strokeEnd")
            draw.fromValue = 0
            draw.toValue = 1
            draw.duration = 0.42
            draw.beginTime = CACurrentMediaTime() + 0.30 + Double(index) * 0.08
            draw.fillMode = .backwards
            shape.add(draw, forKey: "web-draw-\(index)")
        }
        revealMessage(delay: 0.55, duration: 0.32)
    }

    private func revealMessage(delay: CFTimeInterval, duration: CFTimeInterval) {
        let appear = CAAnimationGroup()
        let scale = CAKeyframeAnimation(keyPath: "transform.scale")
        scale.values = [0.30, 1.06, 1]
        scale.keyTimes = [0, 0.76, 1]
        let fade = CABasicAnimation(keyPath: "opacity")
        fade.fromValue = 0
        fade.toValue = 1
        appear.animations = [scale, fade]
        appear.duration = duration
        appear.beginTime = CACurrentMediaTime() + delay
        appear.fillMode = .backwards
        appear.timingFunction = CAMediaTimingFunction(name: .easeOut)
        messageLayer.add(appear, forKey: "message-reveal")
    }

    private func streakAccents(delay: CFTimeInterval) {
        for (index, layer) in accentLayers.enumerated() {
            let move = CABasicAnimation(keyPath: "transform.translation.x")
            move.fromValue = -150
            move.toValue = 0
            move.duration = 0.40
            move.beginTime = CACurrentMediaTime() + delay + Double(index) * 0.05
            move.fillMode = .backwards
            move.timingFunction = CAMediaTimingFunction(name: .easeOut)
            layer.add(move, forKey: "streak")
        }
    }

    private func floatAccents(delay: CFTimeInterval) {
        for (index, layer) in accentLayers.enumerated() {
            let float = CAKeyframeAnimation(keyPath: "transform.translation.y")
            float.values = [-18, 7, 0]
            float.duration = 0.46
            float.beginTime = CACurrentMediaTime() + delay + Double(index) * 0.06
            float.fillMode = .backwards
            layer.add(float, forKey: "leaf-float")
        }
    }

    private func pulseAccents(delay: CFTimeInterval) {
        for layer in accentLayers {
            let pulse = CABasicAnimation(keyPath: "opacity")
            pulse.fromValue = 0
            pulse.toValue = 1
            pulse.duration = 0.34
            pulse.beginTime = CACurrentMediaTime() + delay
            pulse.fillMode = .backwards
            layer.add(pulse, forKey: "hologram-pulse")
        }
    }

    private func springTimingFunctions() -> [CAMediaTimingFunction] {
        [
            CAMediaTimingFunction(controlPoints: 0.18, 0.76, 0.24, 1),
            CAMediaTimingFunction(controlPoints: 0.28, 0.86, 0.36, 1),
            CAMediaTimingFunction(controlPoints: 0.30, 0.86, 0.42, 1)
        ]
    }
}
