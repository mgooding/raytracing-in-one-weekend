using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

public class PathTraceScene : MonoBehaviour
{
    private Camera _camera;

    private Texture2D _texture;
    private byte[] _textureBytes;

    private Dictionary<Transform, MyMaterial> _materials;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _materials = new Dictionary<Transform, MyMaterial>();
    }

    private void Start()
    {
        int width = _camera.pixelWidth, height = _camera.pixelHeight;
        _texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        _textureBytes = new byte[width * height * 4];

        const int samplesPerPixel = 1;

        DateTime start = DateTime.UtcNow;
        RenderToBytes(samplesPerPixel);
        Debug.Log(DateTime.UtcNow - start);

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
        public Vector3 Point;
        public Vector3 Normal;
        public MyMaterial Material;
    }

    private enum MyMaterialType
    {
        Lambertian,
        Metal,
        Dielectric,
    }

    private class MyMaterial
    {
        public MyMaterialType MaterialType;
        public Vector3 Albedo;
        public float Fuzz;
        public float RefractionIndex;

        public bool Scatter(Ray ray, HitRecord hitRecord, ref Vector3 attenuation, ref Ray scattered)
        {
            switch (MaterialType)
            {
                case MyMaterialType.Lambertian:
                {
                    Vector3 target = hitRecord.Point + hitRecord.Normal + Random.insideUnitSphere;
                    scattered = new Ray(hitRecord.Point, target - hitRecord.Point);
                    attenuation = Albedo;
                    return true;
                }
                case MyMaterialType.Metal:
                {
                    Vector3 reflection = Vector3.Reflect(ray.direction.normalized, hitRecord.Normal);
                    scattered = new Ray(hitRecord.Point, reflection + Fuzz * Random.insideUnitSphere);
                    attenuation = Albedo;
                    return Vector3.Dot(scattered.direction, hitRecord.Normal) > 0.0f;
                }
                case MyMaterialType.Dielectric:
                {
                    Vector3 outwardNormal;
                    Vector3 reflection = Vector3.Reflect(ray.direction, hitRecord.Normal);
                    float niOverNt;
                    attenuation = Vector3.one;
                    Vector3 refraction = Vector3.zero;
                    float cosine;

                    if (Vector3.Dot(ray.direction, hitRecord.Normal) > 0.0f)
                    {
                        outwardNormal = hitRecord.Normal * -1.0f;
                        niOverNt = RefractionIndex;
                        cosine = RefractionIndex * Vector3.Dot(ray.direction, hitRecord.Normal) /
                                 ray.direction.magnitude;
                    }
                    else
                    {
                        outwardNormal = hitRecord.Normal;
                        niOverNt = 1.0f / RefractionIndex;
                        cosine = -Vector3.Dot(ray.direction, hitRecord.Normal) / ray.direction.magnitude;
                    }

                    float reflectionProb;
                    if (Refract(ray.direction, outwardNormal, niOverNt, ref refraction))
                        reflectionProb = Schlick(cosine);
                    else
                        reflectionProb = 1.0f;

                    if (Random.Range(0.0f, 0.999999f) < reflectionProb)
                        scattered = new Ray(hitRecord.Point, reflection);
                    else
                        scattered = new Ray(hitRecord.Point, refraction);

                    return true;
                }
            }

            return false;
        }

        private float Schlick(float cosine)
        {
            float r0 = (1 - RefractionIndex) / (1 + RefractionIndex);
            r0 = r0 * r0;
            return r0 + (1 - r0) * Mathf.Pow(1.0f - cosine, 5.0f);
        }

        private bool Refract(Vector3 vector, Vector3 normal, float niOverNt, ref Vector3 refracted)
        {
            vector.Normalize();
            float dt = Vector3.Dot(vector, normal);
            float discriminant = 1.0f - niOverNt * niOverNt * (1 - dt * dt);
            if (discriminant <= 0.0f)
                return false;

            refracted = niOverNt * (vector - normal * dt) - normal * Mathf.Sqrt(discriminant);
            return true;
        }
    }

    private MyMaterial FindMaterial(Transform xform)
    {
        MyMaterial result;
        if (_materials.TryGetValue(xform, out result))
            return result;

        string objectName = xform.name;
        switch (objectName)
        {
            case "default": // bunny
                result = new MyMaterial()
                {
                    MaterialType = MyMaterialType.Metal,
                    Albedo = new Vector3(0.95f, 0.95f, 0.95f),
                    Fuzz = 0.15f,
                };
                break;
            case "EthanGlasses":
            case "EthanBody":
                result = new MyMaterial()
                {
                    MaterialType = MyMaterialType.Lambertian,
                    Albedo = new Vector3(0.35f, 0.35f, 0.95f)
                };
                break;
            case "Plane":
                result = new MyMaterial()
                {
                    MaterialType = MyMaterialType.Metal,
                    Albedo = new Vector3(0.95f, 0.45f, 0.45f),
                    Fuzz = 0.85f,
                };
                break;
            case "Sphere":
                result = new MyMaterial()
                {
                    MaterialType = MyMaterialType.Dielectric,
                    RefractionIndex = 1.5f,
                };
                break;
            default:
                result = new MyMaterial()
                {
                    MaterialType = MyMaterialType.Lambertian,
                    Albedo = new Vector3(0.65f, 0.35f, 0.15f),
                };
                break;
        }

        _materials.Add(xform, result);
        return result;
    }

    private void HitColliders(Ray ray, float tMin, float tMax, out HitRecord hitRecord)
    {
        RaycastHit hitInfo;
        ray.origin += ray.direction * tMin;
        if (!Physics.Raycast(ray, out hitInfo, tMax - tMin))
        {
            hitRecord = null;
            return;
        }

        hitRecord = new HitRecord()
        {
            Material = FindMaterial(hitInfo.transform),
            Normal = hitInfo.normal,
            Point = hitInfo.point,
        };
    }

    private Vector3 ColorFromRay(Ray ray, int depth)
    {
        HitRecord hitRecord;
        HitColliders(ray, 0.001f, float.MaxValue, out hitRecord);
        if (hitRecord != null)
        {
            var scattered = new Ray();
            Vector3 attenuation = Vector3.zero;
            if (depth < 50 && hitRecord.Material.Scatter(ray, hitRecord, ref attenuation, ref scattered))
                return Vector3.Scale(attenuation, ColorFromRay(scattered, depth + 1));

            return Vector3.zero;
        }

        Vector3 normalizedDir = ray.direction.normalized;
        float t = 0.5f * (normalizedDir.y + 1.0f);
        return (1.0f - t) * Vector3.one + t * new Vector3(0.5f, 0.7f, 1.0f);
    }

    private void RenderToBytes(int spp)
    {
        int bytesOffset = 0;

        int height = _camera.pixelHeight;
        int width = _camera.pixelWidth;

        for (int j = 0; j < height; ++j)
        {
            for (int i = 0; i < width; ++i)
            {
                Vector3 rgb = Vector3.zero;
                for (int k = 0; k < spp; ++k)
                {
                    float u = ((float)i + Random.Range(0.0f, 0.999999f)) / (float)width;
                    float v = ((float)j + Random.Range(0.0f, 0.999999f)) / (float)height;

                    rgb += ColorFromRay(_camera.ViewportPointToRay(new Vector3(u, v, 0.0f)), 0);
                }
                rgb /= spp;
                rgb = new Vector3(Mathf.Sqrt(rgb.x), Mathf.Sqrt(rgb.y), Mathf.Sqrt(rgb.z));

                _textureBytes[bytesOffset++] = (byte)(255.99f * rgb.x);
                _textureBytes[bytesOffset++] = (byte)(255.99f * rgb.y);
                _textureBytes[bytesOffset++] = (byte)(255.99f * rgb.z);
                _textureBytes[bytesOffset++] = 255;
            }
        }
    }
}
