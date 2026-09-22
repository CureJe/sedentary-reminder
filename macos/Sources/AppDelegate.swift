import AppKit

protocol SettingsWindowControllerDelegate: AnyObject {
    func settingsWindowDidChange(_ controller: SettingsWindowController)
    func settingsWindowDidRequestPreview(_ controller: SettingsWindowController)
}

protocol ReminderWindowControllerDelegate: AnyObject {
    func reminderWindow(_ controller: ReminderWindowController, didFinish result: ReminderResult, preview: Bool)
}

final class AppDelegate: NSObject, NSApplicationDelegate, SettingsWindowControllerDelegate, ReminderWindowControllerDelegate {
    private let settings: SettingsStore
    private var statusItem: NSStatusItem!
    private var statusMenu: NSMenu!
    private var toggleMenuItem: NSMenuItem!
    private var timer: Timer?
    private var nextReminder = Date()
    private var settingsController: SettingsWindowController?
    private var reminderController: ReminderWindowController?
    private var rotationIndex = 0

    init(settings: SettingsStore = SettingsStore()) {
        self.settings = settings
        super.init()
    }

    #if RUNTIME_CHECKS
    // Read-only observations, compiled only into the isolated test application.
    var runtimeStatusMenu: NSMenu { statusMenu }
    var runtimeSettingsWindow: SettingsWindowController? { settingsController }
    var runtimeReminder: ReminderWindowController? { reminderController }
    var runtimeNextReminder: Date { nextReminder }
    #endif

    func applicationDidFinishLaunching(_ notification: Notification) {
        if let identifier = Bundle.main.bundleIdentifier {
            let currentPID = ProcessInfo.processInfo.processIdentifier
            let alreadyRunning = NSRunningApplication.runningApplications(withBundleIdentifier: identifier)
                .contains { $0.processIdentifier != currentPID }
            if alreadyRunning {
                NSApp.terminate(nil)
                return
            }
        }

        NSApp.setActivationPolicy(.accessory)
        configureStatusItem()
        startClock()
        scheduleNextReminder()

        if !settings.hasLaunched {
            settings.hasLaunched = true
            showSettings(nil)
        }
    }

    func applicationWillTerminate(_ notification: Notification) {
        timer?.invalidate()
    }

    private func configureStatusItem() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
        if let button = statusItem.button {
            button.image = NSImage(systemSymbolName: "figure.stand", accessibilityDescription: "Stand Up Buddy")
            button.image?.isTemplate = true
            button.toolTip = "Stand Up Buddy"
        }

        statusMenu = NSMenu()
        statusMenu.addItem(withTitle: "Open settings", action: #selector(showSettings(_:)), keyEquivalent: ",")
        statusMenu.addItem(withTitle: "Preview now", action: #selector(previewReminder(_:)), keyEquivalent: "p")
        toggleMenuItem = statusMenu.addItem(withTitle: "", action: #selector(toggleRunning(_:)), keyEquivalent: "")
        statusMenu.addItem(.separator())
        statusMenu.addItem(withTitle: "Quit Stand Up Buddy", action: #selector(quit(_:)), keyEquivalent: "q")
        statusMenu.items.forEach { $0.target = self }
        statusItem.menu = statusMenu
        updateMenuState()
    }

    private func startClock() {
        let clock = Timer(timeInterval: 1, repeats: true) { [weak self] _ in
            self?.clockTick()
        }
        RunLoop.main.add(clock, forMode: .common)
        timer = clock
    }

    private func clockTick() {
        guard settings.isRunning,
              reminderController == nil,
              Date() >= nextReminder else { return }
        showReminder(preview: false)
    }

    private func scheduleNextReminder(minutes: Int? = nil) {
        let value = minutes ?? settings.intervalMinutes
        nextReminder = Date().addingTimeInterval(TimeInterval(value * 60))
        settingsController?.refreshStatus()
    }

    @objc private func showSettings(_ sender: Any?) {
        if settingsController == nil {
            let controller = SettingsWindowController(settings: settings)
            controller.settingsDelegate = self
            controller.nextReminderProvider = { [weak self] in
                guard let self, self.settings.isRunning else { return nil }
                return self.nextReminder
            }
            settingsController = controller
        }
        settingsController?.refresh()
        settingsController?.showWindow(nil)
        NSApp.activate(ignoringOtherApps: true)
    }

    @objc private func previewReminder(_ sender: Any?) {
        showReminder(preview: true)
    }

    @objc private func toggleRunning(_ sender: Any?) {
        settings.isRunning.toggle()
        if settings.isRunning { scheduleNextReminder() }
        updateMenuState()
        settingsController?.refresh()
    }

    @objc private func quit(_ sender: Any?) {
        reminderController?.dismiss(result: .completed)
        NSApp.terminate(nil)
    }

    private func showReminder(preview: Bool) {
        guard reminderController == nil else { return }
        let character = settings.character(rotationIndex: &rotationIndex)
        let controller = ReminderWindowController(
            character: character,
            popupSeconds: settings.popupSeconds,
            preview: preview,
            soundEnabled: settings.soundEnabled
        )
        controller.reminderDelegate = self
        reminderController = controller
        controller.show()
    }

    private func updateMenuState() {
        toggleMenuItem.title = settings.isRunning ? "Pause reminders" : "Start reminders"
    }

    func settingsWindowDidChange(_ controller: SettingsWindowController) {
        if settings.isRunning { scheduleNextReminder() }
        updateMenuState()
    }

    func settingsWindowDidRequestPreview(_ controller: SettingsWindowController) {
        showReminder(preview: true)
    }

    func reminderWindow(_ controller: ReminderWindowController, didFinish result: ReminderResult, preview: Bool) {
        reminderController = nil
        guard !preview else { return }
        if result == .snoozed {
            scheduleNextReminder(minutes: 5)
        } else {
            scheduleNextReminder()
        }
    }
}

#if !RUNTIME_CHECKS
@main
enum StandUpBuddyMain {
    private static let delegate = AppDelegate()

    static func main() {
        let application = NSApplication.shared
        application.delegate = delegate
        application.run()
    }
}
#endif
