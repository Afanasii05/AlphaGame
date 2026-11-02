using UnityEngine;
using System.Collections;
using System;
using UnityEngine.AI;

public class BlockWall : MonoBehaviour
{
    Transform[] pietre;
    Vector3[] finalPos;
    

    int total_pietre;
    [SerializeField] float adancime = 2.0f;
    [SerializeField] float speedOfPlace = 0.1f;
    [SerializeField] float distance = 2.0f;
    [SerializeField] float timeBetweenStones = 0.02f;
    [SerializeField] LayerMask groundMask;
    NavMeshObstacle obst;
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
        Destroy(wall.gameObject, 5f);
        return wall;
    }
    public void stopWallBuild()
    {
        StopAllCoroutines();
        StartCoroutine(applyPhysics());
    }
    IEnumerator buildWall()
    {
        for (int i = 0; i < total_pietre; i++)
        {
            if (i == 8)
                obst.enabled = true;
            pietre[i].gameObject.SetActive(true);
            StartCoroutine(placeStone(pietre[i], finalPos[i]));
            yield return new WaitForSeconds(timeBetweenStones);
        }
        yield return new WaitForSeconds(2.0f);
        StartCoroutine(applyPhysics());
    }
    IEnumerator placeStone(Transform piatra, Vector3 endPos)
    {
        Vector3 startPos = endPos + Vector3.down * adancime;
        float t = 0;
        while(t<1)
        {
            t += Time.deltaTime * speedOfPlace;
            Vector3 travel = Vector3.Lerp(startPos, endPos, t);
            piatra.localPosition = travel;
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
    }
}
