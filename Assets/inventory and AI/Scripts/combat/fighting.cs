using UnityEngine;

namespace FGJ.Combat
{
    public class fighting : MonoBehaviour
    {
        public equipment gun;
        public equipment melee;
        [HideInInspector] public health target;

        public void hit(float damage)
        {
            if(target != null)
            {
                target.takeDamage(damage);
            }
        }

        public void meleeHit()
        {
            hit(melee.damage);
        }

        public void gunHit()
        {
            hit(gun.damage);
        }
    }
}
