using System.Collections;
using UnityEngine;

public class HeavyHitSwordsTransition : MonoBehaviour
{
    [SerializeField] float transitionDuration = 1f;
    Vector3 finalPos;
    private void Awake()
    {
        finalPos = transform.position;    
        StartCoroutine(fullMove());
    }
    IEnumerator fullMove()
    {
        StartCoroutine(moveFromToPos(transform.position - Vector3.up * 2f, finalPos));
        yield return new WaitForSeconds(1.2f);
        StartCoroutine(moveFromToPos(finalPos, finalPos - Vector3.up * 2 ));
    }
    IEnumerator moveFromToPos(Vector3 initial,Vector3 finalPos)
    {
        
        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            float t = elapsedTime / transitionDuration;
            transform.position = Vector3.Lerp(initial, finalPos, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = finalPos;
    }
}
