using UnityEngine;
namespace Alpha.Combat
{
    public class CraterSpawner : MonoBehaviour
    {
        [SerializeField] float SpawnOffsetFromGround = 0.05f;
        [SerializeField] GameObject GroundCraterPrefab;
        [SerializeField] Weapon wp;
        [SerializeField] Collider headColider;
        private bool wasThrown = false;
        public void setWasThrown(bool st) { wasThrown = st; }
        
        private void OnCollisionEnter(Collision collision)
        {

            if (wasThrown || wp.IsHeld)
                return;

            ContactPoint cont = collision.GetContact(0);
            if (cont.thisCollider != headColider)
                return;
            Vector3 point = cont.point;
            Vector3 nr = cont.normal;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, nr);
            SpawnCrater(rot, point + nr * SpawnOffsetFromGround);
            wasThrown = true;
        }
        public void SpawnCrater(Quaternion rotation, Vector3 pozition)
        {
            GameObject cr = Instantiate(GroundCraterPrefab, pozition, rotation);
            Destroy(cr, 5f);
        }
       
    }
}