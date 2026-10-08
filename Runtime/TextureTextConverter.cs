using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public static class TextureTextConverter
{
    public enum CompressionLevel
    {
        None,       // 100%
        Low,        // 75%
        Medium,     // 50%
        High,       // 25%
        Extreme     // 12.5%
    }

    public static string TextureToText(Texture2D texture, CompressionLevel level, bool useJpg = true, int jpgQuality = 90)
    {
        Texture2D processed = ResizeTexture(texture, level);

        byte[] imageBytes;

        if (useJpg)
            imageBytes = processed.EncodeToJPG(jpgQuality);
        else
            imageBytes = processed.EncodeToPNG();

        byte[] compressed = Compress(imageBytes);

        if (processed != texture)
            UnityEngine.Object.Destroy(processed);

        return Convert.ToBase64String(compressed);
    }

    public static Texture2D TextToTexture(string text)
    {
        byte[] compressed = Convert.FromBase64String(text);
        byte[] imageBytes = Decompress(compressed);

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(imageBytes);

        return texture;
    }

    private static Texture2D ResizeTexture(Texture2D original, CompressionLevel level)
    {
        float scale = level switch
        {
            CompressionLevel.None => 1f,
            CompressionLevel.Low => 0.75f,
            CompressionLevel.Medium => 0.5f,
            CompressionLevel.High => 0.25f,
            CompressionLevel.Extreme => 0.125f,
            _ => 1f
        };

        if (Mathf.Approximately(scale, 1f))
            return original;

        int width = Mathf.Max(1, Mathf.RoundToInt(original.width * scale));
        int height = Mathf.Max(1, Mathf.RoundToInt(original.height * scale));

        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(original, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    private static byte[] Compress(byte[] data)
    {
        using MemoryStream output = new MemoryStream();

        using (GZipStream gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal))
        {
            gzip.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

    private static byte[] Decompress(byte[] data)
    {
        using MemoryStream input = new MemoryStream(data);
        using GZipStream gzip = new GZipStream(input, CompressionMode.Decompress);
        using MemoryStream output = new MemoryStream();

        gzip.CopyTo(output);

        return output.ToArray();
    }
}

public static class UtilidadesImagen
{
    public static bool IntentarRedimensionarExacto(
        Texture2D original,
        int ancho,
        int alto,
        out Texture2D resultado,
        out string mensajeError)
    {
        resultado = null;
        mensajeError = string.Empty;

        try
        {
            if (original == null)
            {
                mensajeError = "La textura original es nula.";
                Debug.LogError(mensajeError);
                return false;
            }

            RenderTexture rt = RenderTexture.GetTemporary(
                ancho,
                alto,
                0,
                RenderTextureFormat.ARGB32);

            Graphics.Blit(original, rt);

            RenderTexture anterior = RenderTexture.active;
            RenderTexture.active = rt;

            resultado = new Texture2D(
                ancho,
                alto,
                TextureFormat.RGBA32,
                false);

            resultado.ReadPixels(
                new Rect(0, 0, ancho, alto),
                0,
                0);

            resultado.Apply();

            RenderTexture.active = anterior;
            RenderTexture.ReleaseTemporary(rt);

            return true;
        }
        catch (Exception ex)
        {
            mensajeError = $"Error redimensionando imagen: {ex.Message}";
            Debug.LogError($"{mensajeError}\n{ex}");
            return false;
        }
    }


    public static bool IntentarCargarImagen(
        string ruta,
        out Texture2D textura,
        out string mensajeError)
    {
        textura = null;
        mensajeError = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                mensajeError = "La ruta está vacía.";
                Debug.LogError(mensajeError);
                return false;
            }

            if (!File.Exists(ruta))
            {
                mensajeError = $"No existe el archivo: {ruta}";
                Debug.LogError(mensajeError);
                return false;
            }

            byte[] datos = File.ReadAllBytes(ruta);

            if (datos == null || datos.Length == 0)
            {
                mensajeError = "El archivo está vacío.";
                Debug.LogError(mensajeError);
                return false;
            }

            textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!textura.LoadImage(datos))
            {
                UnityEngine.Object.Destroy(textura);

                mensajeError = "No fue posible interpretar la imagen.";
                Debug.LogError(mensajeError);

                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            mensajeError = $"Error cargando imagen: {ex.Message}";
            Debug.LogError($"{mensajeError}\n{ex}");
            return false;
        }
    }

    public static bool EsDemasiadoGrande(
        Texture2D textura,
        int maxAncho,
        int maxAlto,
        out string mensaje)
    {
        mensaje = string.Empty;

        if (textura == null)
        {
            mensaje = "La textura es nula.";
            Debug.LogError(mensaje);
            return true;
        }

        if (textura.width > maxAncho || textura.height > maxAlto)
        {
            mensaje =
                $"La imagen es demasiado grande. " +
                $"Tamaño: {textura.width}x{textura.height}. " +
                $"Máximo permitido: {maxAncho}x{maxAlto}.";

            Debug.LogWarning(mensaje);
            return true;
        }

        return false;
    }

    public static bool IntentarRecortarCentroCuadrado(
        Texture2D original,
        out Texture2D resultado,
        out string mensajeError)
    {
        resultado = null;
        mensajeError = string.Empty;

        try
        {
            if (original == null)
            {
                mensajeError = "La textura original es nula.";
                Debug.LogError(mensajeError);
                return false;
            }

            int lado = Mathf.Min(original.width, original.height);

            int x = (original.width - lado) / 2;
            int y = (original.height - lado) / 2;

            Color[] pixeles = original.GetPixels(x, y, lado, lado);

            resultado = new Texture2D(
                lado,
                lado,
                TextureFormat.RGBA32,
                false);

            resultado.SetPixels(pixeles);
            resultado.Apply();

            return true;
        }
        catch (Exception ex)
        {
            mensajeError = $"Error recortando imagen: {ex.Message}";
            Debug.LogError($"{mensajeError}\n{ex}");
            return false;
        }
    }

    public static bool IntentarRedimensionar(
        Texture2D original,
        int tamañoMaximo,
        out Texture2D resultado,
        out string mensajeError)
    {
        resultado = null;
        mensajeError = string.Empty;

        try
        {
            if (original == null)
            {
                mensajeError = "La textura original es nula.";
                Debug.LogError(mensajeError);
                return false;
            }

            float escala = Mathf.Min(
                (float)tamañoMaximo / original.width,
                (float)tamañoMaximo / original.height);

            if (escala >= 1f)
            {
                resultado = original;
                return true;
            }

            int nuevoAncho = Mathf.RoundToInt(original.width * escala);
            int nuevoAlto = Mathf.RoundToInt(original.height * escala);

            RenderTexture rt = RenderTexture.GetTemporary(
                nuevoAncho,
                nuevoAlto,
                0,
                RenderTextureFormat.ARGB32);

            Graphics.Blit(original, rt);

            RenderTexture anterior = RenderTexture.active;
            RenderTexture.active = rt;

            resultado = new Texture2D(
                nuevoAncho,
                nuevoAlto,
                TextureFormat.RGBA32,
                false);

            resultado.ReadPixels(
                new Rect(0, 0, nuevoAncho, nuevoAlto),
                0,
                0);

            resultado.Apply();

            RenderTexture.active = anterior;
            RenderTexture.ReleaseTemporary(rt);

            return true;
        }
        catch (Exception ex)
        {
            mensajeError = $"Error redimensionando imagen: {ex.Message}";
            Debug.LogError($"{mensajeError}\n{ex}");
            return false;
        }
    }
}