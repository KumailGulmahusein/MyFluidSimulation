using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class ParticleManager : MonoBehaviour
{
    //Particle variables
    private struct Particle
    {
        public GameObject gameObject;
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 bodyForce;
        public float density;
        public float pressure;
        public int parameterID;


        public void Init(Vector3 pposition, int pparameterID, GameObject pGo)
        {
            position = pposition;
            parameterID = pparameterID;
            gameObject = pGo;

        }
    }

    //Simulation variables
    [System.Serializable]
    private struct ParticleParameters
    {

        public float radius;
        public float kernelRadius;
        public float kernelRadiusSq;
        public float restDensity;
        public float mass;
        public float viscosity;
        public float drag;
    }

    private Vector3 gravity = new Vector3(0.0f, -9.81f, 0.0f);
    private const float gasValue = 2000.0f;
    private const float deltaTime = 0.0007f;
    private const float boundDamping = -0.5f;

    // Properties
    [Header("Particle Prefab")]
    [SerializeField] private GameObject ParticlePrefab = null;

    [Header("Parameters")]
    [SerializeField] private int parameterID = 0;
    [SerializeField] private ParticleParameters[] parameters = null;

    [Header("Properties")]
    [SerializeField] private int Amount = 2000;
    [SerializeField] private int Rows = 20;

    private Particle[] particles;


    //Collider variables
    private struct ParticleCollider
    {
        public Vector3 position;
        public Vector3 right;
        public Vector3 up;
        public Vector2 scale;

        public void Initializecollider(Transform _transform)
        {
            position = _transform.position;
            right = _transform.right;
            up = _transform.up;
            scale = new Vector2(_transform.lossyScale.x / 2f, _transform.lossyScale.y / 2f);
        }
    }
    // Start is called before the first frame Update
    private void Start()
    {
        InitializeParticles();
    }

    // Update is called once per frame
    void Update()
    {
        ComputeDensityPressure();
        ComputeForces();
        Integrate();
        ComputeBoundary();
        UpdatePosition();
        PlayerInput();
    }

    //Initialize GameObjects
    private void InitializeParticles()
    {
        particles = new Particle[Amount];

        for (int i = 0; i < Amount; i++)
        {
            float x = (i % Rows) + Random.Range(-0.1f, 0.1f);
            float y = 2 + (float)((i / Rows) / Rows) * 1.1f;
            float z = ((i / Rows) % Rows) + Random.Range(-0.1f, 0.1f);

            GameObject particlePrefab = Instantiate(ParticlePrefab);
            particlePrefab.transform.localScale = Vector3.one * parameters[parameterID].radius;
            particlePrefab.transform.position = new Vector3(x, y, z);

            particles[i].Init(new Vector3(x, y, z), parameterID, particlePrefab);
        }
    }

    //Compute density pressure
    private void ComputeDensityPressure()
    {

        Parallel.For(0, particles.Length, i =>
        {
            particles[i].density = 0.0f;

            for (int j = 0; j < particles.Length; j++)
            {
                Vector3 rij = particles[i].position - particles[j].position;
                float r = rij.sqrMagnitude;


                if (r < parameters[particles[i].parameterID].kernelRadiusSq)
                {

                    particles[i].density += parameters[particles[i].parameterID].mass * 315.0f / (64.0f * Mathf.PI * (float)Mathf.Pow(parameters[particles[i].parameterID].kernelRadius, 9.0f)) * Mathf.Pow(parameters[particles[i].parameterID].kernelRadiusSq - r, 3f);

                }

                particles[i].pressure = gasValue * (particles[i].density - parameters[particles[i].parameterID].restDensity);
            }
        });
    }

    //Compute pressure, viscosity and gravity
    private void ComputeForces()
    {
        Parallel.For(0, particles.Length, i =>
        {
            Vector3 forceViscosity = Vector3.zero;
            Vector3 forcePressure = Vector3.zero;

            for (int j = 0; j < particles.Length; j++)
            {
                if (i == j) continue;

                Vector3 rij = particles[j].position - particles[i].position;
                float r = rij.sqrMagnitude;

                if (r < parameters[particles[i].parameterID].kernelRadius)
                {
                    forcePressure += (particles[i].pressure + particles[j].pressure) * parameters[particles[i].parameterID].mass * -rij.normalized / (2.0f * particles[j].density) * (-15.0f / (Mathf.PI * Mathf.Pow(parameters[particles[i].parameterID].kernelRadius, 6.0f))) * Mathf.Pow(parameters[particles[i].parameterID].kernelRadius - r, 3.0f);

                    forceViscosity += parameters[particles[i].parameterID].viscosity * parameters[particles[i].parameterID].mass * (particles[j].velocity - particles[i].velocity) / particles[j].density * (45.0f / (Mathf.PI * Mathf.Pow(parameters[particles[i].parameterID].kernelRadius, 6.0f))) * (parameters[particles[i].parameterID].kernelRadius - r);

                }
            }

            Vector3 forceGravity = gravity * parameters[particles[i].parameterID].mass * 1500;

            particles[i].bodyForce = forcePressure + forceViscosity + forceGravity;
        });
    }

    //Numerical integration timestep
    private void Integrate()
          {
              for (int i = 0; i < particles.Length; i++)
              {
                  particles[i].velocity += deltaTime * (particles[i].bodyForce) / particles[i].density;
                  particles[i].position += deltaTime * (particles[i].velocity);
              }
          }

    //Boundary conditions
    private void ComputeBoundary()
    {
        // Get colliders
        GameObject[] collidersGO = GameObject.FindGameObjectsWithTag("SPHCollider");
        ParticleCollider[] colliders = new ParticleCollider[collidersGO.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].Initializecollider(collidersGO[i].transform);
        }

        Parallel.For(0, particles.Length, i =>
        {
            for (int j = 0; j < colliders.Length; j++)
            {
                // Check collision
                Vector3 penetrationNormal;
                Vector3 penetrationposition;
                float penetrationLength;

                if (Intersect(colliders[j], particles[i].position, parameters[particles[i].parameterID].radius, out penetrationNormal, out penetrationposition, out penetrationLength))
                {
                    particles[i].velocity = Dampvelocity(colliders[j], particles[i].velocity, penetrationNormal, 1.0f - parameters[particles[i].parameterID].drag);
                    particles[i].position = penetrationposition - penetrationNormal * Mathf.Abs(penetrationLength);
                }
            }
        });
    }
    private static bool Intersect(ParticleCollider collider, Vector3 position, float radius, out Vector3 penetrationNormal, out Vector3 penetrationposition, out float penetrationLength)
    {
        Vector3 colliderProjection = collider.position - position;

        penetrationNormal = Vector3.Cross(collider.right, collider.up);
        penetrationLength = Mathf.Abs(Vector3.Dot(colliderProjection, penetrationNormal)) - (radius / 2.0f);
        penetrationposition = collider.position - colliderProjection;

        return penetrationLength < 0.0f && Mathf.Abs(Vector3.Dot(colliderProjection, collider.right)) < collider.scale.x && Mathf.Abs(Vector3.Dot(colliderProjection, collider.up)) < collider.scale.y;
    }
    private static Vector3 Dampvelocity(ParticleCollider collider, Vector3 velocity, Vector3 penetrationNormal, float drag)
    {
        Vector3 newvelocity = Vector3.Dot(velocity, penetrationNormal) * penetrationNormal * boundDamping + Vector3.Dot(velocity, collider.right) * collider.right * drag + Vector3.Dot(velocity, collider.up) * collider.up * drag;
        newvelocity = Vector3.Dot(newvelocity, Vector3.forward) * Vector3.forward + Vector3.Dot(newvelocity, Vector3.right) * Vector3.right + Vector3.Dot(newvelocity, Vector3.up) * Vector3.up;
        return newvelocity;
    }

    private void UpdatePosition()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].gameObject.transform.position = particles[i].position;
        }
    }
    //Manipulate movement
    private void PlayerInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            gravity = new Vector3(-9.81f, 0.0f, 0.0f);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            gravity = new Vector3(9.81f, 0.0f, 0.0f);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            gravity = new Vector3(0.0f, -9.81f, 0.0f);  
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            gravity = new Vector3(0.0f, 9.81f, 0.0f);
        }
    }
}
