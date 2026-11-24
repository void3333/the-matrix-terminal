using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: AssemblyTitle("The Matrix Setup")]
[assembly: AssemblyProduct("The Matrix")]
[assembly: AssemblyVersion("1.0.1.0")]
[assembly: AssemblyFileVersion("1.0.1.0")]

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new SetupWizard());
    }
}

internal sealed class SetupWizard : Form
{
    private static readonly Color Background = Color.FromArgb(4, 11, 6);
    private static readonly Color Card = Color.FromArgb(11, 22, 15);
    private static readonly Color Rail = Color.FromArgb(7, 26, 14);
    private static readonly Color Border = Color.FromArgb(48, 87, 61);
    private static readonly Color Green = Color.FromArgb(128, 255, 151);
    private static readonly Color Bright = Color.FromArgb(219, 246, 225);
    private static readonly Color Muted = Color.FromArgb(162, 197, 172);
    private static readonly Color Quiet = Color.FromArgb(110, 149, 119);
    private static readonly Color ErrorColor = Color.FromArgb(255, 162, 153);
    private static float scale = 1.0f;
    private static float fontScale = 1.0f;

    private readonly Panel deck = new Panel();
    private readonly MatrixPanel body = new MatrixPanel();
    private readonly MatrixPanel rail = new MatrixPanel();
    private readonly MatrixRain rain = new MatrixRain();
    private readonly Label[] navigation = new Label[3];
    private readonly Label mode = new Label();
    private Image badge;
    private bool fullScreen;
    private readonly string installRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\MatrixTerminal");
    private string project = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "vibecode");
    private bool startup = true;
    private bool hidden = true;
    private bool busy;
    private CheckBox autoStart, hide;
    private Label agentStatusCodex;
    private Label agentStatusClaude;
    private bool codexLoginRunning;
    private bool claudeLoginRunning;
    private ComboBox claudeMode;

    internal SetupWizard()
    {
        Text = "Instalação do The Matrix";
        BackColor = Background;
        ForeColor = Green;
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font("Segoe UI", 10);
        StartPosition = FormStartPosition.Manual;
        MaximizeBox = false;
        KeyPreview = true;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        Screen display = Screen.FromPoint(Cursor.Position);
        using (var graphics = CreateGraphics())
        {
            float dpiScale = graphics.DpiX / 96.0f;
            scale = Math.Min(dpiScale, Math.Min((display.WorkingArea.Width - 40) / 1010f, (display.WorkingArea.Height - 66) / 570f));
            fontScale = scale / dpiScale;
        }

        rain.Dock = DockStyle.Fill;
        Controls.Add(rain);
        deck.BackColor = Background;
        deck.Size = new Size(Pixels(974), Pixels(532));
        rain.Controls.Add(deck);

        rail.BackColor = Rail;
        rail.Edge = Border;
        Place(rail, 0, 0, 266, 532);
        deck.Controls.Add(rail);
        body.BackColor = Card;
        body.Edge = Border;
        Place(body, 283, 0, 691, 532);
        deck.Controls.Add(body);
        BuildRail();
        Resize += delegate { CenterDeck(); };
        FormClosing += delegate(object sender, FormClosingEventArgs args) { if (busy) args.Cancel = true; };
        Shown += delegate { rain.Start(); };
        SetFullScreen(true, display);
        ShowOptions();
    }

    private void BuildRail()
    {
        using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Matrix.Icon.png"))
        using (var source = Image.FromStream(resource))
            badge = new Bitmap(source);

        var artwork = new PictureBox { Image = badge, SizeMode = PictureBoxSizeMode.Zoom, AccessibleName = "Monitor do The Matrix" };
        Place(artwork, 25, 23, 123, 123);
        rail.Controls.Add(artwork);
        rail.Controls.Add(Mono("THE MATRIX", Green, 22, 25, 155, 230, 43, true));
        for (int i = 0; i < navigation.Length; i++)
        {
            navigation[i] = Mono("", Muted, 12, 26, 228 + i * 50, 210, 29, false);
            rail.Controls.Add(navigation[i]);
        }
        mode.Font = NewFont("Consolas", 10, FontStyle.Regular);
        mode.ForeColor = Muted;
        Place(mode, 25, 487, 228, 26);
        rail.Controls.Add(mode);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F11)
        {
            SetFullScreen(!fullScreen);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && badge != null) badge.Dispose();
    }

    private void SetFullScreen(bool enabled, Screen target = null)
    {
        Screen display = target ?? Screen.FromControl(this);
        fullScreen = enabled;
        if (enabled)
        {
            FormBorderStyle = FormBorderStyle.None;
            Bounds = display.Bounds;
        }
        else
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = new Size(Pixels(1006), Pixels(563));
            Rectangle area = display.WorkingArea;
            Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
        }
        mode.Text = enabled ? "F11  ·  Modo janela" : "F11  ·  Tela cheia";
        CenterDeck();
    }

    private void CenterDeck()
    {
        deck.Location = new Point((ClientSize.Width - deck.Width) / 2, (ClientSize.Height - deck.Height) / 2);
    }

    private void ResetPage(int index)
    {
        AcceptButton = null;
        CancelButton = null;
        while (body.Controls.Count != 0) body.Controls[0].Dispose();
        string[] names = { "Pasta de trabalho", "Instalação", "Contas" };
        for (int i = 0; i < navigation.Length; i++)
        {
            navigation[i].Text = (i < index ? "✓  " : i == index ? "›  " : "·  ") + names[i];
            navigation[i].ForeColor = i == index ? Green : i < index ? Bright : Quiet;
        }
    }

    private void Header(string title, string text)
    {
        body.Controls.Add(Caption(title, Bright, 29, 34, 35, 622, 55));
        body.Controls.Add(Caption(text, Muted, 11, 37, 99, 616, 51));
    }

    private void ShowOptions()
    {
        ResetPage(0);
        Header("Preparar o terminal", "O assistente instala Codex, Claude Code, Git Bash e o terminal com tema CRT nesta conta do Windows.");
        body.Controls.Add(Mono("PASTA DE TRABALHO", Green, 10, 37, 169, 610, 24, true));
        var path = new TextBox
        {
            Text = project, AccessibleName = "Pasta de trabalho", BackColor = Background,
            ForeColor = Bright, BorderStyle = BorderStyle.FixedSingle
        };
        Place(path, 37, 201, 476, 36);
        var browse = MakeButton("Escolher…", 529, 194, 125, false);
        browse.Click += delegate
        {
            using (var picker = new FolderBrowserDialog())
            {
                picker.Description = "Escolha a pasta inicial do The Matrix";
                picker.SelectedPath = Directory.Exists(path.Text) ? path.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                picker.ShowNewFolderButton = true;
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                path.Text = picker.SelectedPath;
                hide.Checked = false;
            }
        };
        body.Controls.Add(path);
        body.Controls.Add(browse);
        autoStart = MakeCheckBox("Abrir o The Matrix ao entrar no Windows", startup, 274);
        hide = MakeCheckBox("Marcar a pasta de trabalho como oculta", hidden, 319);
        body.Controls.Add(autoStart);
        body.Controls.Add(hide);
        body.Controls.Add(Caption("Conexão à internet necessária para baixar os componentes oficiais.", Muted, 10, 37, 386, 618, 42));
        var install = MakeButton("Instalar ›", 491, 456, 163, true);
        install.Click += delegate
        {
            try
            {
                string value = Environment.ExpandEnvironmentVariables(path.Text.Trim());
                if (!Path.IsPathRooted(value) || value.Length < 3)
                    throw new ArgumentException("Informe o caminho completo de uma pasta.");
                project = Path.GetFullPath(value);
                if (File.Exists(project)) throw new ArgumentException("Esse caminho é um arquivo. Escolha uma pasta.");
            }
            catch (Exception error) { ShowError(error.Message); return; }
            startup = autoStart.Checked;
            hidden = hide.Checked;
            InstallAsync();
        };
        var cancel = MakeButton("Cancelar", 352, 456, 125, false);
        cancel.Click += delegate { Close(); };
        body.Controls.Add(cancel);
        body.Controls.Add(install);
        AcceptButton = install;
        CancelButton = cancel;
        ActiveControl = install;
    }

    private async void InstallAsync()
    {
        ResetPage(1);
        busy = true;
        Header("Instalando o The Matrix", "Cada pacote é baixado da fonte oficial e conferido antes da instalação.");
        var stage = Mono("Preparando os arquivos…", Green, 11, 37, 153, 561, 32, false);
        var percent = Mono("0%", Green, 11, 606, 153, 52, 32, true);
        percent.TextAlign = ContentAlignment.TopRight;
        body.Controls.Add(stage);
        body.Controls.Add(percent);
        var progress = new MatrixMeter();
        Place(progress, 37, 192, 617, 10);
        body.Controls.Add(progress);

        string[] names = { "WezTerm", "PowerShell 7", "Oh My Posh", "Git e Git Bash", "Codex", "Claude Code" };
        int[] starts = { 8, 22, 36, 45, 58, 73, 88 };
        var rows = new Label[names.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = Mono("·  " + names[i], Muted, 11, 40, 224 + 31 * i, 610, 27, false);
            body.Controls.Add(rows[i]);
        }

        string problem = await Task.Run(() => Install(line =>
        {
            if (!line.StartsWith("STEP ", StringComparison.Ordinal)) return;
            string[] parts = line.Split(new[] { ' ' }, 3);
            int value;
            if (parts.Length != 3 || !int.TryParse(parts[1], out value)) return;
            if (!IsDisposed) BeginInvoke((Action)(() =>
            {
                if (stage.IsDisposed) return;
                progress.Value = value;
                percent.Text = value + "%";
                stage.Text = parts[2];
                for (int i = 0; i < rows.Length; i++)
                {
                    bool done = value >= starts[i + 1];
                    bool current = value >= starts[i] && !done;
                    rows[i].Text = (done ? "✓  " : current ? "›  " : "·  ") + names[i];
                    rows[i].ForeColor = done ? Bright : current ? Green : Muted;
                }
            }));
        }));
        busy = false;
        if (problem == null) { ShowAccounts(); return; }

        var errorText = Caption(problem, ErrorColor, 10, 37, 421, 453, 80);
        body.Controls.Add(errorText);
        var retry = MakeButton("Voltar", 516, 456, 138, true);
        retry.Click += delegate { ShowOptions(); };
        body.Controls.Add(retry);
        AcceptButton = retry;
    }

    private string Install(Action<string> report)
    {
        string scratch = Path.Combine(Path.GetTempPath(), "TheMatrix.Setup." + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(scratch);
            string archivePath = Path.Combine(scratch, "payload.zip");
            using (Stream payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("Matrix.Payload.zip"))
            using (var file = File.Create(archivePath))
            {
                if (payload == null) throw new IOException("O pacote do instalador está incompleto.");
                payload.CopyTo(file);
            }
            ZipFile.ExtractToDirectory(archivePath, scratch);
            var info = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"),
                Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -File "
                    + Quote(Path.Combine(scratch, "Install.ps1")) + " -Project " + Quote(project)
                    + (startup ? " -AutoStart" : "") + (hidden ? " -HideProject" : ""),
                WorkingDirectory = scratch,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            string lastError = null;
            using (var install = Process.Start(info))
            {
                Task<string> stderr = install.StandardError.ReadToEndAsync();
                string line;
                while ((line = install.StandardOutput.ReadLine()) != null)
                {
                    if (line.StartsWith("ERROR ", StringComparison.Ordinal)) lastError = line.Substring(6);
                    else report(line);
                }
                install.WaitForExit();
                string errors = stderr.GetAwaiter().GetResult();
                if (install.ExitCode == 0) return null;
                if (!string.IsNullOrWhiteSpace(lastError)) return lastError;
                return "A instalação foi interrompida (código " + install.ExitCode + "). "
                    + (string.IsNullOrWhiteSpace(errors) ? "Tente novamente." : errors.Split('\n')[0].Trim());
            }
        }
        catch (Exception error) { return error.GetBaseException().Message; }
        finally
        {
            try { if (Directory.Exists(scratch)) Directory.Delete(scratch, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private void ShowAccounts()
    {
        ResetPage(2);
        Header("Conecte suas contas", "Os botões abrem os logins oficiais no navegador. Você também pode entrar depois, dentro do terminal.");

        var codex = MakeButton("Entrar no Codex", 37, 169, 190, false);
        var claude = MakeButton("Entrar no Claude", 37, 238, 190, false);
        agentStatusCodex = Mono("Verificando…", Muted, 11, 246, 183, 409, 26, false);
        agentStatusClaude = Mono("Verificando…", Muted, 11, 246, 252, 193, 25, false);
        claudeMode = new ComboBox
        {
            BackColor = Background, ForeColor = Bright, DropDownStyle = ComboBoxStyle.DropDownList,
            AccessibleName = "Tipo de conta do Claude"
        };
        claudeMode.Items.AddRange(new object[] { "Conta Claude", "Anthropic Console", "SSO" });
        claudeMode.SelectedIndex = 0;
        claudeMode.FlatStyle = FlatStyle.Flat;
        claudeMode.DrawMode = DrawMode.OwnerDrawFixed;
        claudeMode.DrawItem += delegate(object _, DrawItemEventArgs args)
        {
            bool chosen = (args.State & DrawItemState.Selected) != 0;
            using (var fill = new SolidBrush(chosen ? Rail : Background))
                args.Graphics.FillRectangle(fill, args.Bounds);
            if (args.Index >= 0)
                TextRenderer.DrawText(args.Graphics, claudeMode.Items[args.Index].ToString(), claudeMode.Font,
                    args.Bounds, chosen ? Green : Bright,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
        Place(claudeMode, 447, 246, 207, 28);
        codex.Click += delegate { BeginLogin(true, codex); };
        claude.Click += delegate { BeginLogin(false, claude); };
        body.Controls.Add(codex);
        body.Controls.Add(agentStatusCodex);
        body.Controls.Add(claude);
        body.Controls.Add(agentStatusClaude);
        body.Controls.Add(claudeMode);

        body.Controls.Add(Mono("ABRIR A PARTIR DE QUALQUER PASTA", Green, 10, 37, 338, 617, 26, true));
        body.Controls.Add(Caption("No Explorador, pressione Ctrl+L, digite vibecode e aperte Enter. Use Ctrl+Alt+H no terminal para ver a ajuda.", Muted, 10, 37, 368, 617, 54));
        var open = MakeButton("Entrar na Matrix ›", 444, 456, 210, true);
        open.Click += delegate { LaunchTerminalAsync(); };
        body.Controls.Add(open);
        AcceptButton = open;
        RefreshLoginAsync(true);
        RefreshLoginAsync(false);
    }

    private async void LaunchTerminalAsync()
    {
        ResetPage(3);
        busy = true;
        Header("Abrindo o The Matrix", "Codex e Claude Code vão abrir lado a lado nesta pasta:");
        body.Controls.Add(Mono(project, Green, 12, 37, 167, 616, 76, true));
        var sweep = new MatrixMeter();
        Place(sweep, 37, 265, 617, 11);
        body.Controls.Add(sweep);

        try
        {
            sweep.Value = 25;
            await Task.Delay(170);
            sweep.Value = 53;
            using (var launcher = Process.Start(new ProcessStartInfo(Path.Combine(installRoot, "vibecode.exe"))
            { WorkingDirectory = project, UseShellExecute = false }))
            {
                if (launcher == null) throw new IOException("Não foi possível abrir o terminal.");
                sweep.Value = 82;
                await Task.Delay(480);
                if (launcher.HasExited && launcher.ExitCode != 0) throw new IOException("O terminal não conseguiu iniciar.");
            }
            sweep.Value = 100;
            await Task.Delay(120);
            busy = false;
            Close();
        }
        catch (Exception error) { busy = false; ShowError(error.Message); ShowAccounts(); }
    }

    private string AgentPath(bool codex)
    {
        return Path.Combine(installRoot, codex ? @"Agents\Codex\bin\codex.exe" : @"Agents\Claude\claude.exe");
    }

    private async void BeginLogin(bool codex, Button button)
    {
        button.Enabled = false;
        if (codex) codexLoginRunning = true;
        else claudeLoginRunning = true;
        Label status = codex ? agentStatusCodex : agentStatusClaude;
        status.Text = "Aguardando login…";
        status.ForeColor = Muted;
        string args = codex ? "login" : "auth login" + (
            claudeMode.SelectedIndex == 1 ? " --console" : claudeMode.SelectedIndex == 2 ? " --sso" : " --claudeai");
        string problem = await Task.Run(() =>
        {
            try
            {
                using (var agent = Process.Start(new ProcessStartInfo(AgentPath(codex), args)
                { WorkingDirectory = project, UseShellExecute = true }))
                {
                    if (agent != null) agent.WaitForExit();
                }
                return null;
            }
            catch (Exception error) { return error.Message; }
        });
        if (IsDisposed || button.IsDisposed) return;
        button.Enabled = true;
        if (codex) codexLoginRunning = false;
        else claudeLoginRunning = false;
        if (problem != null) { ShowError(problem); }
        RefreshLoginAsync(codex);
    }

    private async void RefreshLoginAsync(bool codex)
    {
        bool? connected = await Task.Run(() => CheckLogin(codex));
        if (IsDisposed || (codex ? codexLoginRunning : claudeLoginRunning)) return;
        Label status = codex ? agentStatusCodex : agentStatusClaude;
        if (status == null || status.IsDisposed) return;
        status.Text = connected == true ? "Conectado" : connected == false ? "Ainda não conectado" : "Status indisponível";
        status.ForeColor = connected == true ? Green : Muted;
    }

    private bool? CheckLogin(bool codex)
    {
        try
        {
            string args = codex ? "login status" : "auth status --text";
            using (var agent = Process.Start(new ProcessStartInfo(AgentPath(codex), args)
            {
                WorkingDirectory = project, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            }))
            {
                agent.BeginOutputReadLine();
                agent.BeginErrorReadLine();
                if (!agent.WaitForExit(8000)) return null;
                return agent.ExitCode == 0;
            }
        }
        catch { return null; }
    }

    private static string Quote(string value)
    {
        return "\"" + value + new string('\\', value.Length - value.TrimEnd('\\').Length) + "\"";
    }

    private static int Pixels(int value) { return (int)Math.Round(value * scale); }

    private static void Place(Control item, int x, int y, int width, int height)
    {
        item.SetBounds(Pixels(x), Pixels(y), Pixels(width), Pixels(height));
    }

    private static Font NewFont(string name, int size, FontStyle style)
    {
        return new Font(name, size * fontScale, style);
    }

    private static Label Caption(string text, Color color, int size, int x, int y, int width, int height)
    {
        var label = new Label { Text = text, ForeColor = color, Font = NewFont("Segoe UI", size, FontStyle.Regular), UseMnemonic = false };
        Place(label, x, y, width, height);
        return label;
    }

    private static Label Mono(string text, Color color, int size, int x, int y, int width, int height, bool bold)
    {
        var label = new Label { Text = text, ForeColor = color, Font = NewFont("Consolas", size, bold ? FontStyle.Bold : FontStyle.Regular), UseMnemonic = false };
        Place(label, x, y, width, height);
        return label;
    }

    private static CheckBox MakeCheckBox(string text, bool selected, int y)
    {
        var check = new CheckBox { Text = text, Checked = selected, ForeColor = Bright, FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false };
        check.FlatAppearance.BorderColor = Green;
        check.FlatAppearance.CheckedBackColor = Color.FromArgb(24, 87, 41);
        Place(check, 37, y, 617, 32);
        return check;
    }

    private static Button MakeButton(string text, int x, int y, int width, bool primary)
    {
        var button = new Button
        {
            Text = text, FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false,
            ForeColor = primary ? Background : Green, BackColor = primary ? Green : Card,
            Font = NewFont("Segoe UI", 10, primary ? FontStyle.Bold : FontStyle.Regular)
        };
        button.FlatAppearance.BorderColor = primary ? Green : Border;
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(172, 255, 179) : Rail;
        Place(button, x, y, width, 44);
        return button;
    }

    private void ShowError(string message) { MessageBox.Show(this, message, "The Matrix", MessageBoxButtons.OK, MessageBoxIcon.Error); }
}
