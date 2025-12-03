namespace Tlumach.Extensions.Localization
{
    public interface ITlumachSettingsProvider
    {
        TlumachLocalizationOptions GetOptionsFor(string context);
    }
}
