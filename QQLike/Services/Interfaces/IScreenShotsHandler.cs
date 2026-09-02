using System.Windows.Media;

namespace QQLike.Services.Interfaces;

public interface IScreenShotsHandler
{
    public Task Store(ImageSource imageSource);
}