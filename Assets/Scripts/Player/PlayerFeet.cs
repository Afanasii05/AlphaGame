using UnityEngine;
namespace Alpha.Player
{
    public static class PlayerFeet
    {
        //Pozitia talpilor: pivotul playerului e in centrul capsulei
        public static Vector3 Position(CapsuleCollider capsule)
        {
            Vector3 p = capsule.transform.position;
            return new Vector3(p.x, capsule.bounds.min.y, p.z);
        }
    }
}
