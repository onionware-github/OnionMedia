using System.Drawing;
using OnionMedia.Core.Models;

namespace OnionMedia.Core.Services;

public interface IThemingService
{
    void SetTheme(Theme theme);
    void SetThemeType(ThemeType theme);
    void SetAccentColor(Color color);
    event EventHandler<Theme> ThemeChanged;
}
public record Theme(ThemeType ThemeType, Color Color);