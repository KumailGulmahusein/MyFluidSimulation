using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
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

        public int parameterID;

        public void Init(Vector3 _position, int _parameterID, GameObject _go)
        {
            Position = _position;
            parameterID = _parameterID;
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
        public float Mass;
        public float Viscosity;
        public float Drag;

    }

    private Vector3 gravity = new Vector3(0.0f, -9.81f, 0.0f);
    private const float constant = 2000.0f;
    private const float deltaTime = 0.0008f;
    private const float boundDamping = -0.5f;

    // Properties
    [Header("Import")]
    [SerializeField] private GameObject character0Prefab = null;

    [Header("Parameters")]
    [SerializeField] private int parameterID = 0;
    [SerializeField] private SPHParameters[] parameters = null;

    [Header("Properties")]
    [SerializeField] private int Amount = 250;
    [SerializeField] private int Rows = 16;

    private Particle[] particles;


    // Start is called before the first frame Update
    private void Start()
    {
        particles = new Particle[Amount];

        for (int i = 0; i < Amount; i++)
        {
            float x = (i % Rows) + Random.Range(-0.1f, 0.1f);
            float y = 2 + (float)((i / Rows) / Rows) * 1.1f;
            float z = ((i / Rows) % Rows) + Random.Range(-0.1f, 0.1f);

            GameObject thisGO = Instantiate(character0Prefab);
            thisGO.transform.localScale = Vector3.one * parameters[parameterID].Radius;
            thisGO.transform.localScale = Vector3.one * parameters[parameterID].Radius;
            thisGO.transform.position = new Vector3(x, y, z);

            particles[i].Init(new Vector3(x, y, z), parameterID, thisGO);
        }
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

    // Update is called once per frame
    void Update()
    {
        ComputeDensityPressure();
        ComputeForces();
        Integrate();
        ComputeColliders();

        ApplyPosition();
    }

    private static bool Intersect(ParticleCollider collider, Vector3 Position, float radius, out Vector3 penetrationNormal, out Vector3 penetrationPosition, out float penetrationLength)
    {
        Vector3 colliderProjection = collider.Position - Position;

        penetrationNormal = Vector3.Cross(collider.Right, collider.Up);
        penetrationLength = Mathf.Abs(Vector3.Dot(colliderProjection, penetrationNormal)) - (radius / 2.0f);
        penetrationPosition = collider.Position - colliderProjection;

        return penetrationLength < 0.0f && Mathf.Abs(Vector3.Dot(colliderProjection, collider.Right)) < collider.Scale.x && Mathf.Abs(Vector3.Dot(colliderProjection, collider.Up)) < collider.Scale.y;
    }

    private static Vector3 DampVelocity(ParticleCollider collider, Vector3 Velocity, Vector3 penetrationNormal, float drag)
    {
        Vector3 newVelocity = Vector3.Dot(Velocity, penetrationNormal) * penetrationNormal * boundDamping + Vector3.Dot(Velocity, collider.Right) * collider.Right * drag + Vector3.Dot(Velocity, collider.Up) * collider.Up * drag;
        newVelocity = Vector3.Dot(newVelocity, Vector3.forward) * Vector3.forward + Vector3.Dot(newVelocity, Vector3.right) * Vector3.right + Vector3.Dot(newVelocity, Vector3.up) * Vector3.up;
        return newVelocity;
    }

    private void ComputeColliders()
    {
        // Get colliders
        GameObject[] collidersGO = GameObject.FindGameObjectsWithTag("SPHCollider");
        ParticleCollider[] colliders = new ParticleCollider[collidersGO.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].Init(collidersGO[i].transform);
        }

        for (int i = 0; i < particles.Length; i++)
        {
            for (int j = 0; j < colliders.Length; j++)
            {
                // Check collision
                Vector3 penetrationNormal;
                Vector3 penetrationPosition;
                float penetrationLength;
                if (Intersect(colliders[j], particles[i].Position, parameters[particles[i].parameterID].Radius, out penetrationNormal, out penetrationPosition, out penetrationLength))
                {
                    particles[i].Velocity = DampVelocity(colliders[j], particles[i].Velocity, penetrationNormal, 1.0f - parameters[particles[i].parameterID].Drag);
                    particles[i].Position = penetrationPosition - penetrationNormal * Mathf.Abs(penetrationLength);
                }
            }
        }
    }

    private void Integrate()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Velocity += deltaTime * (particles[i].forcePhysic) / particles[i].Density;
            particles[i].Position += deltaTime * (particles[i].Velocity);
        }
    }

    private void ComputeDensityPressure()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Density = 0.0f;

            for (int j = 0; j < particles.Length; j++)
            {
                Vector3 rij = particles[j].Position - particles[i].Position;
                float r2 = rij.sqrMagnitude;

                if (r2 < parameters[particles[i].parameterID].smoothingRadiusSq)
                {
                    particles[i].Density += parameters[particles[i].parameterID].Mass * (315.0f / (64.0f * Mathf.PI * Mathf.Pow(parameters[particles[i].parameterID].smoothingRadius, 9.0f))) * Mathf.Pow(parameters[particles[i].parameterID].smoothingRadiusSq - r2, 3.0f);
                }
            }

            particles[i].Pressure = constant * (particles[i].Density - parameters[particles[i].parameterID].RestDensity);
        }
    }

    private void ComputeForces()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            Vector3 forcePressure = Vector3.zero;
            Vector3 forceViscosity = Vector3.zero;

            for (int j = 0; j < particles.Length; j++)
            {
                if (i == j) continue;

                Vector3 rij = particles[j].Position - particles[i].Position;
                float r2 = rij.sqrMagnitude;
                float r = Mathf.Sqrt(r2);

                if (r < parameters[particles[i].parameterID].smoothingRadius)
                {
                    forcePressure += -rij.normalized * parameters[particles[i].parameterID].Mass * (particles[i].Pressure + particles[j].Pressure) / (2.0f * particles[j].Density) * (-45.0f / (Mathf.PI * Mathf.Pow(parameters[particles[i].parameterID].smoothingRadius, 6.0f))) * Mathf.Pow(parameters[particles[i].parameterID].smoothingRadius - r, 2.0f);

                    forceViscosity += parameters[particles[i].parameterID].Viscosity * parameters[particles[i].parameterID].Mass * (particles[j].Velocity - particles[i].Velocity) / particles[j].Density * (45.0f / (Mathf.PI * Mathf.Pow(parameters[particles[i].parameterID].smoothingRadius, 6.0f))) * (parameters[particles[i].parameterID].smoothingRadius - r);
                }
            }

            Vector3 forceGravity = gravity * particles[i].Density * parameters[particles[i].parameterID].gravityMultiplier;

            particles[i].forcePhysic = forcePressure + forceViscosity + forceGravity;
        }
    }

    private void ApplyPosition()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Gameobject.transform.position = particles[i].Position;
        }
    }
}
