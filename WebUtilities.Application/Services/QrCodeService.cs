using QRCoder;
using WebUtilities.Application.Interfaces;

namespace WebUtilities.Application.Services;

public class QrCodeService : IQrCodeService
{
    public byte[] GeneratePng(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text is required to generate a QR code.", nameof(text));
        }

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(20);
    }
}
