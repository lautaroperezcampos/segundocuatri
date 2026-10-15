using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// SubjefeVolador "especial" (jefe): hereda TODO de SubjefeVolador/Subjefe (misma logica,
// mismo vuelo al poseerlo), pero ademas tiene una secuencia de fases:
//   0) Parado arriba en el puente "farmeando aura", invulnerable, esperando que el jugador entre a la arena
//   1) Sale la oleada de minions (spawner) -> hay que matarlos a todos
//   2) Baja, se le puede pegar hasta que llega a "porcentajeFin1" de vida -> vuelve a subir
//   3) Salen subjefes (disparadores/golpeadores) -> hay que matarlos a todos
//   4) Baja de nuevo, se le pega hasta quedar con poca vida -> queda abajo y se puede POSEER
public class SubjefeVoladorJefe : SubjefeVolador
{
    [Header("Jefe - Arena")]
    public Collider2D zonaArena; // un objeto con Collider2D (Is Trigger) que cubre la arena; cuando el jugador entra, arranca
    public Transform puntoAlto; // donde queda parado farmeando aura (el puente, arriba a la derecha): ponelo justo donde apoyan sus pies
    public Transform puntoSuelo; // donde baja cuando se le puede pegar
    public float velocidadTransicion = 6f; // que tan rapido sube/baja

    [Header("Jefe - Fase 1 (minions)")]
    public SpawnerEnemigos spawnerMinions; // el spawner de la oleada de minions (puede quedar vacio)
    [Range(0f, 1f)]
    public float porcentajeFin1 = 0.65f; // en la primera bajada se le pega hasta quedar con este % de vida

    [Header("Jefe - Fase 2 (subjefes)")]
    public GameObject[] prefabsFase2; // los subjefes que salen (disparadores / golpeadores)
    public Transform[] puntosFase2; // donde aparece cada uno (mismo orden que los prefabs)
    // en la segunda bajada se le pega hasta "Porcentaje Vida Para Poseer" (el campo de Subjefe)

    [Header("Jefe - Paredes (opcional)")]
    public GameObject[] paredes; // se activan al empezar la pelea y se desactivan al poseerlo

    [Header("Jefe - Eventos")]
    public UnityEvent alTerminarFase1;
    public UnityEvent alQuedarPoseible;

    private bool invulnerable = true;
    private List<Subjefe> subjefesFase2 = new List<Subjefe>();

    protected override void Start()
    {
        base.Start();

        // arranca arriba, flotando y quieto, sin perseguir ni atacar
        persigueAlJugador = false;
        invulnerable = true;

        if (puntoAlto != null)
        {
            rb.position = puntoAlto.position;
            transform.position = puntoAlto.position;
        }

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (paredes != null)
        {
            foreach (GameObject pared in paredes)
            {
                if (pared != null) pared.SetActive(false);
            }
        }

        Debug.Log("[Jefe] Start OK. jugador=" + (jugador != null ? jugador.name : "NULL (falta tag Player)")
            + " | zonaArena=" + (zonaArena != null ? zonaArena.name : "VACIO")
            + " | puntoAlto=" + (puntoAlto != null ? puntoAlto.name : "VACIO")
            + " | puntoSuelo=" + (puntoSuelo != null ? puntoSuelo.name : "VACIO")
            + " | spawner=" + (spawnerMinions != null ? spawnerMinions.name : "VACIO"));

        StartCoroutine(RutinaJefe());
    }

    // mientras esta invulnerable no recibe dano, y nunca llega a morir:
    // lo maximo que se le puede sacar es hasta dejarlo con 1 de vida (despues se posee)
    public override void RecibirDaño(int cantidad)
    {
        if (invulnerable || estaPoseido || estaMuerto) return;

        int maximoPermitido = vidaActual - 1;
        if (maximoPermitido <= 0) return;

        base.RecibirDaño(Mathf.Min(cantidad, maximoPermitido));
    }

