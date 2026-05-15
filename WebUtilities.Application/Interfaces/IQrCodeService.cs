namespace WebUtilities.Application.Interfaces;

public interface IQrCodeService
{
    byte[] GeneratePng(string text);
}
