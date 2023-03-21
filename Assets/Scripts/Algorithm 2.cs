using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Algorithm2 : MonoBehaviour
{
    private struct Particle
    {
        public GameObject Gameobject;
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 forcePhysic;
        public Vector3 forceHeading;
        public float Density;
        public float Pressure;

        public void Init(Vector3 _position, int _parameterID, GameObject _go)
        {
            Position = _position;
            Gameobject = _go;
            Velocity = Vector3.zero;
            forcePhysic = Vector3.zero;
            forceHeading = Vector3.zero;
            Density = 0.0f;
            Pressure = 0.0f;
        }
    }

    [System.Serializable]
    private struct SPHParameters
    {

        public float Radius;
        public float smoothingRadius;
        public float smoothingRadiusSq;
        public float RestDensity;
        public float gravityMultiplier;
        public float mass;
        public float Viscosity;
        public float Drag;

    }

    private Vector3 gravity = new Vector3(0.0f, -9.81f, 0.0f);
    private const float SPHestimator = 2000.0f;
    private const float deltaTime = 0.0008f;
    private const float boundDamping = -0.5f;

    // Properties
    [Header("Particle Prefab")]
    [SerializeField] private GameObject ParticlePrefab = null;

    [Header("Parameters")]
    [SerializeField] private SPHParameters[] parameters = null;

    [Header("Properties")]
    [SerializeField] private int Amount = 1000;
    [SerializeField] private int Rows = 8;

    private Particle[] particles;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

         private struct ParticleCollider
    {
        public Vector3 Position;
        public Vector3 Right;
        public Vector3 Up;
        public Vector2 Scale;

        public void Init(Transform _transform)
        {
            Position = _transform.position;
            Right = _transform.right;
            Up = _transform.up;
            Scale = new Vector2(_transform.lossyScale.x / 2f, _transform.lossyScale.y / 2f);
        }
    }
    /*
   private void CalculateDensity(float smoothingRadius)
    {
        float density = 0f;
        float hSq = smoothingRadius * smoothingRadius;

        for (int i = 0; i < particles.Length; i++)
        {
            Vector3 r = particles[j].Position - particles[i].Position;
            float rSq = r.sqrMagnitude;

            if (rSq < hSq)
            {
                float poly6 = 315f / (64f * Mathf.PI * Mathf.Pow(hSq, 9f / 2f)) * Mathf.Pow(hSq - rSq, 3f);
                density += particles[i].mass * poly6;
            }
        }

        return density;
    }

    /*public float CalculatePressure(float density, float restDensity, float gasStiffness)
    {
        return gasStiffness * (density - restDensity);
    }

    public Vector3 CalculatePressureForce(Vector3[] positions, float[] masses, float[] pressures, float density, Vector3 position, float smoothingRadius, float smoothingRadiusSq)
    {
        Vector3 pressureForce = Vector3.zero;

        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 r = position - positions[i];
            float rMag = r.magnitude;

            if (rMag < smoothingRadius && rMag > 0f)
            {
                float pressureTerm = masses[i] * (pressures[i] + density) / (2f * density * masses[i]);
                Vector3 gradient = -pressureTerm * SpikyGradient(r, rMag, smoothingRadius, smoothingRadiusSq);
                pressureForce += gradient;
            }
        }

        return pressureForce;
    }

    private Vector3 SpikyGradient(Vector3 r, float rMag, float h, float hSq)
    {
        float coef = -45f / (Mathf.PI * Mathf.Pow(h, 6f));
        float gradMag = coef * Mathf.Pow(h - rMag, 2f);
        return r.normalized * gradMag;
    }

    public Vector3 CalculateViscosityForce(Vector3[] positions, Vector3[] velocities, float[] masses, float viscosityCoefficient, Vector3 position, float smoothingRadius, float smoothingRadiusSq)
    {
        Vector3 viscosityForce = Vector3.zero;

        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 r = position - positions[i];
            float rMag = r.magnitude;

            if (rMag < smoothingRadius && rMag > 0f)
            {
                Vector3 velocityDiff = velocities[i] - velocities[currentIndex];
                float viscosityTerm = masses[i] * viscosityCoefficient / (masses[i] + density);
                Vector3 laplacian = ViscosityLaplacian(r, rMag, smoothingRadius, smoothingRadiusSq);
                viscosityForce += viscosityTerm * velocityDiff / density * laplacian;
            }
        }

        return viscosityForce;
    }

    private Vector3 ViscosityLaplacian(Vector3 r, float rMag, float h, float hSq)
    {
        float coef = 45f / (Mathf.PI * Mathf.Pow(h, 6f));
        float laplacianMag = coef * (h - rMag);
        return laplacianMag * r.normalized;
    }

    public Vector3 CalculateTotalForce(Vector3[] positions, Vector3[] velocities, float[] masses, float pressureCoefficient, float viscosityCoefficient, Vector3 position, float density, float smoothingRadius, float smoothingRadiusSq, Vector3 externalForce)
    {
        Vector3 pressureForce = CalculatePressureForce(positions, masses, pressureCoefficient, position, density, smoothingRadius, smoothingRadiusSq);
        Vector3 viscosityForce = CalculateViscosityForce(positions, velocities, masses, viscosityCoefficient, position, smoothingRadius, smoothingRadiusSq);
        Vector3 totalForce = pressureForce + viscosityForce + externalForce;

        return totalForce;
    }

    public Vector3 CalculateAcceleration(Vector3 totalForce, float density)
    {
        Vector3 acceleration = totalForce / density;

        return acceleration;
    }*/
}
