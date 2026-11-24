local wezterm = require 'wezterm'
local config = dofile(wezterm.config_dir .. '/matrix-theme.lua')

wezterm.on('gui-startup', function(cmd)
  local _, _, window = wezterm.mux.spawn_window(cmd or {})
  window:gui_window():toggle_fullscreen()
end)

return config
