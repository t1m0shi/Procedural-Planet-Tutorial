using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolarSystem : MonoBehaviour
{
    [SerializeField] private Sun sun;
    private Sun theSun;
    [SerializeField] private Planet planet;
    public Planet[] planets;
    public int numPlanets; //how many planets in solar system
    public int minRange = 30;
    public int maxRange; //the range they can spawn away from the sun (radius)


    // Start is called before the first frame update
    void Start()
    {
        numPlanets = Random.Range(2, 9);
        maxRange = Random.Range(70, 110);

        theSun = Instantiate(sun, transform.position, Quaternion.identity);
        planets = new Planet[numPlanets];
        for (int i = 0; i < planets.Length; i++)
        {
            //calculate random distance within the range for each planet
            int randDist = Random.Range(minRange, maxRange);
            planets[i] = Instantiate(planet, new Vector3(0,0,0), Quaternion.identity);
            Vector2 tempP = Random.insideUnitCircle.normalized;
            planets[i].initPos = new Vector3(tempP.x, 0, tempP.y) * randDist;
            planets[i].sunDist = randDist;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
