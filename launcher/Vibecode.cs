using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("The Matrix")]
[assembly: AssemblyProduct("The Matrix")]

internal static class Launcher
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            Application.EnableVisualStyles();
            string folder = SelectFolder(args);
            if (folder == null) return 0;
            string root = AppDomain.CurrentDomain.BaseDirectory;
            if (!File.Exists(Path.Combine(root, "agents.lua")))
                root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\MatrixTerminal");

            string config = Path.Combine(root, "agents.lua");
            string wezterm = Path.Combine(root, @"WezTerm\wezterm-gui.exe");
            if (!File.Exists(wezterm) || !File.Exists(config))
                throw new FileNotFoundException("A instalação do The Matrix não foi encontrada.");

            var start = new ProcessStartInfo
            {
                FileName = wezterm,
                WorkingDirectory = folder,
                UseShellExecute = false,
                Arguments = "--config-file " + Quote(config) + " start --always-new-process --cwd " + Quote(folder)
            };
            start.EnvironmentVariables["THE_MATRIX_LAUNCH_CWD"] = folder;
            start.EnvironmentVariables.Remove("WEZTERM_UNIX_SOCKET");
            using (Process.Start(start)) { }
            return 0;
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "The Matrix", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    private static string SelectFolder(string[] args)
    {
        if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h"))
        {
            MessageBox.Show("vibecode             Abre na pasta atual.\r\nvibecode <pasta>  Abre em outra pasta.\r\nvibecode --pick    Abre o seletor de pastas.\r\n\r\nCtrl+Alt+H abre a ajuda dentro do terminal.", "The Matrix");
            return null;
        }

        bool pick = args.Length > 0 && args[0] == "--pick";
        int folderIndex = pick ? 1 : 0;
        if (args.Length > folderIndex + 1) throw new ArgumentException("Use vibecode <pasta> ou vibecode --pick.");
        string folder = args.Length > folderIndex
            ? Path.GetFullPath(Environment.ExpandEnvironmentVariables(args[folderIndex]))
            : Directory.GetCurrentDirectory();

        if (pick)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Escolha a pasta para o Codex e o Claude Code";
                dialog.SelectedPath = Directory.Exists(folder) ? folder : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                dialog.ShowNewFolderButton = false;
                return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
            }
        }

        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException("Pasta não encontrada:\r\n" + folder);
        return folder;
    }

    internal static string Quote(string value)
    {
        int trailing = value.Length - value.TrimEnd('\\').Length;
        return "\"" + value + new string('\\', trailing) + "\"";
    }
}
