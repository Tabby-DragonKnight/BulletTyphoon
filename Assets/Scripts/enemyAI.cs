using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour, IDamage
{
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Animator anim;
    [SerializeField] Renderer model;
    [SerializeField] Transform shootpos, headpos;

    [SerializeField] int hp;
    [SerializeField] int facetargetspeed;
    [SerializeField] int fov;
    [SerializeField] int animspeedtrans;

    [SerializeField] GameObject bullet;
    [SerializeField] float shootrate;
    [SerializeField] int shootrange;
    [SerializeField] int roampause;
    [SerializeField] int roamdist;

    float angle2player;
    float stoppingdistorig;

    bool isshooting;
    bool playerinrange;
    bool isroaming;

    Color colororig;

    Vector3 playerdir;
    Vector3 startingpos;

    Coroutine cor;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        colororig = model.sharedMaterial.color;
        GameManager.instance.updateGameGoal(1);
        stoppingdistorig = agent.stoppingDistance;
        startingpos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        float agentSpeed = agent.velocity.normalized.magnitude;
        float animSpeed = anim.GetFloat("Speed");

        anim.SetFloat("Speed", Mathf.MoveTowards(animSpeed, agentSpeed, Time.deltaTime * animSpeed));

        if (playerinrange && !canSeePlayer())
        {
            if (!isroaming && agent.remainingDistance < 0.1f)
            {
                cor = StartCoroutine(roam());
            }
        }
        else if (!playerinrange)
        {
            if (!isroaming && agent.remainingDistance < 0.1f)
            {
                cor = StartCoroutine(roam());
            }
        }
    }

    IEnumerator roam()
    {
        isroaming = true;
        yield return new WaitForSeconds(roampause);
        agent.stoppingDistance = 0;
        Vector3 randomPos = Random.insideUnitSphere * roamdist;
        randomPos += startingpos;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomPos, out hit, roamdist, 1);
        agent.SetDestination(hit.position);
        isroaming = false;
    }

    bool canSeePlayer()
    {
        playerdir = GameManager.instance.player.transform.position - headpos.position;
        angle2player = Vector3.Angle(playerdir, transform.forward);
        Debug.DrawRay(headpos.position, playerdir);
        RaycastHit hit;
        if (Physics.Raycast(headpos.position, playerdir, out hit))
        {
            if (hit.collider.CompareTag("Player") && angle2player <= fov)
            {
                agent.SetDestination(GameManager.instance.player.transform.position);
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    faceTarget();
                }
                if (!isshooting)
                {
                    StartCoroutine(shoot());
                }
                agent.stoppingDistance = stoppingdistorig;
                return true;
            }
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            playerinrange = true;
        }
    } 

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            playerinrange = false;
            agent.stoppingDistance = 0;
        }
    }

    void faceTarget()
    {
        Quaternion rot = Quaternion.LookRotation(new Vector3(playerdir.x, 0, playerdir.z));
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * facetargetspeed);
    }

    public void takeDamage(int amount)
    {
        hp -= amount;
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.SetDestination(GameManager.instance.player.transform.position); // Go to player last know location
        }
        if (cor != null)
        {
            StopCoroutine(cor);
            isroaming = false;
        }
        StartCoroutine(flashRed());
        if (hp <= 0)
        {
            GameManager.instance.updateGameGoal(-1);
            agent.enabled = false;
            Destroy(gameObject);
        }
    }

    IEnumerator flashRed()
    {
        model.sharedMaterial.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.sharedMaterial.color = colororig;
    }

    IEnumerator shoot()
    {
        isshooting = true;
        Instantiate(bullet, shootpos.position, transform.rotation);
        yield return new WaitForSeconds(shootrate);
        isshooting = false;
    }
}
