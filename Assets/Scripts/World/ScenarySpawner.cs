using System.Collections.Generic;
using UnityEngine;

// Pone las calles una detrás de otra a distancia fija y las mueve hacia el jugador.
// Las primeras calles son vacías. Las que quedan atrás se guardan y se reutilizan.
public class ScenerySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] streetPrefabs;   // Calles con obstáculos
    [SerializeField] private GameObject emptyStreetPrefab; // Calle sin obstáculos
    [SerializeField] private int startEmptyTiles = 3;      // Calles vacías al empezar
    [SerializeField] private float tileScale = 3f;         // Tamaño de las calles

    [Header("Los 3 números que ajustas tú")]
    [SerializeField] private float streetLength = 30f;     // Largo de cada calle (para que no haya huecos ni solapes)
    [SerializeField] private float offsetX = 0f;           // Mueve la calle a los lados hasta que AJ quede en medio de la carretera
    [SerializeField] private float offsetY = 0f;           // Sube o baja la calle hasta que AJ pise el suelo

    [SerializeField] private float viewDistance = 120f;    // Hasta dónde hay calle por delante
    [SerializeField] private float startZ = -10f;          // Dónde empieza la primera
    [SerializeField] private float recycleZ = -45f;        // Si el final de una calle pasa de aquí, se guarda

    private class Tile
    {
        public GameObject go;
        public GameObject prefab;
        public float startZ; // Dónde empieza esta calle
    }

    private readonly List<Tile> active = new List<Tile>();
    private readonly Dictionary<GameObject, Stack<Tile>> pool = new Dictionary<GameObject, Stack<Tile>>();

    private float nextZ;           // Dónde empieza la siguiente calle
    private int placedCount;
    private GameObject lastPrefab;

    private void Start()
    {
        nextZ = startZ;
        Fill();
    }

    private void Fill()
    {
        while (nextZ < viewDistance)
        {
            Tile tile = GetTile(ChoosePrefab());
            tile.startZ = nextZ;
            tile.go.transform.position = new Vector3(offsetX, offsetY, nextZ);
            tile.go.SetActive(true);

            active.Add(tile);
            nextZ += streetLength;
            placedCount++;
        }
    }

    private GameObject ChoosePrefab()
    {
        if (placedCount < startEmptyTiles && emptyStreetPrefab != null)
            return emptyStreetPrefab;

        GameObject chosen = streetPrefabs[Random.Range(0, streetPrefabs.Length)];

        // Que no salga la misma dos veces seguidas
        if (streetPrefabs.Length > 1)
        {
            while (chosen == lastPrefab)
                chosen = streetPrefabs[Random.Range(0, streetPrefabs.Length)];
        }

        lastPrefab = chosen;
        return chosen;
    }

    // Saca una calle guardada o crea una nueva
    private Tile GetTile(GameObject prefab)
    {
        if (pool.ContainsKey(prefab) && pool[prefab].Count > 0)
            return pool[prefab].Pop();

        GameObject go = Instantiate(prefab, transform);
        go.transform.localScale = Vector3.one * tileScale;
        return new Tile { go = go, prefab = prefab };
    }

    private void Update()
    {
        float move = GameManager.Instance.Speed * Time.deltaTime;

        foreach (Tile tile in active)
        {
            tile.startZ -= move;
            tile.go.transform.position = new Vector3(offsetX, offsetY, tile.startZ);
        }
        nextZ -= move;

        // Si la primera ya quedó detrás, la guardamos
        while (active.Count > 0 && active[0].startZ + streetLength < recycleZ)
        {
            Tile first = active[0];
            active.RemoveAt(0);
            first.go.SetActive(false);

            if (!pool.ContainsKey(first.prefab))
                pool[first.prefab] = new Stack<Tile>();
            pool[first.prefab].Push(first);
        }

        Fill();
    }
}