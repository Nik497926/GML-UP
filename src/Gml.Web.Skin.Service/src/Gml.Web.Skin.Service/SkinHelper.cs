using Gml.Web.Skin.Service.Models;
using Gml.Web.Skin.Service.Models.Dto;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Gml.Web.Skin.Service;

public abstract class SkinHelper
{
    private const string DefaultSkinName = "default.png";

    public static string SkinTextureDirectory { get; } =
        Path.Combine(Environment.CurrentDirectory, "Storage", "Skins");

    public static string CloakTextureDirectory { get; } =
        Path.Combine(Environment.CurrentDirectory, "Storage", "Cloak");

    public static UserTexture Create(string requestPathBase, string userName)
    {
        var skinPath = Path.Combine(SkinTextureDirectory, $"{userName}.png");
        var cloakPath = Path.Combine(CloakTextureDirectory, $"{userName}.png");

        if (!File.Exists(skinPath))
            skinPath = Path.Combine(SkinTextureDirectory, DefaultSkinName);

        if (!File.Exists(cloakPath))
            cloakPath = string.Empty;

        var texture = new UserTexture
        {
            UserName = userName,
            HasCloak = !string.IsNullOrEmpty(cloakPath),
            HasSkin = !skinPath.EndsWith(DefaultSkinName),
            SkinUrl = $"{requestPathBase}/skin/s-{userName}",
            SkinFullPath = skinPath.EndsWith(DefaultSkinName)
                ? Path.Combine(Environment.CurrentDirectory, SkinTextureDirectory, "default.png")
                : Path.Combine(Environment.CurrentDirectory, SkinTextureDirectory, $"{userName}.png"),
            CloakFullPath = string.IsNullOrEmpty(cloakPath)
                ? string.Empty
                : Path.Combine(Environment.CurrentDirectory, CloakTextureDirectory, $"{userName}.png"),
            ClockUrl = $"{requestPathBase}/cloak/c-{userName}",
            Texture = new SkinPartialsDto
            {
                Head = $"{requestPathBase}/skin/{userName}/head/128",
                Front = $"{requestPathBase}/skin/{userName}/front/128",
                Back = $"{requestPathBase}/skin/{userName}/back/128",
                CloakBack = $"{requestPathBase}/skin/{userName}/full-back/128",
                Cloak = $"{requestPathBase}/cloak/{userName}/128"
            }
        };

        if (!texture.HasSkin) return texture;
        
        using var inputImage = Image.Load<Rgba32>(texture.SkinFullPath);
        texture.SkinFormat = GetSkinFormat(inputImage.Width);
        texture.SkinType = GetSkinType(inputImage);
        
        return texture;
    }

    private static SkinFormat GetSkinFormat(double width)
    {
        return width switch
        {
            > 64 and < 1024 => SkinFormat.SkinHD,
            > 512 => SkinFormat.SkinFullHD,
            _ => SkinFormat.SkinSD
        };
    }

    private static SkinType GetSkinType(Image<Rgba32> image)
    {
        var scaleFactor = image.Width / 64;
        
        // Проверяем области рук на втором слое (layer 1)
        // В slim скине руки тоньше (3 пикселя вместо 4)
        
        // Правая рука (слой 1)
        var rightArmRegion = new Rectangle(44 * scaleFactor, 20 * scaleFactor, 4 * scaleFactor, 12 * scaleFactor);
        // Левая рука (слой 1)
        var leftArmRegion = new Rectangle(36 * scaleFactor, 52 * scaleFactor, 4 * scaleFactor, 12 * scaleFactor);

        bool hasPixelsInRightArm = false;
        bool hasPixelsInLeftArm = false;

        // Проверяем правую руку
        for (int x = rightArmRegion.X + 3 * scaleFactor; x < rightArmRegion.X + 4 * scaleFactor; x++)
        {
            for (int y = rightArmRegion.Y; y < rightArmRegion.Y + rightArmRegion.Height; y++)
            {
                if (image[x, y].A > 0)
                {
                    hasPixelsInRightArm = true;
                    break;
                }
            }
            if (hasPixelsInRightArm) break;
        }

        // Если есть расширенный скин (64x64), проверяем левую руку
        if (image.Height >= 64 * scaleFactor)
        {
            for (int x = leftArmRegion.X + 3 * scaleFactor; x < leftArmRegion.X + 4 * scaleFactor; x++)
            {
                for (int y = leftArmRegion.Y; y < leftArmRegion.Y + leftArmRegion.Height; y++)
                {
                    if (image[x, y].A > 0)
                    {
                        hasPixelsInLeftArm = true;
                        break;
                    }
                }
                if (hasPixelsInLeftArm) break;
            }
        }

        // Если хотя бы в одной из областей есть непрозрачные пиксели в 4-м столбце - это обычный скин
        // Если обе области пустые в 4-м столбце - это slim скин
        return (!hasPixelsInRightArm && (!image.Height.Equals(64 * scaleFactor) || !hasPixelsInLeftArm)) 
            ? SkinType.Slim 
            : SkinType.Default;
    }

    public static string GetActualSkinPath(string userName, UserTexture user)
    {
        var imagePath = Path.Combine(SkinTextureDirectory, $"{userName}.png");

        if (!user.HasSkin) imagePath = Path.Combine(SkinTextureDirectory, "default.png");

        return imagePath;
    }
}
