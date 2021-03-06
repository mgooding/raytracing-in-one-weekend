﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

public class RenderToViewport : MonoBehaviour
{
    private Texture2D _texture;
    private byte[] _textureBytes;

    private void Start()
    {
        int width = Camera.main.pixelWidth, height = Camera.main.pixelHeight;
        _texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        _textureBytes = new byte[width * height * 4];

        SphereRecord[] scene = GetRandomScene();

        DateTime start = DateTime.UtcNow;
        RenderToBytes(_texture.width, _texture.height, 10, scene);
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

    private SphereRecord[] GetRandomScene()
    {
        var result = new List<SphereRecord>
        {
            new SphereRecord // ground plane
            {
                Center = new Vector3(0, -1000.0f, 0.0f),
                Radius = 1000.0f,
                Material = new MyMaterial
                {
                    MaterialType = MyMaterialType.Lambertian,
                    Albedo = new Vector3(0.5f, 0.5f, 0.5f)
                }
            }
        };

        var centerTest = new Vector3(4.0f, 0.2f, 0.0f);

        for (int a = -11; a < 11; ++a)
        {
            for (int b = -11; b < 11; ++b)
            {
                float chooseMat = Random.value;
                var center = new Vector3(a + 0.9f*Random.value, 0.2f, b + 0.9f * Random.value);
                if ((center - centerTest).magnitude > 0.9f)
                {
                    if (chooseMat < 0.8f)
                    {
                        result.Add(new SphereRecord()
                        {
                            Center = center,
                            Radius = 0.2f,
                            Material = new MyMaterial()
                            {
                                MaterialType = MyMaterialType.Lambertian,
                                Albedo = new Vector3(Random.value * Random.value, Random.value * Random.value, Random.value * Random.value),
                            }
                        });
                    }
                    else if (chooseMat < 0.95f)
                    {
                        result.Add(new SphereRecord()
                        {
                            Center = new Vector3(4.0f, 1.0f, 0.0f),
                            Radius = 0.2f,
                            Material = new MyMaterial()
                            {
                                MaterialType = MyMaterialType.Metal,
                                Albedo = new Vector3(0.5f * (1.0f + Random.value), 0.5f * (1.0f + Random.value), 0.5f * (1.0f + Random.value)),
                                Fuzz = 0.5f * Random.value,
                            }
                        });
                    }
                    else
                    {
                        result.Add(new SphereRecord()
                        {
                            Center = Vector3.up,
                            Radius = 0.2f,
                            Material = new MyMaterial()
                            {
                                MaterialType = MyMaterialType.Dielectric,
                                RefractionIndex = 1.5f,
                            }
                        });
                    }
                }
            }
        }

        result.Add(new SphereRecord()
        {
            Center = Vector3.up,
            Radius = 1.0f,
            Material = new MyMaterial()
            {
                MaterialType = MyMaterialType.Dielectric,
                RefractionIndex = 1.5f,
            }
        });

        result.Add(new SphereRecord()
        {
            Center = new Vector3(-4.0f, 1.0f, 0.0f),
            Radius = 1.0f,
            Material = new MyMaterial()
            {
                MaterialType = MyMaterialType.Lambertian,
                Albedo = new Vector3(0.4f, 0.2f, 0.1f),
            }
        });

        result.Add(new SphereRecord()
        {
            Center = new Vector3(4.0f, 1.0f, 0.0f),
            Radius = 1.0f,
            Material = new MyMaterial()
            {
                MaterialType = MyMaterialType.Metal,
                Albedo = new Vector3(0.7f, 0.6f, 0.5f),
                Fuzz = 0.0f,
            }
        });

        return result.ToArray();
    }

    private SphereRecord[] GetDefaultScene()
    {
        return new SphereRecord[]
        {
            new SphereRecord
            {
                Center = new Vector3(0.0f, 0.0f, -1.0f),
                Radius = 0.5f,
                Material = new MyMaterial
                {
                    MaterialType = MyMaterialType.Lambertian,
                    Albedo = new Vector3(0.1f, 0.2f, 0.5f)
                }
            },
            new SphereRecord
            {
                Center = new Vector3(0.0f, -100.5f, -1.0f),
                Radius = 100.0f,
                Material = new MyMaterial
                {
                    MaterialType = MyMaterialType.Lambertian,
                    Albedo = new Vector3(0.8f, 0.8f, 0.0f)
                }
            },
            new SphereRecord
            {
                Center = new Vector3(1.0f, 0.0f, -1.0f),
                Radius = 0.5f,
                Material = new MyMaterial
                {
                    MaterialType = MyMaterialType.Metal,
                    Albedo = new Vector3(0.8f, 0.6f, 0.2f),
                    Fuzz = 0.0f
                }
            },
            new SphereRecord
            {
                Center = new Vector3(-1.0f, 0.0f, -1.0f),
                Radius = 0.5f,
                Material = new MyMaterial
                {
                    MaterialType = MyMaterialType.Dielectric,
                    RefractionIndex = 1.5f,
                }
            },
            new SphereRecord
            {
                Center = new Vector3(-1.0f, 0.0f, -1.0f),
                Radius = -0.45f,
                Material = new MyMaterial
                {
                    MaterialType = MyMaterialType.Dielectric,
                    RefractionIndex = 1.5f,
                }
            }
        };
    }

    private class HitRecord
    {
        public float T;
        public Vector3 Point;
        public Vector3 Normal;
        public MyMaterial Material;
    }

    private struct SphereRecord
    {
        public Vector3 Center;
        public float Radius;
        public MyMaterial Material;
    }

    private class MyCamera
    {
        private Vector3 _lowerLeft;
        private Vector3 _horizontal;
        private Vector3 _vertical;
        private Vector3 _origin;
        private Vector3 _u;
        private Vector3 _v;
        private Vector3 _w;
        private float _lensRadius;

        public MyCamera(Vector3 lookFrom, Vector3 lookAt, Vector3 up, float vfov, float aspect, float aperture, float focusDist)
        {
            _lensRadius = aperture / 2.0f;

            float theta = vfov * Mathf.Deg2Rad;
            float halfHeight = Mathf.Tan(theta / 2.0f);
            float halfWidth = aspect * halfHeight;

            _origin = lookFrom;

            _w = (lookFrom - lookAt).normalized;
            _u = Vector3.Cross(up, _w).normalized;
            _v = Vector3.Cross(_w, _u);

            _lowerLeft = _origin - halfWidth * focusDist * _u - halfHeight * focusDist * _v - focusDist * _w;
            _horizontal = 2.0f * halfWidth * focusDist * _u;
            _vertical = 2.0f * halfHeight * focusDist * _v;
        }

        public Ray GetRay(float s, float t)
        {
            Vector3 rd = _lensRadius * Random.insideUnitCircle;
            Vector3 offset = _u * rd.x + _v * rd.y;
            return new Ray(_origin + offset, _lowerLeft + s * _horizontal + t * _vertical - _origin - offset);
        }
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
            hitRecord = new HitRecord()
            {
                T = temp,
                Point = point,
                Normal = (point - sphere.Center) / sphere.Radius,
                Material = sphere.Material,
            };
            return;
        }

        temp = (-b + Mathf.Sqrt(discriminant)) / a;
        if (temp < tMax && temp > tMin)
        {
            Vector3 point = ray.GetPoint(temp);
            hitRecord = new HitRecord()
            {
                T = temp,
                Point = point,
                Normal = (point - sphere.Center) / sphere.Radius,
                Material = sphere.Material,
            };
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

    private Vector3 ColorFromRay(Ray ray, SphereRecord[] spheres, int depth)
    {
        HitRecord hitRecord;
        HitSphereArray(spheres, ray, 0.001f, float.MaxValue, out hitRecord);
        if (hitRecord != null)
        {
            var scattered = new Ray();
            Vector3 attenuation = Vector3.zero;
            if (depth < 50 && hitRecord.Material.Scatter(ray, hitRecord, ref attenuation, ref scattered))
                return Vector3.Scale(attenuation, ColorFromRay(scattered, spheres, depth + 1));
            else
                return Vector3.zero;
        }

        Vector3 normalizedDir = ray.direction.normalized;
        float t = 0.5f * (normalizedDir.y + 1.0f);
        return (1.0f - t) * Vector3.one + t * new Vector3(0.5f, 0.7f, 1.0f);
    }

    private void RenderToBytes(int width, int height, int spp, SphereRecord[] spheres)
    {
        int bytesOffset = 0;

        var lookFrom = new Vector3(13.0f, 2.0f, 3.0f);
        var lookAt = Vector3.zero;
        float distToFocus = 10.0f;
        float aperture = 0.1f;

        var myCamera = new MyCamera(lookFrom, lookAt, Vector3.up, 20.0f, (float)width / (float)height, aperture, distToFocus);

        for (int j = 0; j < height; ++j)
        {
            for (int i = 0; i < width; ++i)
            {
                Vector3 rgb = Vector3.zero;
                for (int k = 0; k < spp; ++k)
                {
                    float u = ((float)i + Random.Range(0.0f, 0.999999f)) / (float)width;
                    float v = ((float)j + Random.Range(0.0f, 0.999999f)) / (float)height;

                    rgb += ColorFromRay(myCamera.GetRay(u, v), spheres, 0);
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