    IEnumerator RutinaJefe()
    {
        // ---- 0) espera a que el jugador entre a la arena ----
        float proximoLog = 0f;
        while (!JugadorEnArena())
        {
            if (Time.time >= proximoLog)
            {
                proximoLog = Time.time + 2f;
                Debug.Log("[Jefe] esperando jugador en arena. jugador=" + (jugador != null ? jugador.position.ToString() : "NULL")
                    + " | zonaArena activa=" + (zonaArena != null && zonaArena.enabled && zonaArena.gameObject.activeInHierarchy)
                    + " | zona bounds=" + (zonaArena != null ? zonaArena.bounds.ToString() : "-"));
            }
            yield return null;
        }

        Debug.Log("[Jefe] jugador entro a la arena, empieza la pelea");

        PonerParedes(true);

        // ---- 1) oleada de minions ----
        if (spawnerMinions != null)
        {
            Debug.Log("[Jefe] activando spawner de minions");
            spawnerMinions.ActivarDesdeAfuera();

            // esperamos a que suelte todos Y no quede ninguno vivo
            while (!spawnerMinions.OleadaTerminada() || HayEnemigosVivos())
            {
                yield return new WaitForSeconds(0.25f);
            }
        }

        Debug.Log("[Jefe] minions muertos, baja");
        // ---- 2) primera bajada: se le pega hasta porcentajeFin1 ----
        yield return Bajar();
        yield return EsperarVidaHasta(porcentajeFin1);
        yield return Subir();

        Debug.Log("[Jefe] subio, salen los subjefes");
        alTerminarFase1.Invoke();

        // ---- 3) salen los subjefes ----
        SpawnearSubjefesFase2();

        while (HaySubjefesFase2Vivos())
        {
            yield return new WaitForSeconds(0.25f);
        }

        Debug.Log("[Jefe] subjefes muertos, baja de nuevo");
        // ---- 4) segunda bajada: se le pega hasta poder poseerlo ----
        yield return Bajar();
        yield return EsperarVidaHasta(porcentajeVidaParaPoseer);

        // queda abajo, sin atacar, para que el jugador lo posea
        persigueAlJugador = false;
        invulnerable = true; // asi no lo mata por accidente
        alQuedarPoseible.Invoke();

        while (!estaPoseido && !estaMuerto)
        {
            yield return null;
        }

        // lo poseyo: se abren las paredes y el jugador ya puede irse volando
        PonerParedes(false);
    }

    // ---------------- movimiento ----------------

    IEnumerator Bajar()
    {
        persigueAlJugador = false;
        invulnerable = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (puntoSuelo != null)
        {
            yield return IrAPunto(puntoSuelo.position);
        }

        // ya esta abajo: vuelve a ser un cuerpo normal, ataca y se le puede pegar
        rb.bodyType = RigidbodyType2D.Dynamic;
        persigueAlJugador = true;
        invulnerable = false;
    }

    IEnumerator Subir()
    {
        persigueAlJugador = false;
        invulnerable = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (puntoAlto != null)
        {
            yield return IrAPunto(puntoAlto.position);
        }
    }

    IEnumerator IrAPunto(Vector3 destino)
    {
        while (Vector2.Distance(rb.position, destino) > 0.05f)
        {
            if (estaPoseido || estaMuerto) yield break;

            rb.position = Vector2.MoveTowards(rb.position, destino, velocidadTransicion * Time.deltaTime);
            yield return null;
        }

        rb.position = destino;
    }

    IEnumerator EsperarVidaHasta(float porcentaje)
    {
        while (vidaActual > vidaMaxima * porcentaje && !estaPoseido && !estaMuerto)
        {
            yield return null;
        }
    }

    // ---------------- ayudas ----------------

    bool JugadorEnArena()
    {
        if (jugador == null) return false;
        if (zonaArena == null) return true; // sin zona: arranca directo
        return zonaArena.OverlapPoint(jugador.position);
    }

    bool HayEnemigosVivos()
    {
        return FindObjectsByType<Enemigo>(FindObjectsSortMode.None).Length > 0;
    }

    void SpawnearSubjefesFase2()
    {
        subjefesFase2.Clear();

        if (prefabsFase2 == null) return;

        for (int i = 0; i < prefabsFase2.Length; i++)
        {
            if (prefabsFase2[i] == null) continue;
            if (puntosFase2 == null || i >= puntosFase2.Length || puntosFase2[i] == null) continue;

            GameObject nuevo = Instantiate(prefabsFase2[i], puntosFase2[i].position, Quaternion.identity);
            Subjefe subjefe = nuevo.GetComponent<Subjefe>();
            if (subjefe != null) subjefesFase2.Add(subjefe);
        }
    }

    bool HaySubjefesFase2Vivos()
    {
        subjefesFase2.RemoveAll(x => x == null);

        foreach (Subjefe s in subjefesFase2)
        {
            if (!s.estaMuerto) return true;
        }

        return false;
    }

    void PonerParedes(bool activas)
    {
        if (paredes == null) return;

        foreach (GameObject pared in paredes)
        {
            if (pared != null) pared.SetActive(activas);
        }
    }
}