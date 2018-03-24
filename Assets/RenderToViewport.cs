using UnityEngine;

public class RenderToViewport : MonoBehaviour
{
    public Texture2D TexturePreview;

    private Texture2D _texture;
    private byte[] _textureBytes;

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (_texture == null || _texture.width != src.width || _texture.height != src.height)
        {
            _texture = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false, false);
            _textureBytes = new byte[src.width * src.height * 4];
        }

        RenderToBytes(_texture.width, _texture.height);
        _texture.LoadRawTextureData(_textureBytes);
        _texture.Apply();

        Graphics.Blit(_texture, dest);

        Debug.Break(); // so slow, just break after a frame to maintain interactivity in editor
    }

    private Vector3 ColorFromRay(Ray ray)
    {
        Vector3 normalizedDir = ray.direction.normalized;
        float t = 0.5f * (normalizedDir.y + 1.0f);
        return (1.0f - t) * Vector3.one + t * new Vector3(0.5f, 0.7f, 1.0f);
    }

    private void RenderToBytes(int width, int height)
    {
        int bytesOffset = 0;

        Vector3 lowerLeft = new Vector3(-2.0f, -1.0f, -1.0f);
        Vector3 horizontal = new Vector3(4.0f, 0.0f, 0.0f);
        Vector3 vertical = new Vector3(0.0f, 2.0f, 0.0f);
        Vector3 origin = Vector3.zero;

        for (int j = 0; j < height; ++j)
        {
            for (int i = 0; i < width; ++i)
            {
                float u = (float) i / (float) width;
                float v = (float) j / (float) height;

                Vector3 rgb = ColorFromRay(new Ray(origin, lowerLeft + u * horizontal + v * vertical));

                _textureBytes[bytesOffset++] = (byte)(255.99f * rgb.x);
                _textureBytes[bytesOffset++] = (byte)(255.99f * rgb.y);
                _textureBytes[bytesOffset++] = (byte)(255.99f * rgb.z);
                _textureBytes[bytesOffset++] = 255;
            }
        }
    }
}
