using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RenderToViewport : MonoBehaviour
{
    public Texture2D TexturePreview;

    private Texture2D _texture;
    private byte[] _textureBytes;

    private void Start()
    {
        int width = Camera.main.pixelWidth, height = Camera.main.pixelHeight;
        _texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        _textureBytes = new byte[width * height * 4];

        var spheres = new SphereRecord[]
        {
            new SphereRecord {Center = new Vector3(0.0f, 0.0f, -1.0f), Radius = 0.5f},
            new SphereRecord {Center = new Vector3(0.0f, -100.5f, -1.0f), Radius = 100.0f}
        };

        RenderToBytes(_texture.width, _texture.height, 10, spheres);
        _texture.LoadRawTextureData(_textureBytes);
        _texture.Apply();
        
        if (false)
            File.WriteAllBytes(@"D:\renderTest.png", _texture.EncodeToPNG());
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(_texture, dest);
    }

    private class HitRecord
    {
        public float T;
        public Vector3 Point;
        public Vector3 Normal;
    }

    private struct SphereRecord
    {
        public Vector3 Center;
        public float Radius;
    }

    private class MyCamera
    {
        private Vector3 _lowerLeft = new Vector3(-2.0f, -1.0f, -1.0f);
        private Vector3 _horizontal = new Vector3(4.0f, 0.0f, 0.0f);
        private Vector3 _vertical = new Vector3(0.0f, 2.0f, 0.0f);
        private Vector3 _origin = Vector3.zero;

        public Ray GetRay(float u, float v)
        {
            return new Ray(_origin, _lowerLeft + u * _horizontal + v * _vertical);
        }
    }

    private void HitSphere(SphereRecord sphere, Ray ray, float tMin, float tMax, out HitRecord hitRecord)
    {
        Vector3 oc = ray.origin - sphere.Center;
        float a = Vector3.Dot(ray.direction, ray.direction);
        float b = Vector3.Dot(oc, ray.direction);
        float c = Vector3.Dot(oc, oc) - sphere.Radius * sphere.Radius;
        float discriminant = b * b - a * c;
        if (discriminant < 0.0f)
        {
            hitRecord = null;
            return;
        }

        float temp = (-b - Mathf.Sqrt(discriminant)) / a;
        if (temp < tMax && temp > tMin)
        {
            Vector3 point = ray.GetPoint(temp);
            hitRecord = new HitRecord() { T = temp, Point = point, Normal = (point - sphere.Center) / sphere.Radius };
            return;
        }

        temp = (-b + Mathf.Sqrt(discriminant)) / a;
        if (temp < tMax && temp > tMin)
        {
            Vector3 point = ray.GetPoint(temp);
            hitRecord = new HitRecord() { T = temp, Point = point, Normal = (point - sphere.Center) / sphere.Radius };
            return;
        }

        hitRecord = null;
    }

    private void HitSphereArray(IEnumerable<SphereRecord> spheres, Ray ray, float tMin, float tMax, out HitRecord hitRecord)
    {
        float closestHit = tMax;
        hitRecord = null;

        foreach (SphereRecord sphere in spheres)
        {
            HitRecord tmpHit;
            HitSphere(sphere, ray, tMin, closestHit, out tmpHit);
            if (tmpHit != null)
            {
                hitRecord = tmpHit;
                closestHit = tmpHit.T;
            }
        }
    }

    private Vector3 ColorFromRay(Ray ray, IEnumerable<SphereRecord> spheres)
    {
        HitRecord hitRecord;
        HitSphereArray(spheres, ray, 0.0f, float.MaxValue, out hitRecord);
        if (hitRecord != null)
            return 0.5f * (hitRecord.Normal + Vector3.one);

        Vector3 normalizedDir = ray.direction.normalized;
        float t = 0.5f * (normalizedDir.y + 1.0f);
        return (1.0f - t) * Vector3.one + t * new Vector3(0.5f, 0.7f, 1.0f);
    }

    private void RenderToBytes(int width, int height, int spp, SphereRecord[] spheres)
    {
        int bytesOffset = 0;

        var myCamera = new MyCamera();

        for (int j = 0; j < height; ++j)
        {
            for (int i = 0; i < width; ++i)
            {
                Vector3 rgb = Vector3.zero;
                for (int k = 0; k < spp; ++k)
                {
                    float u = ((float)i + Random.Range(0.0f, 0.999999f)) / (float)width;
                    float v = ((float)j + Random.Range(0.0f, 0.999999f)) / (float)height;

                    rgb += ColorFromRay(myCamera.GetRay(u, v), spheres);   
                }
                rgb /= spp;

                _textureBytes[bytesOffset++] = (byte)(255.99f * rgb.x);
                _textureBytes[bytesOffset++] = (byte)(255.99f * rgb.y);
                _textureBytes[bytesOffset++] = (byte)(255.99f * rgb.z);
                _textureBytes[bytesOffset++] = 255;
            }
        }
    }
}
