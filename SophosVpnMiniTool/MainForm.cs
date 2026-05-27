using System.Text.RegularExpressions;

namespace SophosVpnMiniTool;

internal sealed class MainForm : Form
{
    private const string PasswordPlaceholder = "********";

    private readonly ConfigStore _configStore = new();
    private readonly PasswordStore _passwordStore = new();
    private readonly SophosCliService _sophosCliService = new();
    private readonly AutostartService _autostartService = new();

    private readonly TextBox _sccliPathTextBox = new();
    private readonly TextBox _connectionNameTextBox = new();
    private readonly TextBox _usernameTextBox = new();
    private readonly TextBox _passwordTextBox = new();
    private readonly TextBox _tokenTextBox = new();
    private readonly TextBox _logTextBox = new();
    private readonly Panel _statusPanel = new();
    private readonly Label _statusLabel = new();
    private readonly Button _connectButton = new();
    private readonly Button _disconnectButton = new();
    private readonly Button _listButton = new();
    private readonly Button _statusButton = new();
    private readonly Button _saveCredentialsButton = new();
    private readonly CheckBox _autostartCheckBox = new();
    private readonly NotifyIcon _trayIcon = new();
    private readonly ContextMenuStrip _trayMenu = new();

    private AppConfig _config = new();
    private bool _isExitRequested;
    private bool _isLoadingSettings;

    public MainForm()
    {
        Text = "SophosVpnMiniTool";
        MinimumSize = new Size(760, 560);
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
        BuildTrayIcon();
        LoadSettings();

        FormClosing += MainForm_FormClosing;
        Resize += MainForm_Resize;
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var inputGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3
        };
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddPathRow(inputGrid);
        AddTextRow(inputGrid, "Verbindungsname", _connectionNameTextBox);
        AddTextRow(inputGrid, "Benutzername", _usernameTextBox);
        AddTextRow(inputGrid, "VPN-Passwort ohne Token", _passwordTextBox);
        _passwordTextBox.UseSystemPasswordChar = true;

        _saveCredentialsButton.Text = "Zugangsdaten speichern/aendern";
        _saveCredentialsButton.AutoSize = true;
        _saveCredentialsButton.Click += SaveCredentialsButton_Click;
        inputGrid.Controls.Add(new Label(), 0, inputGrid.RowCount);
        inputGrid.Controls.Add(_saveCredentialsButton, 1, inputGrid.RowCount);
        inputGrid.SetColumnSpan(_saveCredentialsButton, 2);
        inputGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        inputGrid.RowCount++;

        AddTextRow(inputGrid, "Aktueller Token", _tokenTextBox);
        _tokenTextBox.MaxLength = 6;

