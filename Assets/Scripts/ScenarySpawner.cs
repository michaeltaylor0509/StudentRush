using System.Collections.Generic;
using UnityEngine;

public class ScenerySpawner : MonoBehaviour
{
    [Header("Calles normales (los 3 arrays con el mismo tamaño)")]
    public GameObject[] streetPrefabs;   // calles que salen al azar
    public int[] weights;                // probabilidad de cada una
    public int[] maxInARow;              // máximo de veces seguidas de cada una

    [Header("Calles especiales")]
    public GameObject emptyStreetPrefab; // Inicial: las primeras calles vacías
    public int startEmptyTiles = 3;
    public GameObject crossroadPrefab;   // el cruce
    public int streetsBetweenCrossroads = 2; // calles normales entre cruces (2 para probar, luego ~8)

    [Header("Cruce")]
    public float decisionDistance = 10f; // metros antes del centro a los que se mira el carril y EMPIEZA el giro
    public int armStreets = 4;           // calles que se preparan por cada lado del cruce (minimo 3)

    [Header("Medidas")]
    public float tileScale = 1f;
    public float streetLength = 30f;
    public float offsetX = 0f;
    public float offsetY = 0f;
    public float viewDistance = 120f;    // hasta donde generamos por delante
    public float startZ = -10f;          // donde empieza la primera calle
    public float recycleZ = -45f;        // por detras de esto se recicla

    // las calles de los dos brazos de un cruce
    class CrossArms
    {
        public List<GameObject> left = new List<GameObject>();
        public List<GameObject> right = new List<GameObject>();
    }

    List<GameObject> active = new List<GameObject>();   // calles en pantalla (la primera es la mas cercana)
    List<GameObject> stored = new List<GameObject>();   // calles apagadas guardadas para reutilizar
    HashSet<GameObject> decided = new HashSet<GameObject>(); // cruces que ya hemos procesado
    Dictionary<GameObject, CrossArms> armsOf = new Dictionary<GameObject, CrossArms>(); // brazos de cada cruce

    float nextZ;            // donde va el origen de la siguiente calle
    int placed = 0;         // calles puestas en total
    int sinceCrossroad = 0; // calles normales desde el ultimo cruce
    int lastType = -1;      // ultima calle normal elegida
    int repeats = 0;        // veces seguidas que salio
    PlayerController player;

