﻿using UnityEngine;

namespace TensorFlowLite
{
    /// <summary>
    /// Utility to resize the texture
    /// </summary>
    public class TextureResizer : System.IDisposable
    {
        public enum AspectMode
        {
            None,
            Fit,
            Fill,
        }

        public struct ResizeOptions
        {
            public int width;
            public int height;
            public float rotationDegree;
            public bool flipX;
            public bool flipY;
            public AspectMode aspectMode;
        }

        RenderTexture resizeTexture;
        Material _blitMaterial;

        static readonly int _VertTransform = Shader.PropertyToID("_VertTransform");
        static readonly int _UVRect = Shader.PropertyToID("_UVRect");

        public Material material
        {
            get
            {
                if (_blitMaterial == null)
                {
                    _blitMaterial = new Material(Shader.Find("Hidden/TFLite/Resize"));
                }
                return _blitMaterial;
            }
        }

        public TextureResizer()
        {

        }

        public void Dispose()
        {
            DisposeUtil.TryDispose(resizeTexture);
            DisposeUtil.TryDispose(_blitMaterial);
        }

        public ResizeOptions ModifyOptionForWebcam(ResizeOptions options, WebCamTexture texture)
        {
            options.rotationDegree += texture.videoRotationAngle;
            if (texture.videoVerticallyMirrored)
            {
                options.flipX = !options.flipX;
            }
            return options;
        }

        public RenderTexture Resize(Texture texture, ResizeOptions options)
        {
            // Set options
            if (texture is WebCamTexture)
            {
                options = ModifyOptionForWebcam(options, (WebCamTexture)texture);
            }
            Matrix4x4 trs = GetVertTransform(options.rotationDegree, options.flipX, options.flipY);
            Vector4 uvRect = GetTextureST(
                (float)texture.width / (float)texture.height, // src
                (float)options.width / (float)options.height, // dst
                options.aspectMode);
            material.SetMatrix(_VertTransform, trs);
            material.SetVector(_UVRect, uvRect);
            return Blit(texture, material, options.width, options.height);
        }

        public RenderTexture Blit(Texture texture, Material mat, int width, int height)
        {
            if (resizeTexture == null
                || resizeTexture.width != width
                || resizeTexture.height != height)
            {
                DisposeUtil.TryDispose(resizeTexture);
                resizeTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            }
            Graphics.Blit(texture, resizeTexture, mat, 0);
            return resizeTexture;
        }

        private static Vector4 GetTextureST(float srcAspect, float dstAspect, AspectMode mode)
        {
            switch (mode)
            {
                case AspectMode.None:
                    return new Vector4(1, 1, 0, 0);
                case AspectMode.Fit:
                    if (srcAspect > dstAspect)
                    {
                        float s = srcAspect / dstAspect;
                        return new Vector4(1, s, 0, (1 - s) / 2);
                    }
                    else
                    {
                        float s = dstAspect / srcAspect;
                        return new Vector4(s, 1, (1 - s) / 2, 0);
                    }
                case AspectMode.Fill:
                    if (srcAspect > dstAspect)
                    {
                        float s = dstAspect / srcAspect;
                        return new Vector4(s, 1, (1 - s) / 2, 0);
                    }
                    else
                    {
                        float s = srcAspect / dstAspect;
                        return new Vector4(1, s, 0, (1 - s) / 2);
                    }
            }
            throw new System.Exception("Unknown aspect mode");
        }

        private static readonly Matrix4x4 PUSH_MATRIX = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0));
        private static readonly Matrix4x4 POP_MATRIX = Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0));
        private static Matrix4x4 GetVertTransform(float rotation, bool invertX, bool invertY)
        {
            Vector3 scale = new Vector3(
                invertX ? -1 : 1,
                invertY ? -1 : 1,
                1);
            Matrix4x4 trs = Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.Euler(0, 0, rotation),
                scale
            );
            return PUSH_MATRIX * trs * POP_MATRIX;
        }
    }
}
