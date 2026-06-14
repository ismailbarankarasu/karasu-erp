using System.Security.Cryptography;
using System.Text;
using Karasu.ERP.Application.Common.Interfaces;
using QRCoder;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;
using SkiaSharp;

namespace Karasu.ERP.Infrastructure.Services;

public class BarcodeService : IBarcodeService
{
    public string GenerateUniqueBarcode(Guid tenantId, string sku)
    {
        var seed = $"{tenantId:N}{sku}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var numeric = BitConverter.ToUInt32(hash, 0) % 1_000_000_0000UL;
        var body = numeric.ToString("D10");
        var prefix = "869";
        var withoutCheck = prefix + body;
        var checkDigit = CalculateEan13CheckDigit(withoutCheck);
        return withoutCheck + checkDigit;
    }

    public string GenerateQrCodeBase64(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        var bytes = qrCode.GetGraphic(8);
        return Convert.ToBase64String(bytes);
    }

    public string GenerateBarcodeImageBase64(string barcode) =>
        GenerateBarcodeImageBase64Internal(barcode);

    private static string GenerateBarcodeImageBase64Internal(string barcode)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Height = 80,
                Width = 280,
                Margin = 2,
                PureBarcode = true
            }
        };

        using var bitmap = writer.Write(barcode);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToBase64String(encoded.ToArray());
    }

    private static char CalculateEan13CheckDigit(string digits12)
    {
        var sum = 0;
        for (var i = 0; i < digits12.Length; i++)
        {
            var digit = digits12[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        var check = (10 - sum % 10) % 10;
        return (char)('0' + check);
    }
}
