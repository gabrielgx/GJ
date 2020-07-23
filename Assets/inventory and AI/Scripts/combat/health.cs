using UnityEngine;

namespace FGJ.Combat
{
    public class health : MonoBehaviour
    {
        public float maxHealth;
        [HideInInspector] float HP;

        private void Start() 
        {
            HP = maxHealth;
        }

        public void takeDamage(float damage)
        {
            HP = Mathf.Clamp(HP - damage, 0f, maxHealth);
            if(HP <= 0f)
            {
                if(GetComponent<Icontroller>() != null)
                {
                    GetComponent<Icontroller>().died();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        public void healHealth(float healAmount)
        {
            HP = Mathf.Clamp(HP + healAmount, 0f, maxHealth);
        }
    }
}
