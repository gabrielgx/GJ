using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FGJ.Core
{
    public class spawner : MonoBehaviour
    {
        [SerializeField] float timeToSpawn;
        [SerializeField] GameObject objectToSpawn;
        private float spawnTimer;
        private GameObject player;
        private GameObject spawnedObject;

        private void Start() 
        {
            spawnNew();
            player = GameObject.FindGameObjectWithTag("Player");
        }
        private void Update() 
        {
            if(Vector3.Distance(transform.position, player.transform.position) < 50 && Vector3.Distance(transform.position, player.transform.position) > 10)
            {
                if(objectToSpawn != null && spawnedObject == null)
                {
                    spawnTimer += Time.deltaTime;
                    if(spawnTimer > timeToSpawn)
                    {
                        spawnNew();
                    }
                }
            }
            else if(Vector3.Distance(transform.position, player.transform.position) >= 50)
            {
                if(spawnedObject != null)
                {
                    Manager.instance.setCombat(false);
                    Destroy(spawnedObject);
                    spawnTimer = timeToSpawn + 1;
                }
            }
        }
        private void spawnNew()
        {
            if(objectToSpawn != null)
            {
                spawnedObject = Instantiate(objectToSpawn, transform.position, Quaternion.identity, transform);
                spawnTimer = 0f;
            }
        }
    }
}
