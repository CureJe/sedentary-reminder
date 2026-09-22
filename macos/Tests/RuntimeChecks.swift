import AppKit
import QuartzCore

enum CheckFailure: Error, CustomStringConvertible {
    case failed(String)
    var description: String {
        switch self { case .failed(let message): return message }
    }
}

final class FinishRecorder: ReminderWindowControllerDelegate {
    var results: [ReminderResult] = []
    func reminderWindow(_ controller: ReminderWindowController, didFinish result: ReminderResult, preview: Bool) {
        results.append(result)
    }
}

@MainActor
final class RuntimeChecks {
    let delegate: AppDelegate
    let settings: SettingsStore
    let output: URL
    var checks = 0
    var events: [String] = []
    let started = Date()

    init(delegate: AppDelegate, settings: SettingsStore, output: URL) {
        self.delegate = delegate
        self.settings = settings
        self.output = output
    }

    func require(_ condition: Bool, _ message: String) throws {
        guard condition else { throw CheckFailure.failed(message) }
        checks += 1
    }

    func record(_ message: String) {
        let line = String(format: "%.1fs: %@", Date().timeIntervalSince(started), message)
        events.append(line)
        print(line)
    }

    func wait(_ message: String, timeout: TimeInterval, until predicate: () -> Bool) async throws {
        let deadline = Date().addingTimeInterval(timeout)
        while !predicate() {
            guard Date() < deadline else { throw CheckFailure.failed("Timeout: " + message) }
            try await Task.sleep(nanoseconds: 50_000_000)
        }
        checks += 1
    }

    func views(_ root: NSView) -> [NSView] {
        [root] + root.subviews.flatMap { views($0) }
    }

    func menuAction(_ title: String) throws {
        let menu = delegate.runtimeStatusMenu
        let index = menu.indexOfItem(withTitle: title)
        try require(index >= 0, "Missing menu action: " + title)
        menu.performActionForItem(at: index)
    }

    func key(_ code: UInt16, in controller: ReminderWindowController) throws {
        guard let window = controller.window, let view = window.contentView,
              let event = NSEvent.keyEvent(with: .keyDown, location: .zero,
                modifierFlags: [], timestamp: ProcessInfo.processInfo.systemUptime,
                windowNumber: window.windowNumber, context: nil,
                characters: code == 1 ? "s" : "", charactersIgnoringModifiers: "",
                isARepeat: false, keyCode: code) else {
            throw CheckFailure.failed("Could not create key event")
        }
        view.keyDown(with: event)
    }

    func saveView(_ view: NSView, name: String, layerOnly: Bool = false) throws {
        view.layoutSubtreeIfNeeded()
        guard let bitmap = view.bitmapImageRepForCachingDisplay(in: view.bounds) else {
            throw CheckFailure.failed("Could not allocate render: " + name)
        }
        if layerOnly {
            guard let context = NSGraphicsContext(bitmapImageRep: bitmap)?.cgContext,
                  let layer = view.layer else { throw CheckFailure.failed("Missing render layer") }
            // Capture the settled model-layer geometry, not the runner's desktop.
            layer.render(in: context)
        } else {
            view.cacheDisplay(in: view.bounds, to: bitmap)
        }
        guard let png = bitmap.representation(using: .png, properties: [:]) else {
            throw CheckFailure.failed("Could not encode render: " + name)
        }
        try png.write(to: output.appendingPathComponent(name + ".png"))
        try require(png.count > 1000, "Render is unexpectedly small: " + name)
    }

    func inspectSettings(_ controller: SettingsWindowController) throws {
        guard let window = controller.window, let root = window.contentView else {
            throw CheckFailure.failed("Settings window missing")
        }
        try require(window.isVisible && window.title == "Stand Up Buddy", "Settings did not open")
        root.layoutSubtreeIfNeeded()
        try saveView(root, name: "settings")
        let all = views(root)
        let controls = all.compactMap { $0 as? NSControl }.filter { !$0.isHiddenOrHasHiddenAncestor }
        try require(controls.count >= 10, "Settings controls missing")
        for control in controls {
            let frame = control.convert(control.bounds, to: root)
            try require(frame.width > 0 && frame.height > 0, "Empty settings control")
            try require(root.bounds.insetBy(dx: -2, dy: -2).contains(frame),
                        "Settings control outside content: \(control.stringValue); frame=\(frame), content=\(root.bounds)")
            try require(control.stringValue.range(of: "\\p{Han}", options: .regularExpression) == nil,
                        "Non-English settings control")
        }
        let fields = all.compactMap { $0 as? NSTextField }.filter { $0.isEditable }
        try require(fields.count == 2, "Expected interval and duration fields")
        let interval = fields.first { ($0.formatter as? NumberFormatter)?.maximum?.intValue == 240 }
        guard let interval else { throw CheckFailure.failed("Interval field missing") }
        interval.integerValue = 2
        _ = interval.sendAction(interval.action, to: interval.target)
        try require(settings.intervalMinutes == 2, "Settings action did not persist interval")
        let popup = all.compactMap { $0 as? NSPopUpButton }.first
        try require(popup?.numberOfItems == 6, "Character choices missing")
        popup?.selectItem(at: 3)
        if let popup { _ = popup.sendAction(popup.action, to: popup.target) }
        try require(settings.characterSelection == 2, "Character selection action failed")
        try saveView(root, name: "settings")
    }

