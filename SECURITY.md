# Security Policy

EasySophosVPN (SophosVpnMiniTool) speichert VPN-Zugangsdaten lokal (DPAPI,
Benutzerkontext) und steuert Sophos Connect über `sccli.exe`. Sicherheitslücken
bitte verantwortungsvoll melden.

## Unterstützte Versionen

Sicherheitskorrekturen gibt es nur für den aktuellen Stand von `main` bzw. das
neueste Release.

## Schwachstellen melden

Bitte **keine** öffentlichen Issues für Sicherheitslücken anlegen, sondern privat
über GitHub melden:
[Security Advisories → Report a vulnerability](https://github.com/marcwoge/EasySophosVPN/security/advisories/new)

Bitte betroffene Version, Reproduktionsschritte und mögliche Auswirkungen angeben.
Eingangsbestätigung in der Regel innerhalb von 5 Werktagen.

## Bekannte Einschränkung

Sophos Connect nimmt Passwort und OTP nur als Kommandozeilenargument von
`sccli.exe` entgegen. Während des Verbindungsaufbaus ist dieser Wert daher für
Prozesse desselben Benutzers (bzw. Administratoren) in der Prozessliste sichtbar.
