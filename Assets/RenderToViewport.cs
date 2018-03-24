using UnityEngine;

public class RenderToViewport : MonoBehaviour
{
    private Texture2D _texture;
    private byte[] _textureBytes;

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (_texture == null)
        {
            _texture = new Texture2D(src.width, src.height, TextureFormat.ARGB32, false, false);
            _textureBytes = new byte[src.width * src.height * 4];
        }

        _texture.LoadRawTextureData(_textureBytes);
        Graphics.Blit(_texture, dest);
    }
}
