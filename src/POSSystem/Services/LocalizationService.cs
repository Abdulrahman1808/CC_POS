using System;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace POSSystem.Services;

/// <summary>
/// Service for managing application localization.
/// </summary>
public partial class LocalizationService : ObservableObject
{
    private static LocalizationService? _instance;
    private static readonly object _lock = new();
    
    private readonly ResourceManager _resourceManager;
    
    /// <summary>
    /// Gets the singleton instance of the localization service.
    /// </summary>
    public static LocalizationService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new LocalizationService();
                }
            }
            return _instance;
        }
    }

    [ObservableProperty]
    private string _currentLanguage = "en";
    
    [ObservableProperty]
    private FlowDirection _flowDirection = FlowDirection.LeftToRight;

    /// <summary>
    /// Event raised when the language changes.
    /// </summary>
    public event EventHandler? LanguageChanged;

    private LocalizationService()
    {
        _resourceManager = new ResourceManager(
            "POSSystem.Resources.Strings",
            typeof(LocalizationService).Assembly);
        
        // Try to load saved language preference
        var savedLanguage = Environment.GetEnvironmentVariable("POS_LANGUAGE") ?? "en";
        SetLanguage(savedLanguage);
    }

    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    public string GetString(string key)
    {
        try
        {
            var value = _resourceManager.GetString(key, CultureInfo.CurrentUICulture);
            return value ?? key;
        }
        catch
        {
            Debug.WriteLine($"[i18n] Missing key: {key}");
            return key;
        }
    }

    /// <summary>
    /// Indexer for easy access to localized strings.
    /// </summary>
    public string this[string key] => GetString(key);

    /// <summary>
    /// Sets the current language.
    /// </summary>
    /// <param name="languageCode">Language code (e.g., "en", "ar")</param>
    public void SetLanguage(string languageCode)
    {
        try
        {
            var culture = new CultureInfo(languageCode);
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            
            CurrentLanguage = languageCode;
            
            // Set flow direction for RTL languages
            FlowDirection = languageCode.StartsWith("ar") || languageCode.StartsWith("he")
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
            
            Debug.WriteLine($"[i18n] Language set to: {languageCode}, FlowDirection: {FlowDirection}");
            
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[i18n] Error setting language: {ex.Message}");
        }
    }

    /// <summary>
    /// Available languages.
    /// </summary>
    public static readonly (string Code, string Name)[] AvailableLanguages =
    {
        ("en", "English"),
        ("ar", "العربية (Arabic)")
    };
}

/// <summary>
/// XAML markup extension for localized strings.
/// Usage: Text="{loc:Localize Dashboard}"
/// </summary>
public class LocalizeExtension : System.Windows.Markup.MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocalizeExtension() { }
    
    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return LocalizationService.Instance.GetString(Key);
    }
}
