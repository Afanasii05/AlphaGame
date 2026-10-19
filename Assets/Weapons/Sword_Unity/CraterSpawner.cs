using UnityEngine;
namespace Alpha.Combat
{
    public class CraterSpawner : MonoBehaviour
    {
        [SerializeField] float SpawnOffsetFromGround = 0.05f;
        [SerializeField] GameObject GroundCraterPrefab;
        [SerializeField] Weapon wp;
        [SerializeField] Collider headColider;
        private bool canSpawn = false;

        private void OnEnable()
        {
            wp.onHeldChange += ChangeStateOfSpawn;
        }
        private void OnDisable()
        {
            wp.onHeldChange -= ChangeStateOfSpawn;
        }
        private void ChangeStateOfSpawn(bool held)
        {
            canSpawn = !held;
        }
        private void OnCollisionEnter(Collision collision)
        {

            if (wp.IsHeld || !canSpawn)
                return;

            ContactPoint cont = collision.GetContact(0);
            if (cont.thisCollider != headColider)
                return;
            Vector3 point = cont.point;
            Vector3 nr = cont.normal;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, nr);
            SpawnCrater(rot, point + nr * SpawnOffsetFromGround);
            canSpawn = false;
        }
       
        public void SpawnCrater(Quaternion rotation, Vector3 pozition)
        { 
            GameObject cr = Instantiate(GroundCraterPrefab, pozition, rotation);
            Destroy(cr, 5f);
        }
       
    }
}