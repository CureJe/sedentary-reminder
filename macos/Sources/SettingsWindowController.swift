import AppKit
import ServiceManagement

final class SettingsWindowController: NSWindowController, NSWindowDelegate {
    weak var settingsDelegate: SettingsWindowControllerDelegate?
    var nextReminderProvider: (() -> Date?)?

    private let settings: SettingsStore
    private let intervalField = NSTextField()
    private let popupField = NSTextField()
    private let characterPopup = NSPopUpButton()
    private let soundCheckbox = NSButton(checkboxWithTitle: "Play a short character sound", target: nil, action: nil)
    private let loginCheckbox = NSButton(checkboxWithTitle: "Launch at login", target: nil, action: nil)
    private let runningButton = NSButton()
    private let statusLabel = NSTextField(labelWithString: "")
    private let nextLabel = NSTextField(labelWithString: "")
    private var statusTimer: Timer?

    init(settings: SettingsStore) {
        self.settings = settings

        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 680, height: 560),
            styleMask: [.titled, .closable, .miniaturizable],
            backing: .buffered,
            defer: false
        )
        window.title = "Stand Up Buddy"
        window.center()
        window.isReleasedWhenClosed = false
        window.titlebarAppearsTransparent = true
        window.backgroundColor = .windowBackgroundColor

        super.init(window: window)
        window.delegate = self
        buildInterface(in: window)
        startStatusTimer()
        refresh()
    }

    required init?(coder: NSCoder) {
        nil
    }

    deinit {
        statusTimer?.invalidate()
    }

    private func buildInterface(in window: NSWindow) {
        let effect = NSVisualEffectView()
        effect.material = .sidebar
        effect.blendingMode = .behindWindow
        effect.state = .active
        effect.translatesAutoresizingMaskIntoConstraints = false
        window.contentView = effect

        let title = NSTextField(labelWithString: "Stay focused. Take breaks.")
        title.font = .systemFont(ofSize: 26, weight: .semibold)
        title.textColor = .labelColor

        let subtitle = NSTextField(labelWithString: "Native menu bar app / Lightweight / Animated reminders")
        subtitle.font = .systemFont(ofSize: 13, weight: .regular)
        subtitle.textColor = .secondaryLabelColor

        statusLabel.font = .systemFont(ofSize: 15, weight: .semibold)
        statusLabel.textColor = .labelColor
        nextLabel.font = .monospacedDigitSystemFont(ofSize: 13, weight: .regular)
        nextLabel.textColor = .secondaryLabelColor

        let statusStack = NSStackView(views: [statusLabel, nextLabel])
        statusStack.orientation = .vertical
        statusStack.alignment = .leading
        statusStack.spacing = 5

        let statusCard = makeCard(containing: statusStack)

        configureNumberField(intervalField, minimum: 1, maximum: 240)
        configureNumberField(popupField, minimum: 5, maximum: 120)

        characterPopup.addItem(withTitle: "Rotate all five characters")
        CharacterKind.allCases.forEach { characterPopup.addItem(withTitle: $0.displayName) }
        characterPopup.target = self
        characterPopup.action = #selector(settingsChanged(_:))

        soundCheckbox.target = self
        soundCheckbox.action = #selector(settingsChanged(_:))
        loginCheckbox.target = self
        loginCheckbox.action = #selector(loginSettingChanged(_:))

        let settingsGrid = NSGridView(views: [
            [label("Remind every"), fieldRow(intervalField, suffix: "minutes")],
            [label("Show for"), fieldRow(popupField, suffix: "seconds")],
            [label("Character"), characterPopup]
        ])
        settingsGrid.rowSpacing = 14
        settingsGrid.columnSpacing = 24
        settingsGrid.column(at: 0).xPlacement = .trailing
        settingsGrid.column(at: 1).xPlacement = .fill

        let optionsStack = NSStackView(views: [settingsGrid, soundCheckbox, loginCheckbox])
        optionsStack.orientation = .vertical
        optionsStack.alignment = .leading
        optionsStack.spacing = 14
        optionsStack.setCustomSpacing(20, after: settingsGrid)

        let settingsCard = makeCard(containing: optionsStack)

        let previewButton = NSButton(title: "Preview next character", target: self, action: #selector(preview(_:)))
        previewButton.bezelStyle = .rounded
        previewButton.controlSize = .large

        runningButton.target = self
        runningButton.action = #selector(toggleRunning(_:))
        runningButton.bezelStyle = .rounded
        runningButton.controlSize = .large
        runningButton.keyEquivalent = "\r"

        let buttons = NSStackView(views: [previewButton, runningButton])
        buttons.orientation = .horizontal
        buttons.alignment = .centerY
        buttons.distribution = .fillEqually
        buttons.spacing = 12

        let footnote = NSTextField(wrappingLabelWithString: "Click a character to dismiss, or right-click to snooze for 5 minutes. You can also use Return / Space to dismiss and S to snooze. Animations respect the macOS Reduce Motion setting.")
        footnote.font = .systemFont(ofSize: 12)
        footnote.textColor = .tertiaryLabelColor

        let root = NSStackView(views: [title, subtitle, statusCard, settingsCard, buttons, footnote])
        root.orientation = .vertical
        root.alignment = .leading
        root.spacing = 12
        root.setCustomSpacing(24, after: subtitle)
        root.setCustomSpacing(18, after: statusCard)
        root.setCustomSpacing(20, after: settingsCard)
        root.translatesAutoresizingMaskIntoConstraints = false
        effect.addSubview(root)

        NSLayoutConstraint.activate([
            root.leadingAnchor.constraint(equalTo: effect.leadingAnchor, constant: 40),
            root.trailingAnchor.constraint(equalTo: effect.trailingAnchor, constant: -40),
            root.topAnchor.constraint(equalTo: effect.topAnchor, constant: 34),
            root.bottomAnchor.constraint(lessThanOrEqualTo: effect.bottomAnchor, constant: -30),
            statusCard.widthAnchor.constraint(equalTo: root.widthAnchor),
            settingsCard.widthAnchor.constraint(equalTo: root.widthAnchor),
            buttons.widthAnchor.constraint(equalTo: root.widthAnchor),
            intervalField.widthAnchor.constraint(equalToConstant: 84),
            popupField.widthAnchor.constraint(equalToConstant: 84),
            characterPopup.widthAnchor.constraint(greaterThanOrEqualToConstant: 260)
        ])
    }

    private func makeCard(containing view: NSView) -> NSBox {
        let box = NSBox()
        box.boxType = .custom
        box.borderType = .lineBorder
        box.borderWidth = 0.5
        box.borderColor = .separatorColor
        box.cornerRadius = 16
        box.fillColor = NSColor.controlBackgroundColor.withAlphaComponent(0.78)
        box.contentViewMargins = NSSize(width: 22, height: 18)
        box.contentView = view
        return box
    }

    private func label(_ text: String) -> NSTextField {
        let field = NSTextField(labelWithString: text)
        field.font = .systemFont(ofSize: 13, weight: .medium)
        field.textColor = .secondaryLabelColor
        return field
    }

    private func fieldRow(_ field: NSTextField, suffix: String) -> NSStackView {
        let suffixLabel = NSTextField(labelWithString: suffix)
        suffixLabel.textColor = .secondaryLabelColor
        let row = NSStackView(views: [field, suffixLabel])
        row.orientation = .horizontal
        row.spacing = 8
        return row
    }

    private func configureNumberField(_ field: NSTextField, minimum: Int, maximum: Int) {
        let formatter = NumberFormatter()
        formatter.numberStyle = .none
        formatter.minimum = NSNumber(value: minimum)
        formatter.maximum = NSNumber(value: maximum)
        formatter.allowsFloats = false
        field.formatter = formatter
        field.alignment = .right
        field.target = self
        field.action = #selector(settingsChanged(_:))
    }

    private func startStatusTimer() {
        let timer = Timer(timeInterval: 1, repeats: true) { [weak self] _ in
            self?.refreshStatus()
        }
        RunLoop.main.add(timer, forMode: .common)
        statusTimer = timer
    }

    func refresh() {
        intervalField.integerValue = settings.intervalMinutes
        popupField.integerValue = settings.popupSeconds
        characterPopup.selectItem(at: settings.characterSelection + 1)
        soundCheckbox.state = settings.soundEnabled ? .on : .off
        let loginStatus = SMAppService.mainApp.status
        loginCheckbox.state = (loginStatus == .enabled || loginStatus == .requiresApproval) ? .on : .off
        runningButton.title = settings.isRunning ? "Pause reminders" : "Start reminders"
        refreshStatus()
    }

    func refreshStatus() {
        guard isWindowLoaded else { return }
        if settings.isRunning {
            statusLabel.stringValue = "Reminders running"
            statusLabel.textColor = .systemGreen
            if let next = nextReminderProvider?() {
                let remaining = max(0, Int(next.timeIntervalSinceNow.rounded(.up)))
                nextLabel.stringValue = String(format: "Next break: %02d:%02d", remaining / 60, remaining % 60)
            } else {
                nextLabel.stringValue = "Scheduling your next break"
            }
        } else {
            statusLabel.stringValue = "Reminders paused"
            statusLabel.textColor = .secondaryLabelColor
            nextLabel.stringValue = "Select Start reminders to restart the timer"
        }
    }

    @objc private func settingsChanged(_ sender: Any?) {
        settings.intervalMinutes = intervalField.integerValue
        settings.popupSeconds = popupField.integerValue
        settings.characterSelection = characterPopup.indexOfSelectedItem - 1
        settings.soundEnabled = soundCheckbox.state == .on
        refresh()
        settingsDelegate?.settingsWindowDidChange(self)
    }

    @objc private func loginSettingChanged(_ sender: NSButton) {
        let service = SMAppService.mainApp
        do {
            if sender.state == .on {
                try service.register()
            } else {
                try service.unregister()
            }
        } catch {
            let status = service.status
            sender.state = (status == .enabled || status == .requiresApproval) ? .on : .off
            let alert = NSAlert(error: error)
            alert.messageText = "Unable to update launch at login"
            alert.informativeText = "Check permissions in System Settings > General > Login Items, then try again."
            alert.runModal()
        }

        if service.status == .requiresApproval {
            let alert = NSAlert()
            alert.messageText = "Approval required"
            alert.informativeText = "Allow Stand Up Buddy to launch at login in System Settings > General > Login Items."
            alert.addButton(withTitle: "OK")
            alert.runModal()
        }
        refresh()
    }

    @objc private func toggleRunning(_ sender: Any?) {
        settings.isRunning.toggle()
        refresh()
        settingsDelegate?.settingsWindowDidChange(self)
    }

    @objc private func preview(_ sender: Any?) {
        settingsChanged(nil)
        settingsDelegate?.settingsWindowDidRequestPreview(self)
    }
}
