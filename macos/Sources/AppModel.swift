import Foundation

enum CharacterKind: Int, CaseIterable {
    case orangeCat
    case corgi
    case redPanda
    case mechGuardian
    case webRanger

    var displayName: String {
        switch self {
        case .orangeCat: return "橘猫团子"
        case .corgi: return "柯基短腿"
        case .redPanda: return "小熊猫阿卷"
        case .mechGuardian: return "机甲守卫"
        case .webRanger: return "蛛网侠客 · Web Ranger"
        }
    }

    var imageFilename: String {
        switch self {
        case .orangeCat: return "cat.png"
        case .corgi: return "corgi.png"
        case .redPanda: return "red-panda.png"
        case .mechGuardian: return "mech.png"
        case .webRanger: return "web-ranger.png"
        }
    }

    var soundFilename: String {
        switch self {
        case .orangeCat: return "cat.wav"
        case .corgi: return "corgi.wav"
        case .redPanda: return "red-panda.wav"
        case .mechGuardian: return "mech.wav"
        case .webRanger: return "web-ranger.wav"
        }
    }

    var anchorsRight: Bool { self == .webRanger }
}

enum ReminderResult {
    case completed
    case snoozed
    case timedOut
}

final class SettingsStore {
    private enum Key {
        static let intervalMinutes = "intervalMinutes"
        static let popupSeconds = "popupSeconds"
        static let characterSelection = "characterSelection"
        static let isRunning = "isRunning"
        static let soundEnabled = "soundEnabled"
        static let hasLaunched = "hasLaunched"
    }

    private let defaults: UserDefaults

    init(defaults: UserDefaults = .standard) {
        self.defaults = defaults
        defaults.register(defaults: [
            Key.intervalMinutes: 45,
            Key.popupSeconds: 20,
            Key.characterSelection: -1,
            Key.isRunning: true,
            Key.soundEnabled: true,
            Key.hasLaunched: false
        ])
    }

    var intervalMinutes: Int {
        get { clamp(defaults.integer(forKey: Key.intervalMinutes), 1, 240) }
        set { defaults.set(clamp(newValue, 1, 240), forKey: Key.intervalMinutes) }
    }

    var popupSeconds: Int {
        get { clamp(defaults.integer(forKey: Key.popupSeconds), 5, 120) }
        set { defaults.set(clamp(newValue, 5, 120), forKey: Key.popupSeconds) }
    }

    // -1 means deterministic rotation; 0...4 maps to CharacterKind.
    var characterSelection: Int {
        get { clamp(defaults.integer(forKey: Key.characterSelection), -1, CharacterKind.allCases.count - 1) }
        set { defaults.set(clamp(newValue, -1, CharacterKind.allCases.count - 1), forKey: Key.characterSelection) }
    }

    var isRunning: Bool {
        get { defaults.bool(forKey: Key.isRunning) }
        set { defaults.set(newValue, forKey: Key.isRunning) }
    }

    var soundEnabled: Bool {
        get { defaults.bool(forKey: Key.soundEnabled) }
        set { defaults.set(newValue, forKey: Key.soundEnabled) }
    }

    var hasLaunched: Bool {
        get { defaults.bool(forKey: Key.hasLaunched) }
        set { defaults.set(newValue, forKey: Key.hasLaunched) }
    }

    func character(rotationIndex: inout Int) -> CharacterKind {
        if characterSelection >= 0,
           let selected = CharacterKind(rawValue: characterSelection) {
            return selected
        }
        let all = CharacterKind.allCases
        let selected = all[rotationIndex % all.count]
        rotationIndex += 1
        return selected
    }

    private func clamp(_ value: Int, _ minimum: Int, _ maximum: Int) -> Int {
        Swift.max(minimum, Swift.min(maximum, value))
    }
}

enum ResourceLocator {
    static func url(folder: String, filename: String) -> URL? {
        Bundle.main.resourceURL?
            .appendingPathComponent(folder, isDirectory: true)
            .appendingPathComponent(filename, isDirectory: false)
    }
}
