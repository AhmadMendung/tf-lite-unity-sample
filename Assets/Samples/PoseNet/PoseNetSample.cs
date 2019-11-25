﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TensorFlowLite;
using Gizmos = Popcron.Gizmos;

public class PoseNetSample : MonoBehaviour
{
    [SerializeField] string fileName = "posenet_mobilenet_v1_100_257x257_multi_kpt_stripped.tflite";
    [SerializeField] RawImage cameraView = null;
    [SerializeField, Range(0f, 1f)] float threshold = 0.5f;

    WebCamTexture webcamTexture;
    PoseNet poseNet;

    public PoseNet.Result[] results;

    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        poseNet = new PoseNet(path);

        // Init camera
        string cameraName = GetWebcamName();
        webcamTexture = new WebCamTexture(cameraName, 640, 480, 30);
        webcamTexture.Play();
        cameraView.texture = webcamTexture;
    }

    void OnDestroy()
    {
        webcamTexture?.Stop();
        poseNet?.Dispose();
    }

    void Update()
    {
        poseNet.Invoke(webcamTexture);
        results = poseNet.GetResults();

        //
        Vector4 texST = TextureToTensor.GetUVRect((float)webcamTexture.width / webcamTexture.height, 1, TextureToTensor.AspectMode.Fill);
        cameraView.uvRect = new Rect(texST.z, texST.w, texST.x, texST.y);

        UpdateGizmo();
    }

    static string GetWebcamName()
    {
        if (Application.isMobilePlatform)
        {
            return WebCamTexture.devices.Where(d => !d.isFrontFacing).Last().name;

        }
        return WebCamTexture.devices.Last().name;
    }



    Vector3[] corners = new Vector3[4];
    void UpdateGizmo()
    {
        if (results.Length == 0)
        {
            return;
        }

        var rect = cameraView.GetComponent<RectTransform>();
        rect.GetWorldCorners(corners);
        Vector3 min = corners[0];
        Vector3 max = corners[2];

        Color color = Color.green;

        // Spheres
        foreach (var result in results)
        {
            if (result.confidence >= threshold)
            {
                var p = Leap3(min, max, new Vector3(result.x, 1f - result.y, 0));
                Gizmos.Sphere(p, 1, color);
            }
        }

        // Lines
        var connections = PoseNet.Connections;
        int len = connections.GetLength(0);
        for (int i = 0; i < len; i++)
        {
            var a = results[(int)connections[i, 0]];
            var b = results[(int)connections[i, 1]];

            if (a.confidence >= threshold && b.confidence >= threshold)
            {
                Gizmos.Line(
                    Leap3(min, max, new Vector3(a.x, 1f - a.y, 0)),
                    Leap3(min, max, new Vector3(b.x, 1f - b.y, 0)),
                    color);
            }
        }
    }

    /// <summary>
    /// 3 Dimentional Leap
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    static Vector3 Leap3(in Vector3 a, in Vector3 b, in Vector3 t)
    {
        return new Vector3(
            Mathf.Lerp(a.x, b.x, t.x),
            Mathf.Lerp(a.y, b.y, t.y),
            Mathf.Lerp(a.z, b.z, t.z)
        );
    }

}