    // datos del giro en curso
    bool turning = false;
    float turnElapsed = 0f;     // tiempo que llevamos girando
    float turnDuration = 0.5f;  // se calcula en cada giro segun la velocidad
    float turnTotal = 0f;       // grados totales (+90 izquierda, -90 derecha)
    Vector3 turnPivot;          // centro de la curva (a un lado de AJ)
    Vector3[] startPos;         // posicion inicial de cada calle al empezar el giro
    Quaternion[] startRot;      // rotacion inicial de cada calle al empezar el giro
    GameObject turnCross;       // el cruce en el que estamos girando

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>(); // buscamos al jugador solo
        nextZ = startZ;
        Fill();
    }

    void Update()
    {
        // mientras gira, solo rotamos el mundo: no lo movemos ni generamos calles
        if (turning)
        {
            DoTurn();
            return;
        }

        // el mundo solo se mueve si se esta jugando (no en menu, pausa ni game over)
        float move = 0f;
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.IsPlaying()) move = gm.GetSpeed() * Time.deltaTime;

        // movemos todas las calles hacia el jugador (hacia -Z)
        foreach (GameObject s in active)
            s.transform.position += Vector3.back * move;
        nextZ -= move;

        // si la primera ya quedo muy atras la apagamos y la guardamos
        if (active.Count > 0 && active[0].transform.position.z + streetLength < recycleZ)
        {
            GameObject old = active[0];
            active.RemoveAt(0);
            decided.Remove(old);
            armsOf.Remove(old);
            old.SetActive(false);
            stored.Add(old);
        }

        Fill();
        CheckCrossroad();
    }

    // pone calles por delante hasta llenar la distancia de vision
    void Fill()
    {
        while (nextZ < viewDistance)
        {
            GameObject prefab = ChoosePrefab();
            GameObject street = GetStreet(prefab);
            street.transform.rotation = Quaternion.identity; // por si venia girada de un cruce
            street.transform.position = new Vector3(offsetX, offsetY, nextZ);
            active.Add(street);

            // si es un cruce, preparamos tambien sus dos brazos
            if (prefab == crossroadPrefab) PlaceArms(street);

            nextZ += streetLength;
            placed++;
        }
    }

    // pone las calles de la izquierda y de la derecha de un cruce
    void PlaceArms(GameObject cross)
    {
        Transform centro = cross.transform.Find("Centro");
        if (centro == null) return;

        // largo del brazo del propio cruce (del centro al borde), medido con el modelo sin girar
        Renderer r = cross.GetComponentInChildren<Renderer>();
        float armLen = (r != null) ? r.bounds.extents.x : streetLength / 2f;

        CrossArms arms = new CrossArms();
        for (int k = 0; k < armStreets; k++)
        {
            // distancia del centro del cruce al origen de esta calle del brazo
            float dist = armLen + k * streetLength;

            // la primera calle de cada brazo es la vacia (entrada segura sin obstaculos)
            GameObject prefabL = (k == 0 && emptyStreetPrefab != null) ? emptyStreetPrefab : ChooseStreet();
            GameObject prefabR = (k == 0 && emptyStreetPrefab != null) ? emptyStreetPrefab : ChooseStreet();

            arms.left.Add(PlaceArmStreet(prefabL, centro.position, -1f, dist));
            arms.right.Add(PlaceArmStreet(prefabR, centro.position, 1f, dist));
        }
        armsOf[cross] = arms;
    }

    // side = -1 izquierda, +1 derecha. La calle se gira para que su "delante" apunte hacia ese lado
    GameObject PlaceArmStreet(GameObject prefab, Vector3 center, float side, float dist)
    {
        GameObject s = GetStreet(prefab);
        s.transform.rotation = Quaternion.Euler(0f, 90f * side, 0f); // -90: mira a -X, +90: mira a +X
        s.transform.position = new Vector3(center.x + side * dist, offsetY, center.z);
        active.Add(s);
        return s;
    }

    // decide que toca poner: inicial, cruce o calle normal
    GameObject ChoosePrefab()
    {
        if (placed < startEmptyTiles) return emptyStreetPrefab;

        if (crossroadPrefab != null && sinceCrossroad >= streetsBetweenCrossroads)
        {
            sinceCrossroad = 0;
            return crossroadPrefab;
        }

        sinceCrossroad++;
        return ChooseStreet();
    }

    // ruleta con pesos, evitando repetir una calle mas veces de las permitidas
    GameObject ChooseStreet()
    {
        int total = 0;
        for (int i = 0; i < streetPrefabs.Length; i++)
        {
            if (i == lastType && repeats >= maxInARow[i]) continue; // esta ya se repitio el maximo
            total += weights[i];
        }
        if (total <= 0) return streetPrefabs[0]; // por seguridad

        int roll = Random.Range(0, total);
        for (int i = 0; i < streetPrefabs.Length; i++)
        {
            if (i == lastType && repeats >= maxInARow[i]) continue;
            if (roll < weights[i])
            {
                if (i == lastType) repeats++; else { lastType = i; repeats = 1; }
                return streetPrefabs[i];
            }
            roll -= weights[i];
        }
        return streetPrefabs[0];
    }

    // busca una calle apagada del mismo tipo; si no hay, crea una nueva
    GameObject GetStreet(GameObject prefab)
    {
        for (int i = 0; i < stored.Count; i++)
        {
            if (stored[i].name == prefab.name + "(Clone)")
            {
                GameObject s = stored[i];
                stored.RemoveAt(i);
                s.SetActive(true);
                return s;
            }
        }
        GameObject n = Instantiate(prefab, transform);
        n.transform.localScale = Vector3.one * tileScale;
        return n;
    }

    // cuando un cruce esta a decisionDistance metros, miramos el carril y giramos YA
    void CheckCrossroad()
    {
        if (crossroadPrefab == null || player == null) return;

        foreach (GameObject s in active)
        {
            if (s.name != crossroadPrefab.name + "(Clone)") continue; // no es un cruce
            if (decided.Contains(s)) continue;                        // este ya lo procesamos

            Transform centro = s.transform.Find("Centro");
            if (centro == null) continue;

            // metros que le faltan al centro del cruce para llegar a AJ
            float distance = centro.position.z - player.transform.position.z;
            if (distance > decisionDistance) continue; // todavia esta lejos

            decided.Add(s);
            string direction = DirectionFromLane(player.GetLane());
            Debug.Log("CRUCE: " + direction);

            if (direction == "IZQUIERDA") StartTurn(s, 90f, distance);
            else if (direction == "DERECHA") StartTurn(s, -90f, distance);
            // RECTO: no hacemos nada, el mundo sigue su camino
            return;
        }
    }

    // prepara los datos del giro. 'distance' = metros que faltan hasta el centro del cruce
    void StartTurn(GameObject cross, float degrees, float distance)
    {
        turnCross = cross;
        turnTotal = degrees;
        turnElapsed = 0f;

        // centro de la curva: a la altura de AJ y 'distance' metros al lado del eje de la carretera
        // (izquierda = -X, derecha = +X). Asi la carretera nueva queda centrada y AJ sigue en su carril
        float side = (degrees > 0f) ? -1f : 1f;
        turnPivot = new Vector3(offsetX + side * distance, 0f, player.transform.position.z);

        // duracion = lo que tarda el mundo en recorrer el cuarto de circulo a la velocidad del juego
        float radius = Mathf.Abs(player.transform.position.x - turnPivot.x);
        float speed = (GameManager.Instance != null) ? GameManager.Instance.GetSpeed() : 10f;
        turnDuration = Mathf.Max(0.2f, (Mathf.PI / 2f * radius) / Mathf.Max(speed, 0.1f));

        // guardamos como estaba cada calle para calcular el giro desde ahi
        startPos = new Vector3[active.Count];
        startRot = new Quaternion[active.Count];
        for (int i = 0; i < active.Count; i++)
        {
            startPos[i] = active[i].transform.position;
            startRot[i] = active[i].transform.rotation;
        }

        turning = true;
    }

    // cada frame colocamos todas las calles segun cuanto va del giro (0 a 1)
    void DoTurn()
    {
        turnElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(turnElapsed / turnDuration); // giro lineal: velocidad constante

        Quaternion q = Quaternion.Euler(0f, turnTotal * t, 0f); // giro acumulado

        for (int i = 0; i < active.Count; i++)
        {
            Transform tr = active[i].transform;
            // giramos la posicion inicial alrededor del centro de la curva
            tr.position = turnPivot + q * (startPos[i] - turnPivot);
            tr.rotation = q * startRot[i];
        }

        if (t >= 1f) FinishTurn();
    }

    // al acabar: nos quedamos con el cruce y el brazo elegido, y apagamos todo lo demas
    void FinishTurn()
    {
        turning = false;

        // calles del brazo elegido: izquierda si giramos +90, derecha si -90
        List<GameObject> keep = new List<GameObject>();
        CrossArms arms;
        if (armsOf.TryGetValue(turnCross, out arms))
            keep = (turnTotal > 0f) ? arms.left : arms.right;
        armsOf.Remove(turnCross);

        // apagamos todo lo que no sea el cruce ni el brazo elegido
        for (int i = active.Count - 1; i >= 0; i--)
        {
            GameObject s = active[i];
            if (s == turnCross || keep.Contains(s)) continue;
            decided.Remove(s);
            armsOf.Remove(s);
            s.SetActive(false);
            stored.Add(s);
        }

        // la lista queda: cruce + brazo elegido (en orden)
        active.Clear();
        active.Add(turnCross);
        active.AddRange(keep);

        // seguimos generando justo detras de la ultima calle del brazo
        if (keep.Count > 0)
        {
            nextZ = keep[keep.Count - 1].transform.position.z + streetLength;
        }
        else
        {
            // por si no hay brazos: usamos el final del modelo del cruce
            Renderer r = turnCross.GetComponentInChildren<Renderer>();
            nextZ = (r != null) ? r.bounds.max.z : player.transform.position.z + streetLength / 2f;
        }

        sinceCrossroad = 0; // empezamos a contar calles hasta el proximo cruce
    }

    // carril 0 = izquierda, carril 3 = derecha, los del medio = recto
    string DirectionFromLane(int lane)
    {
        if (lane == 0) return "IZQUIERDA";
        if (lane == 3) return "DERECHA";
        return "RECTO";
    }
}