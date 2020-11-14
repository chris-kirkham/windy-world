﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshFilter))]
public class WindAffectedPhysObj : MonoBehaviour
{
    public enum WindSampleMode { Centre, RandomNPoints, AABBCorners, Mesh }; //position(s) to sample wind from on this object 
    public WindSampleMode mode = WindSampleMode.Centre;

    [Range(1, 10)] public int randomNPointsSamples = 1;

    public Wind.ComputeWindField windField;
    public Mesh windSampleMesh;

    private Rigidbody rb;
    private Mesh mesh;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mesh = GetComponent<MeshFilter>().mesh;
        if (windField == null) Debug.LogError("No wind field selected for " + this + "!");
    }

    void FixedUpdate()
    {
        Tuple<Vector3, Vector3>[] samples = GetWindSamples();
        foreach(Tuple<Vector3, Vector3> sample in samples)
        {
            rb.AddForceAtPosition(sample.Item1, sample.Item2, ForceMode.Force);
        }
    }

    private Tuple<Vector3, Vector3>[] GetWindSamples()
    {
        switch (mode)
        {
            case WindSampleMode.Centre:
                {
                    return new Tuple<Vector3, Vector3>[1] { new Tuple<Vector3, Vector3>(windField.GetWind(transform.position), transform.position) };
                }
            case WindSampleMode.RandomNPoints:
                {
                    Tuple<Vector3, Vector3>[] samples = new Tuple<Vector3, Vector3>[randomNPointsSamples];
                    float radius = Vector3.Magnitude(mesh.bounds.max - mesh.bounds.min) / 2; //approximation of object size
                    for (int i = 0; i < randomNPointsSamples; i++)
                    {
                        Vector3 pos = transform.position + (UnityEngine.Random.insideUnitSphere * radius);
                        samples[i] = new Tuple<Vector3, Vector3>(windField.GetWind(pos), pos);
                    }

                    return samples;
                }
            case WindSampleMode.AABBCorners:
                {
                    Vector3[] positions = new Vector3[8];
                    Vector3 min = mesh.bounds.min;
                    Vector3 x = new Vector3(mesh.bounds.size.x, 0, 0);
                    Vector3 y = new Vector3(0, mesh.bounds.size.y, 0);
                    Vector3 z = new Vector3(0, 0, mesh.bounds.size.z);
                    positions[0] = min;
                    positions[1] = min + x;
                    positions[2] = min + x + z;
                    positions[3] = min + z;
                    positions[4] = positions[0] + y;
                    positions[5] = positions[1] + y;
                    positions[6] = positions[2] + y;
                    positions[7] = positions[3] + y;

                    Tuple<Vector3, Vector3>[] samples = new Tuple<Vector3, Vector3>[8];
                    for (int i = 0; i < 8; i++)
                    {
                        samples[i] = new Tuple<Vector3, Vector3>(windField.GetWind(positions[i]), positions[i]);
                    }

                    return samples;
                }
            case WindSampleMode.Mesh:
                if(windSampleMesh == null)
                {
                    Debug.LogError("Trying to sample wind from a mesh, but sample mesh is null! Returning zero vector.");
                    return new Tuple<Vector3, Vector3>[1] { new Tuple<Vector3, Vector3>(Vector3.zero, transform.position) };
                }
                else
                {
                    Vector3[] samplePositions = windSampleMesh.vertices;
                    Tuple<Vector3, Vector3>[] samples = new Tuple<Vector3, Vector3>[samplePositions.Length];
                    for(int i = 0; i < samplePositions.Length; i++)
                    {
                        samples[i] = new Tuple<Vector3, Vector3>(windField.GetWind(samplePositions[i]), samplePositions[i]);
                    }

                    return samples;
                }
            default:
                Debug.LogError("Trying to sample wind with and unhandled WindSampleMode! Did you add another mode and forget to update this function?");
                return new Tuple<Vector3, Vector3>[1] { new Tuple<Vector3, Vector3>(Vector3.zero, transform.position) };
        }
    }
}
