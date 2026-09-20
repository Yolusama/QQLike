using System.IO;
using System.Windows.Media;

namespace QQLike.Services.Interfaces;

public interface IScreenShotsHandler
{
    public Task<string> StoreAsync(ImageSource imageSource);
    public string Store(ImageSource imageSource);
 
}