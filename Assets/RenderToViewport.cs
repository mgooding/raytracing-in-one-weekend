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
    }

    private void RenderToBytes(int width, int height)
    {
        int bytesOffset = 0;

        for (int j = 0; j < height; ++j)
        {
            for (int i = 0; i < width; ++i)
            {
                float r = (float)i / (float)width;
                float g = (float) j / (float) height;
                const float b = 0.2f;

                _textureBytes[bytesOffset++] = (byte)(255.99f * r);
                _textureBytes[bytesOffset++] = (byte)(255.99f * g);
                _textureBytes[bytesOffset++] = (byte)(255.99f * b);
                _textureBytes[bytesOffset++] = 255;
            }
        }
    }
}
