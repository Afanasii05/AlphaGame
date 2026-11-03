using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using TMPro;

public class BlockWall : MonoBehaviour
{
    Transform[] pietre;
    Vector3[] finalPos;
    readonly Dictionary<string,Transform> pietreDict = new Dictionary<string, Transform>();

    int total_pietre;
    [Header("Compunere Wall")]
    [SerializeField] float adancime = 2.0f;
    [SerializeField] float speedOfPlace = 0.1f;
    [SerializeField] float distance = 2.0f;
    [SerializeField] float timeBetweenStones = 0.02f;

    [Header("Compunere golem")]
    [SerializeField] float timeToComposeGolem = 1.5f;
    [SerializeField] float speedOfComposeGolem = 0.1f;
    [SerializeField] float timeBetweenStonesCompose = 0.02f;
    [SerializeField] float waitTimeBeforeAwakenGolem = 1.0f;
    [SerializeField] float decreasingInterval = 0.001f;
    [SerializeField] LayerMask groundMask;
    [SerializeField] GameObject golemPrefab;
   
    GolemStonesSlots golemStonesSlots;
    NavMeshObstacle obst;
    bool canBuildGolem;
    bool golembuildStarted = false;

    void Awake()
    {
        obst = GetComponent<NavMeshObstacle>();
        obst.enabled = false;
        total_pietre = transform.childCount;
        pietre = new Transform[total_pietre];
        finalPos = new Vector3[total_pietre];
        for (int i = 0; i < total_pietre; i++)
        {
            pietre[i] = transform.GetChild(i);
            pietre[i].gameObject.SetActive(false);
        }
            
        System.Array.Sort(pietre, (a, b) => a.localPosition.y.CompareTo(b.localPosition.y));
        for (int i = 0; i < total_pietre; i++)
            finalPos[i] = pietre[i].localPosition;
        
    }

    void Start()
    { 
        StartCoroutine(buildWall());
    }
    public BlockWall instantiateWall(Transform playerTr)
    {
        Vector3 fw = Camera.main.transform.forward;
        fw.y = 0;
        fw = fw.normalized;
        Vector3 pos = playerTr.position + fw * distance;
        if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
            pos = hit.point;

        BlockWall wall = Instantiate(this, pos, Quaternion.LookRotation(fw));
        return wall;
    }
    public void stopWallBuild()
    {
        if(golembuildStarted)
            return;
        StopAllCoroutines();
        StartCoroutine(applyPhysics());
    }
    IEnumerator buildWall()
    {
        for (int i = 0; i < total_pietre; i++)
        {
            if (i == 8)
                obst.enabled = true;
            if(i>30)
                canBuildGolem = true;
            pietre[i].gameObject.SetActive(true);
            StartCoroutine(placeStone(pietre[i], finalPos[i] + Vector3.down * adancime, finalPos[i], speedOfPlace));
            yield return new WaitForSeconds(timeBetweenStones);
        }
        yield return new WaitForSeconds(2.0f);
        StartCoroutine(applyPhysics());
    }
    IEnumerator placeStone(Transform piatra, Vector3 startPos, Vector3 endPos, float speed, Quaternion? endRot = null)
    {
        float t = 0;
        while(t<1)
        {
            t += Time.deltaTime * speed;
            Vector3 travel = Vector3.Lerp(startPos, endPos, t);
            piatra.localPosition = travel;
            if (endRot.HasValue) piatra.rotation = Quaternion.Slerp(transform.rotation, endRot.Value, t);
            yield return null;
        }
        piatra.localPosition = endPos;

    }
    IEnumerator applyPhysics()
    {
        yield return new WaitForSeconds(0.5f);
        obst.enabled = false;
        for(int i = total_pietre-1;i>=0;i--)
        {
            Rigidbody rb = pietre[i].GetComponent<Rigidbody>();
            rb.isKinematic = false;
            yield return null;
        }
        if (canBuildGolem)
        {
            GameObject golem = Instantiate(golemPrefab, transform.position + Vector3.up *0.3f, transform.rotation);
            golemStonesSlots = golem.GetComponentInChildren<GolemStonesSlots>();
            StartCoroutine(composeGolem());
            golembuildStarted = true;
        }
        else
            Destroy(gameObject, 2.5f);
    }
  
    IEnumerator composeGolem() {
        yield return new WaitForSeconds(timeToComposeGolem);
        for (int i = 0; i < total_pietre; i++)
            pietreDict[pietre[i].name] = pietre[i];
        
        foreach (var stone in golemStonesSlots.stones)
        {
            Transform piatra = pietreDict[stone.name];
            piatra.SetParent(null, true);
            piatra.gameObject.SetActive(true);
            Destroy(piatra.GetComponent<Rigidbody>());
            piatra.GetComponent<Collider>().enabled = false;

            StartCoroutine(placeStone(piatra, piatra.position, stone.position, speedOfComposeGolem, stone.rotation));
            yield return new WaitForSeconds(Mathf.Max(timeBetweenStonesCompose, 0.001f));
            timeBetweenStonesCompose -= decreasingInterval;
        }
        yield return new WaitForSeconds(Mathf.Max(waitTimeBeforeAwakenGolem, 0.1f));
        golemStonesSlots.Activate();
        foreach(var piatra in pietreDict.Values)
            Destroy(piatra.gameObject);
        Destroy(gameObject);

    }
}