    func run() async throws {
        try FileManager.default.createDirectory(at: output, withIntermediateDirectories: true)
        try require(!NSScreen.screens.isEmpty, "Runner has no AppKit screen")
        try require(NSApp.activationPolicy() == .accessory, "App is not a menu bar application")
        try require(delegate.runtimeStatusMenu.items.map(\.title).contains("Quit Stand Up Buddy"), "Menu did not launch")
        try menuAction("Pause reminders")
        try require(!settings.isRunning, "Menu pause failed")
        guard let settingsWindow = delegate.runtimeSettingsWindow else {
            throw CheckFailure.failed("First launch did not create settings")
        }
        try inspectSettings(settingsWindow)
        record("App launch, menu, settings actions and layout passed")

        settings.intervalMinutes = -1
        try require(settings.intervalMinutes == 1, "Lower interval clamp failed")
        settings.intervalMinutes = 500
        try require(settings.intervalMinutes == 240, "Upper interval clamp failed")
        settings.popupSeconds = 500
        try require(settings.popupSeconds == 120, "Duration clamp failed")
        settings.characterSelection = -1
        var rotation = 0
        for index in 0..<10 {
            try require(settings.character(rotationIndex: &rotation).rawValue == index % 5, "Character rotation failed")
        }
        settings.intervalMinutes = 1
        settings.popupSeconds = 5
        settings.soundEnabled = false
        settingsWindow.refresh()

        for character in CharacterKind.allCases {
            guard let image = ResourceLocator.url(folder: "Characters", filename: character.imageFilename),
                  let audio = ResourceLocator.url(folder: "Audio", filename: character.soundFilename) else {
                throw CheckFailure.failed("Resource path missing")
            }
            try require(NSImage(contentsOf: image)?.isValid == true, "Image cannot decode: " + character.displayName)
            try require(NSSound(contentsOf: audio, byReference: true) != nil, "Sound cannot decode: " + character.displayName)
            let recorder = FinishRecorder()
            let controller = ReminderWindowController(character: character, popupSeconds: 5, preview: true, soundEnabled: false)
            controller.reminderDelegate = recorder
            controller.show()
            try await Task.sleep(nanoseconds: 300_000_000)
            guard let window = controller.window, let view = window.contentView else {
                throw CheckFailure.failed("Reminder view missing")
            }
            try require(window.isVisible, "Reminder did not become visible")
            try require(view.acceptsFirstResponder, "Reminder cannot receive keys")
            try require((view.layer?.sublayers?.count ?? 0) >= 2, "Character scene did not build")
            try require(view.layer?.sublayers?.contains(where: { $0.contents != nil }) == true, "Character image missing from scene")
            try saveView(view, name: "character-" + String(character.rawValue), layerOnly: true)
            try key(53, in: controller)
            try require(recorder.results == [.completed], "Escape failed")
            controller.dismiss(result: .snoozed)
            try require(recorder.results.count == 1, "Dismiss callback repeated")
        }
        record("Five character windows, image/audio decoding, renders and Escape passed")

        for code in [UInt16(36), 49, 1] {
            let recorder = FinishRecorder()
            let controller = ReminderWindowController(character: .orangeCat, popupSeconds: 5, preview: false, soundEnabled: false)
            controller.reminderDelegate = recorder
            controller.show()
            try key(code, in: controller)
            try require(recorder.results == [code == 1 ? .snoozed : .completed], "Reminder key handling failed")
        }
        try menuAction("Preview now")
        guard let preview = delegate.runtimeReminder else { throw CheckFailure.failed("Menu preview failed") }
        try menuAction("Preview now")
        try require(delegate.runtimeReminder === preview, "Duplicate preview window")
        let previewDeadline = delegate.runtimeNextReminder
        try await Task.sleep(nanoseconds: 5_500_000_000)
        try require(delegate.runtimeReminder === preview, "Preview incorrectly timed out")
        try key(53, in: preview)
        try require(delegate.runtimeReminder == nil, "Preview did not clear")
        try require(delegate.runtimeNextReminder == previewDeadline, "Menu preview moved timer deadline")
        record("Return/Space/S, duplicate preview guard and preview lifetime passed")

        try menuAction("Start reminders")
        let firstDeadline = delegate.runtimeNextReminder
        try require(firstDeadline.timeIntervalSinceNow > 58, "Resume did not schedule a full minute")
        guard let root = settingsWindow.window?.contentView,
              let previewButton = views(root).compactMap({ $0 as? NSButton })
                .first(where: { $0.title == "Preview next character" }) else {
            throw CheckFailure.failed("Settings preview button missing")
        }
        _ = previewButton.sendAction(previewButton.action, to: previewButton.target)
        guard let settingsPreview = delegate.runtimeReminder else {
            throw CheckFailure.failed("Settings preview did not open")
        }
        try require(delegate.runtimeNextReminder == firstDeadline, "Unchanged settings preview moved timer deadline")
        try key(53, in: settingsPreview)
        try require(delegate.runtimeNextReminder == firstDeadline, "Settings preview dismissal moved timer deadline")
        try await wait("first natural reminder", timeout: 75) { self.delegate.runtimeReminder != nil }
        try require(Date() >= firstDeadline, "Reminder appeared before its deadline")
        record("First natural one-minute reminder appeared")
        try await wait("automatic five-second dismissal", timeout: 8) { self.delegate.runtimeReminder == nil }
        let secondDeadline = delegate.runtimeNextReminder
        try require(secondDeadline.timeIntervalSinceNow > 57, "Automatic dismissal did not reschedule")
        record("Automatic dismissal and rescheduling passed")
        try await wait("second natural reminder", timeout: 75) { self.delegate.runtimeReminder != nil }
        guard let second = delegate.runtimeReminder else { throw CheckFailure.failed("Second reminder missing") }
        try key(1, in: second)
        try require(delegate.runtimeReminder == nil, "Snooze did not close reminder")
        let snoozeDeadline = delegate.runtimeNextReminder
        try require(snoozeDeadline.timeIntervalSinceNow > 298, "Snooze is not five minutes")
        record("Snoozed second reminder; waiting five real minutes")
        try await wait("five-minute snooze", timeout: 315) { self.delegate.runtimeReminder != nil }
        try require(Date() >= snoozeDeadline, "Snoozed reminder arrived early")
        guard let third = delegate.runtimeReminder else { throw CheckFailure.failed("Snoozed reminder missing") }
        try key(53, in: third)
        try require(delegate.runtimeReminder == nil, "Escape did not close scheduled reminder")
        try require(delegate.runtimeNextReminder.timeIntervalSinceNow > 58, "Normal interval did not resume")
        try menuAction("Pause reminders")
        record("Five-minute snooze, Escape and normal rescheduling passed")
    }

