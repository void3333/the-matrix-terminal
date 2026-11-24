local wezterm = require 'wezterm'
local root = wezterm.config_dir
local config = dofile(root .. '/matrix-theme.lua')
config.initial_cols = 144

local function agent_shell(agent)
  return {
    root .. '/PowerShell/pwsh.exe',
    '-NoLogo', '-NoProfile', '-ExecutionPolicy', 'RemoteSigned', '-NoExit',
    '-File', root .. '/shell.ps1', '-Agent', agent,
  }
end

wezterm.on('gui-startup', function(cmd)
  local cwd = os.getenv('THE_MATRIX_LAUNCH_CWD') or (cmd and cmd.cwd) or wezterm.home_dir
  local tab, codex, window = wezterm.mux.spawn_window {
    cwd = cwd,
    args = agent_shell('Codex'),
  }
  codex:split { direction = 'Right', cwd = cwd, args = agent_shell('Claude') }
  tab:set_title('Codex  |  Claude')
  codex:activate()
  window:gui_window():toggle_fullscreen()
end)

return config