        _autostartCheckBox.Text = "Mit Windows starten";
        _autostartCheckBox.AutoSize = true;
        _autostartCheckBox.CheckedChanged += AutostartCheckBox_CheckedChanged;
        inputGrid.Controls.Add(new Label(), 0, inputGrid.RowCount);
        inputGrid.Controls.Add(_autostartCheckBox, 1, inputGrid.RowCount);
        inputGrid.SetColumnSpan(_autostartCheckBox, 2);
        inputGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        inputGrid.RowCount++;

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 12, 0, 8)
        };

        _connectButton.Text = "Verbinden";
        _connectButton.Click += ConnectButton_Click;
        _disconnectButton.Text = "Trennen";
        _disconnectButton.Click += DisconnectButton_Click;
        _listButton.Text = "Verbindungen anzeigen";
        _listButton.Click += ListButton_Click;
        _statusButton.Text = "Status aktualisieren";
        _statusButton.Click += StatusButton_Click;

        actions.Controls.AddRange(new Control[] { _connectButton, _disconnectButton, _listButton, _statusButton });

        var statusRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        _statusPanel.Size = new Size(18, 18);
        _statusPanel.Margin = new Padding(3, 6, 8, 3);
        _statusLabel.AutoSize = true;
        _statusLabel.Text = "Status: unbekannt";
        _statusLabel.Margin = new Padding(0, 5, 0, 0);
        statusRow.Controls.Add(_statusPanel);
        statusRow.Controls.Add(_statusLabel);
        SetStatus(ConnectionState.Unknown);

        var top = new Panel { Dock = DockStyle.Top, AutoSize = true };
        top.Controls.Add(statusRow);
        top.Controls.Add(actions);
        top.Controls.Add(inputGrid);

        _logTextBox.Dock = DockStyle.Fill;
        _logTextBox.Multiline = true;
        _logTextBox.ReadOnly = true;
        _logTextBox.ScrollBars = ScrollBars.Vertical;
        _logTextBox.Font = new Font(FontFamily.GenericMonospace, 9f);

        root.Controls.Add(top, 0, 0);
        root.Controls.Add(_logTextBox, 0, 1);

        Controls.Add(root);
    }

    private void BuildTrayIcon()
    {
        var openItem = new ToolStripMenuItem("Oeffnen", null, (_, _) => ShowMainWindow());
        var connectItem = new ToolStripMenuItem("Verbinden...", null, (_, _) =>
        {
            ShowMainWindow();
            _tokenTextBox.Focus();
        });
        var disconnectItem = new ToolStripMenuItem("Trennen", null, DisconnectButton_Click);
        var statusItem = new ToolStripMenuItem("Status aktualisieren", null, StatusButton_Click);
        var exitItem = new ToolStripMenuItem("Beenden", null, (_, _) =>
        {
            _isExitRequested = true;
            _trayIcon.Visible = false;
            Close();
        });

        _trayMenu.Items.AddRange(new ToolStripItem[]
        {
            openItem,
            connectItem,
            disconnectItem,
            statusItem,
            new ToolStripSeparator(),
            exitItem
        });

        _trayIcon.Text = "SophosVpnMiniTool";
        _trayIcon.Icon = SystemIcons.Application;
        _trayIcon.ContextMenuStrip = _trayMenu;
        _trayIcon.Visible = true;
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void AddPathRow(TableLayoutPanel grid)
    {
        AddLabel(grid, "sccli.exe-Pfad");
        _sccliPathTextBox.Dock = DockStyle.Fill;
        grid.Controls.Add(_sccliPathTextBox, 1, grid.RowCount);

        var browseButton = new Button { Text = "Suchen...", AutoSize = true };
        browseButton.Click += BrowseButton_Click;
        grid.Controls.Add(browseButton, 2, grid.RowCount);

        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowCount++;
    }

    private static void AddTextRow(TableLayoutPanel grid, string labelText, TextBox textBox)
    {
        AddLabel(grid, labelText);
        textBox.Dock = DockStyle.Fill;
        grid.Controls.Add(textBox, 1, grid.RowCount);
        grid.SetColumnSpan(textBox, 2);
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowCount++;
    }

    private static void AddLabel(TableLayoutPanel grid, string text)
    {
        grid.Controls.Add(new Label
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 8, 8, 3)
        }, 0, grid.RowCount);
    }

    private void LoadSettings()
    {
        _isLoadingSettings = true;
        _config = _configStore.Load();
        _sccliPathTextBox.Text = _config.SccliPath;
        _connectionNameTextBox.Text = _config.ConnectionName;
        _usernameTextBox.Text = _config.Username;
        _passwordTextBox.Text = _passwordStore.HasPassword ? PasswordPlaceholder : string.Empty;
        _autostartCheckBox.Checked = _autostartService.IsEnabled();
        _isLoadingSettings = false;

        AppendLog("Einstellungen geladen.");
        AppendLog($"Konfigurationsdatei: {AppPaths.ConfigFilePath}");
        AppendLog($"Passwortdatei vorhanden: {(_passwordStore.HasPassword ? "ja" : "nein")}");
        AppendLog($"Windows-Autostart: {(_autostartCheckBox.Checked ? "aktiv" : "inaktiv")}");
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "sccli.exe auswaehlen",
            Filter = "Sophos sccli.exe|sccli.exe|Ausfuehrbare Dateien (*.exe)|*.exe|Alle Dateien (*.*)|*.*",
            CheckFileExists = true
        };

        if (File.Exists(_sccliPathTextBox.Text))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(_sccliPathTextBox.Text);
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _sccliPathTextBox.Text = dialog.FileName;
            SaveConfigFromUi();
            AppendLog("sccli.exe-Pfad gespeichert.");
        }
    }

    private void SaveCredentialsButton_Click(object? sender, EventArgs e)
    {
        try
        {
            SaveConfigFromUi();
            SaveAutostartFromUi();

            var passwordText = _passwordTextBox.Text;
            if (passwordText == PasswordPlaceholder && _passwordStore.HasPassword)
            {
                AppendLog("Einstellungen gespeichert. Das vorhandene Passwort wurde beibehalten.");
                return;
            }

            if (string.IsNullOrWhiteSpace(passwordText))
            {
                AppendLog("Einstellungen gespeichert. Kein neues Passwort eingegeben.");
                return;
            }

            _passwordStore.SavePassword(passwordText);
            _passwordTextBox.Text = PasswordPlaceholder;
            AppendLog("Einstellungen gespeichert. VPN-Passwort wurde verschluesselt gespeichert.");
        }
        catch (Exception ex)
        {
            AppendLog($"Speichern fehlgeschlagen: {ex.Message}");
        }
    }

    private void AutostartCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        try
        {
            SaveAutostartFromUi();
            AppendLog($"Windows-Autostart wurde {(_autostartCheckBox.Checked ? "aktiviert" : "deaktiviert")}.");
        }
        catch (Exception ex)
        {
            AppendLog($"Windows-Autostart konnte nicht geaendert werden: {ex.Message}");
            _isLoadingSettings = true;
            _autostartCheckBox.Checked = _autostartService.IsEnabled();
            _isLoadingSettings = false;
        }
    }

    private async void ConnectButton_Click(object? sender, EventArgs e)
    {
        await RunUiActionAsync(async () =>
        {
            if (!ValidateCommonInputs(requirePassword: true))
            {
                return;
            }

            var token = _tokenTextBox.Text.Trim();
            if (token.Length != 6 || token.Any(c => !char.IsDigit(c)))
            {
                AppendLog("Der Token muss aus genau 6 Ziffern bestehen. Verbindung wird nicht gestartet.");
                SetStatus(ConnectionState.DisconnectedOrError);
                return;
            }

            string? basePassword = null;
            string? passwordWithToken = null;

            try
            {
                basePassword = _passwordStore.LoadPassword();
                passwordWithToken = string.Concat(basePassword, token);

                AppendLog("Starte VPN-Verbindung...");
                var result = await _sophosCliService.EnableAsync(
                    _sccliPathTextBox.Text.Trim(),
                    _connectionNameTextBox.Text.Trim(),
                    _usernameTextBox.Text.Trim(),
                    passwordWithToken);

                AppendProcessResult(result, basePassword, token, passwordWithToken);
                _tokenTextBox.Clear();
                SetStatus(result.ExitCode == 0 ? ConnectionState.Connected : ConnectionState.DisconnectedOrError);
            }
            catch (Exception ex)
            {
                AppendLog($"Verbinden fehlgeschlagen: {ex.Message}");
                SetStatus(ConnectionState.DisconnectedOrError);
            }
            finally
            {
                basePassword = null;
                passwordWithToken = null;
                token = string.Empty;
            }
        });
    }

    private async void DisconnectButton_Click(object? sender, EventArgs e)
    {
        await RunUiActionAsync(async () =>
        {
            if (!ValidateCommonInputs(requirePassword: false))
            {
                return;
            }

            AppendLog("Trenne VPN-Verbindung...");
            var result = await _sophosCliService.DisableAsync(
                _sccliPathTextBox.Text.Trim(),
                _connectionNameTextBox.Text.Trim());

            AppendProcessResult(result);
            SetStatus(result.ExitCode == 0 ? ConnectionState.DisconnectedOrError : ConnectionState.DisconnectedOrError);
        });
    }

    private async void ListButton_Click(object? sender, EventArgs e)
    {
        await RunListAsync(updateStatus: true);
    }

    private async void StatusButton_Click(object? sender, EventArgs e)
    {
        await RunListAsync(updateStatus: true);
    }

    private async Task RunListAsync(bool updateStatus)
    {
        await RunUiActionAsync(async () =>
        {
            if (!ValidateSccliPath())
            {
                return;
            }

            AppendLog("Lese vorhandene Verbindungen...");
            var result = await _sophosCliService.ListDetailedAsync(_sccliPathTextBox.Text.Trim());
            AppendProcessResult(result);

            var names = TryDetectConnectionNames(result.StandardOutput);
            if (names.Count > 0)
            {
                AppendLog("Erkannte Verbindungsnamen: " + string.Join(", ", names));
            }

            if (updateStatus)
            {
                SetStatus(TryDetectConnectionState(result.StandardOutput, _connectionNameTextBox.Text.Trim(), result.ExitCode));
            }
        });
    }

    private void SaveConfigFromUi()
    {
        _config.SccliPath = _sccliPathTextBox.Text.Trim();
        _config.ConnectionName = _connectionNameTextBox.Text.Trim();
        _config.Username = _usernameTextBox.Text.Trim();
        _configStore.Save(_config);
    }

    private void SaveAutostartFromUi()
    {
        _autostartService.SetEnabled(_autostartCheckBox.Checked);
    }

    private bool ValidateCommonInputs(bool requirePassword)
    {
        SaveConfigFromUi();

        if (!ValidateSccliPath())
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_connectionNameTextBox.Text))
        {
            AppendLog("Bitte einen Verbindungsnamen eintragen.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_usernameTextBox.Text))
        {
            AppendLog("Bitte einen Benutzernamen eintragen.");
            return false;
        }

        if (requirePassword && !_passwordStore.HasPassword)
        {
            AppendLog("Es ist noch kein VPN-Passwort gespeichert. Bitte Passwort ohne Token eintragen und Zugangsdaten speichern.");
            return false;
        }

        return true;
    }

    private bool ValidateSccliPath()
    {
        var sccliPath = _sccliPathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(sccliPath))
        {
            AppendLog("Bitte den Pfad zu sccli.exe eintragen.");
            SetStatus(ConnectionState.DisconnectedOrError);
            return false;
        }

        if (!File.Exists(sccliPath))
        {
            AppendLog("sccli.exe wurde nicht gefunden. Bitte Sophos Connect installieren oder den Pfad korrigieren.");
            SetStatus(ConnectionState.DisconnectedOrError);
            return false;
        }

        return true;
    }

    private async Task RunUiActionAsync(Func<Task> action)
    {
        SetButtonsEnabled(false);
        try
        {
            await action();
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        _connectButton.Enabled = enabled;
        _disconnectButton.Enabled = enabled;
        _listButton.Enabled = enabled;
        _statusButton.Enabled = enabled;
        _saveCredentialsButton.Enabled = enabled;
        _autostartCheckBox.Enabled = enabled;
    }

    private void AppendProcessResult(ProcessResult result, params string?[] secrets)
    {
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            AppendLog("stdout:");
            AppendLog(RedactSecrets(result.StandardOutput.TrimEnd(), secrets));
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            AppendLog("stderr:");
            AppendLog(RedactSecrets(result.StandardError.TrimEnd(), secrets));
        }

        AppendLog($"ExitCode: {result.ExitCode}");
    }

    private static string RedactSecrets(string text, params string?[] secrets)
    {
        var redacted = text;
        foreach (var secret in secrets)
        {
            if (!string.IsNullOrEmpty(secret))
            {
                redacted = redacted.Replace(secret, "[REDACTED]", StringComparison.Ordinal);
            }
        }

        return redacted;
    }

    private static IReadOnlyList<string> TryDetectConnectionNames(string output)
    {
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(output, @"(?im)^\s*(?:Name|Connection(?:\s+Name)?)\s*[:=]\s*(.+?)\s*$"))
        {
            var name = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }
        }

        return names.ToList();
    }

    private static ConnectionState TryDetectConnectionState(string output, string connectionName, int exitCode)
    {
        if (exitCode != 0)
        {
            return ConnectionState.DisconnectedOrError;
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            return ConnectionState.Unknown;
        }

        var relevantOutput = output;
        if (!string.IsNullOrWhiteSpace(connectionName))
        {
            var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var matchingLines = lines.Where(line => line.Contains(connectionName, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matchingLines.Length > 0)
            {
                relevantOutput = string.Join(Environment.NewLine, matchingLines);
            }
        }

        if (Regex.IsMatch(relevantOutput, @"\b(connected|enabled|verbunden|aktiv)\b", RegexOptions.IgnoreCase))
        {
            return ConnectionState.Connected;
        }

        if (Regex.IsMatch(relevantOutput, @"\b(disconnected|disabled|getrennt|inactive|inaktiv|error|failed|fehler)\b", RegexOptions.IgnoreCase))
        {
            return ConnectionState.DisconnectedOrError;
        }

        return ConnectionState.Unknown;
    }

    private void SetStatus(ConnectionState state)
    {
        switch (state)
        {
            case ConnectionState.Connected:
                _statusPanel.BackColor = Color.ForestGreen;
                _statusLabel.Text = "Status: verbunden erkannt";
                break;
            case ConnectionState.DisconnectedOrError:
                _statusPanel.BackColor = Color.Firebrick;
                _statusLabel.Text = "Status: getrennt/Fehler";
                break;
            default:
                _statusPanel.BackColor = Color.Gray;
                _statusLabel.Text = "Status: unbekannt";
                break;
        }
    }

    private void AppendLog(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _logTextBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_isExitRequested)
        {
            return;
        }

        e.Cancel = true;
        Hide();
        _trayIcon.ShowBalloonTip(
            1500,
            "SophosVpnMiniTool",
            "Das Tool laeuft weiter im Infobereich.",
            ToolTipIcon.Info);
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
        }
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _trayMenu.Dispose();
        }

        base.Dispose(disposing);
    }
}
