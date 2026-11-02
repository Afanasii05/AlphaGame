using System.Collections;
using UnityEngine;

public class HeavyHitSwordsTransition : MonoBehaviour
{
    [SerializeField] float transitionDuration = 1f;
    private void Awake()
    {
        Vector3 finalPos = transform.position;    
        StartCoroutine(moveToFinalPos(finalPos));
    }
    IEnumerator moveToFinalPos(Vector3 finalPos)
    {
        Vector3 initialPos = transform.position-Vector3.up * 2f;
        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            float t = elapsedTime / transitionDuration;
            transform.position = Vector3.Lerp(initialPos, finalPos, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = finalPos;
    }

}
