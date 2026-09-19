using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using QQLike.Services.Interfaces;

namespace QQLike.Services;

public class ScreenShotsHandler(
    IProjectLogger logger,
    ISessionStorage sessionStorage,
    IRandomGenerator generator,
    SysSetting setting) : IScreenShotsHandler
{
    private const string ScreenShots = nameof(ScreenShots);
   
    public async Task Store(ImageSource imageSource)
    {
        var encoder = new PngBitmapEncoder();
        try
        {
            var user = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
            if (imageSource is not BitmapSource) throw new Exception("无效的图像源");
            var fileName = $"{ScreenShots}/{user.Account}/{generator.Guid}.png";
            var storePath = Path.Combine(setting.FileStorePath, fileName);
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imageSource));
            await using var fileStream = new FileStream(storePath, FileMode.Create,FileAccess.Write, FileShare.ReadWrite);
            encoder.Save(fileStream);
            await fileStream.FlushAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"保存截图出现异常:{e}", "截图保存");
        }
    }
}