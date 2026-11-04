using System.Collections.Generic;
using UnityEngine;

// Pone las calles una detrás de otra y las mueve hacia el jugador.
// Las que quedan atrás se apagan y se reutilizan.
public class ScenerySpawner : MonoBehaviour
{
    public GameObject[] streetPrefabs;    // las calles (sin la Inicial)
    public float[] weights;               // más peso = sale más (uno por calle)
    public int[] maxInARow;               // máximo de veces seguidas (uno por calle)

    public GameObject emptyStreetPrefab;  // calle Inicial, sin obstáculos
    public int startEmptyTiles = 3;       // calles vacías al empezar
    public float tileScale = 3f;          // tamaño de las calles

    public float streetLength = 30f;      // largo de cada calle
    public float offsetX = 0f;            // para centrar la carretera
    public float offsetY = 0f;            // para que AJ pise el suelo

    public float viewDistance = 120f;     // hasta dónde hay calle por delante
    public float startZ = -10f;           // dónde empieza la primera
    public float recycleZ = -45f;         // si una calle pasa de aquí, se apaga

    List<GameObject> active = new List<GameObject>(); // calles en pantalla
    List<GameObject> stored = new List<GameObject>(); // calles apagadas
    float nextZ;                                      // dónde empieza la siguiente
    int placedCount;                                  // cuántas he puesto
    int lastType = -1;                                // tipo de la última calle
    int repeats = 0;                                  // veces seguidas que ha salido

    void Start()
    {
        nextZ = startZ;
        Fill();
    }

    void Update()
    {
        // lo que se mueve el mundo este frame (0 si hay pausa o game over)
        float move = GameManager.Instance.GetSpeed() * Time.deltaTime;

        // muevo todas las calles hacia el jugador
        foreach (GameObject street in active)
            street.transform.position += Vector3.back * move;
        nextZ -= move;

        // si la primera ya quedó detrás, la apago y la guardo
        if (active.Count > 0 && active[0].transform.position.z + streetLength < recycleZ)
        {
            GameObject old = active[0];
            active.RemoveAt(0);
            old.SetActive(false);
            stored.Add(old);
        }

        Fill();
    }

    // pone calles hasta llegar a la distancia de visión
    void Fill()
    {
        while (nextZ < viewDistance)
        {
            GameObject prefab;

            // las primeras son vacías, después elijo con pesos
            if (placedCount < startEmptyTiles)
                prefab = emptyStreetPrefab;
            else
                prefab = streetPrefabs[ChooseStreet()];

            GameObject street = GetStreet(prefab);
            street.transform.position = new Vector3(offsetX, offsetY, nextZ);
            street.SetActive(true);

            active.Add(street);
            nextZ += streetLength;
            placedCount++;
        }
    }

    // devuelve el número de la calle elegida (ruleta con pesos)
    int ChooseStreet()
    {
        // 1) sumo los pesos de las calles permitidas
        float total = 0f;
        for (int i = 0; i < streetPrefabs.Length; i++)
        {
            if (i == lastType && repeats >= maxInARow[i]) continue; // ya salió demasiadas veces seguidas
            total += weights[i];
        }

        // 2) saco un número al azar y voy restando pesos hasta llegar a 0
        float r = Random.Range(0f, total);
        int chosen = 0;
        for (int i = 0; i < streetPrefabs.Length; i++)
        {
            if (i == lastType && repeats >= maxInARow[i]) continue;
            r -= weights[i];
            if (r <= 0f) { chosen = i; break; }
        }

        // 3) apunto cuántas veces seguidas sale
        if (chosen == lastType) repeats++;
        else { lastType = chosen; repeats = 1; }

        return chosen;
    }

    // busca una calle guardada de ese prefab; si no hay, crea una nueva
    GameObject GetStreet(GameObject prefab)
    {
        foreach (GameObject s in stored)
        {
            if (s.name == prefab.name + "(Clone)") // así llama Unity a las copias
            {
                stored.Remove(s);
                return s;
            }
        }

        GameObject go = Instantiate(prefab, transform);
        go.transform.localScale = Vector3.one * tileScale;
        return go;
    }
}