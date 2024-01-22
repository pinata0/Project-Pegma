using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowController : MonoBehaviour
{
    [Range(0, 1f)]
    public float masterIntensity = 1f;
    [Range(0, 1f)]
    public float snowIntensity = 1f;
    [Range(0, 1f)]
    public float windIntensity = 1f;
    [Range(0, 1f)]
    public float fogIntensity = 1f;
    [Range(0, 7f)]
    public float snowLevel;
    public bool autoUpdate;

    public ParticleSystem snowPart;
    public ParticleSystem windPart;
    public ParticleSystem fogPart;

    private ParticleSystem.EmissionModule snowEmission;
    private ParticleSystem.ForceOverLifetimeModule snowForce;
    private ParticleSystem.ShapeModule snowShape;
    private ParticleSystem.EmissionModule windEmission;
    private ParticleSystem.MainModule windMain;
    private ParticleSystem.EmissionModule lightningEmission;
    private ParticleSystem.MainModule lightningMain;
    private ParticleSystem.EmissionModule fogEmission;
    private Transform snowTransform;

    public Material snowMat;

    void Awake()
    {
        snowTransform = snowPart.transform;
        snowEmission = snowPart.emission;
        snowShape = snowPart.shape;
        snowForce = snowPart.forceOverLifetime;
        windEmission = windPart.emission;
        windMain = windPart.main;
        fogEmission = fogPart.emission;
        UpdateAll();
    }

    void Update(){
        if (autoUpdate)
            UpdateAll();
    }

    void UpdateAll()
    {
        ParticleSystem.EmissionModule snowEmissionModule = snowEmission;
        snowEmissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(110f * masterIntensity * snowIntensity);

        snowShape.radius = 30f * Mathf.Clamp(windIntensity, 0.4f, 1f) * masterIntensity;

        ParticleSystem.MinMaxCurve snowForceCurveX = new ParticleSystem.MinMaxCurve(-9f * windIntensity, -3 - 14f * windIntensity);
        snowForce.x = snowForceCurveX;

        ParticleSystem.EmissionModule windEmissionModule = windEmission;
        windEmissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(14f * masterIntensity * (windIntensity + fogIntensity));

        ParticleSystem.MainModule windMainModule = windMain;
        windMainModule.startLifetime = 2f + 6f * (1f - windIntensity);
        windMainModule.startSpeed = new ParticleSystem.MinMaxCurve(15f * windIntensity, 20 * windIntensity);

        ParticleSystem.EmissionModule fogEmissionModule = fogEmission;
        fogEmissionModule.rateOverTime = new ParticleSystem.MinMaxCurve((fogIntensity + (snowIntensity + windIntensity) * 0.5f) * masterIntensity);

        snowMat.SetFloat("_SnowLevel", snowLevel);
    }

    public void OnMasterChanged(float value)
    {
        masterIntensity = value;
        UpdateAll();
    }
    public void OnSnowChanged(float value)
    {
        snowIntensity = value;
        UpdateAll();
    }
    public void OnWindChanged(float value)
    {
        windIntensity = value;
        UpdateAll();
    }
    public void OnFogChanged(float value)
    {
        fogIntensity = value;
        UpdateAll();
    }
    public void OnSnowLevelChanged(float value)
    {
        snowLevel = value;
        UpdateAll();
    }
}
