local wezterm = require 'wezterm'
local config = wezterm.config_builder()
local act = wezterm.action
local root = wezterm.config_dir
wezterm.add_to_config_reload_watch_list(root .. '/matrix-theme.lua')

config.default_prog = {
  root .. '/PowerShell/pwsh.exe',
  '-NoLogo', '-NoProfile', '-ExecutionPolicy', 'RemoteSigned', '-NoExit',
  '-File', root .. '/shell.ps1',
}
config.font_dirs = { root .. '/Fonts' }
config.font = wezterm.font('JetBrainsMono Nerd Font Mono')
config.font_size = 11.5
config.initial_cols = 120
config.initial_rows = 34
config.scrollback_lines = 12000
config.window_background_opacity = 1.0
config.text_background_opacity = 1.0
config.win32_system_backdrop = 'Disable'
config.enable_scroll_bar = false
config.cursor_blink_rate = 650
config.default_cursor_style = 'BlinkingBlock'
config.window_padding = { left = 15, right = 15, top = 10, bottom = 10 }
config.background = {
  { source = { Color = '#000000' }, width = '100%', height = '100%', opacity = 1.0 },
  {
    source = { File = root .. '/matrix-crt.png' },
    width = '100%',
    height = '100%',
    opacity = 1.0,
    repeat_x = 'NoRepeat',
    repeat_y = 'NoRepeat',
  },
}
config.inactive_pane_hsb = { saturation = 1.0, brightness = 0.94 }
config.use_fancy_tab_bar = false
config.tab_max_width = 32
config.colors = {
  foreground = '#8EFF9C',
  background = '#000000',
  cursor_bg = '#53FF65',
  cursor_fg = '#000000',
  cursor_border = '#53FF65',
  selection_fg = '#D3FFCE',
  selection_bg = '#164922',
  split = '#226533',
  scrollbar_thumb = '#000000',
  ansi = { '#000000', '#98CD68', '#5AFF75', '#A4E876', '#6ABD84', '#8ED58D', '#62D9A2', '#98EAA0' },
  brights = { '#4F885C', '#C6F590', '#7FFF8F', '#CDF9A0', '#8FE8AA', '#A8EDAA', '#91F5BC', '#C6FFD0' },
  tab_bar = {
    background = '#07110A',
    active_tab = { bg_color = '#12351E', fg_color = '#A1FFA9', intensity = 'Bold' },
    inactive_tab = { bg_color = '#07110A', fg_color = '#6BA977' },
    inactive_tab_hover = { bg_color = '#102A18', fg_color = '#A1FFA9' },
    new_tab = { bg_color = '#07110A', fg_color = '#6BA977' },
    new_tab_hover = { bg_color = '#12351E', fg_color = '#A1FFA9' },
  },
}
local function pane_folder(pane)
  local url = pane:get_current_working_dir()
  if url and url.scheme == 'file' and url.file_path then
    -- File URLs encode the drive as /C:/ on some WezTerm builds.
    return (url.file_path:gsub('^/([A-Za-z]:/)', '%1'))
  end
  return wezterm.home_dir
end

local help_tab = wezterm.action_callback(function(window, pane)
  local args = {}
  for i, value in ipairs(config.default_prog) do
    args[i] = value
  end
  args[#args + 1] = '-MatrixHelp'
  window:perform_action(act.SpawnCommandInNewTab { args = args, cwd = pane_folder(pane) }, pane)
end)

local folder_picker = wezterm.action_callback(function(_, pane)
  wezterm.background_child_process { root .. '/vibecode.exe', '--pick', pane_folder(pane) }
end)

config.keys = {
  { key = 'o', mods = 'CTRL|SHIFT', action = folder_picker },
  { key = 'h', mods = 'CTRL|ALT', action = help_tab },
  { key = 'F11', mods = 'NONE', action = act.ToggleFullScreen },
  { key = 'LeftArrow', mods = 'CTRL|ALT', action = act.ActivatePaneDirection 'Left' },
  { key = 'RightArrow', mods = 'CTRL|ALT', action = act.ActivatePaneDirection 'Right' },
}
config.status_update_interval = 15000
wezterm.on('update-status', function(window)
  window:set_left_status(wezterm.format {
    { Foreground = { Color = '#69FF80' } },
    { Attribute = { Intensity = 'Bold' } },
    { Text = '  THE MATRIX  ' },
    { Foreground = { Color = '#326440' } },
    { Text = ' /  ' },
  })
  window:set_right_status(wezterm.format {
    { Foreground = { Color = '#72B67D' } },
    { Text = '  ' .. wezterm.strftime('%H:%M') .. '  /  AJUDA Ctrl+Alt+H  ' },
  })
end)
wezterm.on('format-window-title', function()
  return 'The Matrix'
end)

return config
