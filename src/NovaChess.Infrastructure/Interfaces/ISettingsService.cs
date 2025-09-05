namespace NovaChess.Infrastructure.Interfaces;

public interface ISettingsService
{
    T? GetSetting<T>(string key, T? defaultValue = default);
    void SetSetting<T>(string key, T value);
    void SaveSettings();
    void LoadSettings();
    void ResetToDefaults();
}
