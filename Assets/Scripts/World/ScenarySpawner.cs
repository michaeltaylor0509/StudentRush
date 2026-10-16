using System.Collections.Generic;
using UnityEngine;

// Pone las calles una detrás de otra y las mueve hacia el jugador.
// Las que quedan atrás se apagan y se reutilizan (pooling).
public class ScenerySpawner : MonoBehaviour
{
    public GameObject[] streetPrefabs;    // calles con obstáculos
    public GameObject emptyStreetPrefab;  // calle sin obstáculos
    public int startEmptyTiles = 3;       // calles vacías al empezar
    public float tileScale = 3f;          // tamaño de las calles

    public float streetLength = 30f;      // largo de cada calle
    public float offsetX = 0f;            // para centrar la carretera
    public float offsetY = 0f;            // para que AJ pise el suelo

    public float viewDistance = 120f;     // hasta dónde hay calle por delante
    public float startZ = -10f;           // dónde empieza la primera
    public float recycleZ = -45f;         // si una calle pasa de aquí, se apaga

    List<GameObject> active = new List<GameObject>(); // calles en pantalla, en orden
    List<GameObject> stored = new List<GameObject>(); // calles apagadas, para reutilizar
    float nextZ;                                      // dónde empieza la siguiente calle
    int placedCount;                                  // cuántas calles he puesto
    GameObject lastPrefab;                            // la última elegida, para no repetirla

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
        nextZ -= move; // el final de la fila también se acerca

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
            GameObject street = GetStreet(ChoosePrefab());

            street.transform.position = new Vector3(offsetX, offsetY, nextZ);
            street.SetActive(true);

            active.Add(street);
            nextZ += streetLength; // la siguiente empieza donde acaba esta
            placedCount++;
        }
    }

    // las primeras son vacías, después al azar sin repetir la anterior
    GameObject ChoosePrefab()
    {
        if (placedCount < startEmptyTiles && emptyStreetPrefab != null)
            return emptyStreetPrefab;

        GameObject chosen = streetPrefabs[Random.Range(0, streetPrefabs.Length)];

        // si sale la misma que la anterior, vuelvo a elegir
        while (streetPrefabs.Length > 1 && chosen == lastPrefab)
            chosen = streetPrefabs[Random.Range(0, streetPrefabs.Length)];

        lastPrefab = chosen;
        return chosen;
    }

    // busca una calle guardada de ese prefab; si no hay, crea una nueva
    GameObject GetStreet(GameObject prefab)
    {
        foreach (GameObject s in stored)
        {
            // las copias que crea Unity se llaman "NombreDelPrefab(Clone)"
            if (s.name == prefab.name + "(Clone)")
            {
                stored.Remove(s);
                return s;
            }
        }

        GameObject go = Instantiate(prefab, transform); // la creo dentro del spawner
        go.transform.localScale = Vector3.one * tileScale;
        return go;
    }
}