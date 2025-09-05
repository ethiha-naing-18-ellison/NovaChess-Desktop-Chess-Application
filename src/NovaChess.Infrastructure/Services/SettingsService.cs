using System.Text.Json;
using NovaChess.Infrastructure.Interfaces;

namespace NovaChess.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly Dictionary<string, object> _settings = new();
    private readonly object _lockObject = new();
    
    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var novaChessPath = Path.Combine(appDataPath, "NovaChess");
        
        if (!Directory.Exists(novaChessPath))
            Directory.CreateDirectory(novaChessPath);
            
        _settingsPath = Path.Combine(novaChessPath, "config.json");
        LoadSettings();
    }
    
    public T? GetSetting<T>(string key, T? defaultValue = default)
    {
        lock (_lockObject)
        {
            if (_settings.TryGetValue(key, out var value))
            {
                if (value is JsonElement jsonElement)
                {
                    try
                    {
                        return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
                
                if (value is T typedValue)
                    return typedValue;
            }
            
            return defaultValue;
        }
    }
    
    public void SetSetting<T>(string key, T value)
    {
        lock (_lockObject)
        {
            _settings[key] = value!;
        }
    }
    
    public void SaveSettings()
    {
        lock (_lockObject)
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                // Atomic write using temporary file
                var tempPath = _settingsPath + ".tmp";
                File.WriteAllText(tempPath, json);
                
                if (File.Exists(_settingsPath))
                    File.Delete(_settingsPath);
                    
                File.Move(tempPath, _settingsPath);
            }
            catch (Exception ex)
            {
                // Log error - in production you'd want proper error handling
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
    
    public void LoadSettings()
    {
        lock (_lockObject)
        {
            if (!File.Exists(_settingsPath))
            {
                SetDefaults();
                return;
            }
            
            try
            {
                var json = File.ReadAllText(_settingsPath);
                var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                
                if (loadedSettings != null)
                {
                    _settings.Clear();
                    foreach (var kvp in loadedSettings)
                    {
                        _settings[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error and set defaults
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
                SetDefaults();
            }
        }
    }
    
    public void ResetToDefaults()
    {
        lock (_lockObject)
        {
            SetDefaults();
            SaveSettings();
        }
    }
    
    private void SetDefaults()
    {
        _settings.Clear();
        
        // Board appearance
        _settings["BoardTheme"] = "Wood";
        _settings["PieceSet"] = "Classic";
        _settings["ShowCoordinates"] = true;
        _settings["ShowLastMove"] = true;
        _settings["ShowLegalMoves"] = true;
        
        // Sound settings
        _settings["SoundEnabled"] = true;
        _settings["SoundVolume"] = 0.7;
        
        // Engine settings
        _settings["EnginePath"] = "";
        _settings["EngineSkillLevel"] = 10;
        _settings["EngineDepth"] = 15;
        _settings["EngineMoveTime"] = 1000;
        
        // Game settings
        _settings["DefaultTimeControl"] = "5+2";
        _settings["AutoSaveGames"] = true;
        _settings["RecentGamesCount"] = 10;
        
        // UI settings
        _settings["WindowWidth"] = 1200;
        _settings["WindowHeight"] = 800;
        _settings["WindowMaximized"] = false;
    }
}