    func writeReport(error: Error?) {
        #if arch(arm64)
        let architecture = "arm64"
        #else
        let architecture = "x86_64"
        #endif
        let report: [String: Any] = [
            "status": error == nil ? "passed" : "failed",
            "checks": checks,
            "architecture": architecture,
            "os": ProcessInfo.processInfo.operatingSystemVersionString,
            "elapsed_seconds": Date().timeIntervalSince(started),
            "source_commit": ProcessInfo.processInfo.environment["SOURCE_COMMIT"] ?? "unknown",
            "events": events,
            "error": error.map { String(describing: $0) } ?? "",
            "limits": ["CI virtual machine, not physical-device acceptance", "Programmatic control/key actions",
                       "Sound decoded, not played or heard", "No login registration, sleep/wake, multi-monitor or Gatekeeper acceptance",
                       "Own-view renders, not desktop screenshots; Reduce Motion system setting not changed"]
        ]
        do {
            try FileManager.default.createDirectory(at: output, withIntermediateDirectories: true)
            try JSONSerialization.data(withJSONObject: report, options: [.prettyPrinted, .sortedKeys])
                .write(to: output.appendingPathComponent("report.json"))
        } catch { print("Could not write report: \(error)") }
    }
}

@main
enum RuntimeChecksMain {
    @MainActor
    static func main() {
        let application = NSApplication.shared
        let suite = "com.standupbuddy.runtimechecks." + UUID().uuidString
        guard let defaults = UserDefaults(suiteName: suite) else { fatalError("Missing isolated defaults") }
        let settings = SettingsStore(defaults: defaults)
        settings.intervalMinutes = 1
        settings.popupSeconds = 5
        settings.soundEnabled = false
        let delegate = AppDelegate(settings: settings)
        application.delegate = delegate
        let output = URL(fileURLWithPath: CommandLine.arguments.dropFirst().first ?? "runtime-results", isDirectory: true)
        let harness = RuntimeChecks(delegate: delegate, settings: settings, output: output)
        DispatchQueue.main.async {
            Task { @MainActor in
                var failure: Error?
                do { try await harness.run() } catch { failure = error; print("FAIL: \(error)") }
                harness.writeReport(error: failure)
                defaults.removePersistentDomain(forName: suite)
                if failure == nil { print("PASS: \(harness.checks) macOS runtime checks") }
                exit(failure == nil ? 0 : 1)
            }
        }
        application.run()
    }
}
