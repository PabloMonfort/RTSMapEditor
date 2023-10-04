using UnityEngine;
using System.Collections;
public class ControladorDeCamara : MonoBehaviour
{
    private Transform m_Transform;
    public bool usarFixedUpdate = false;

    public float velocidadSeguimiento = 5f; // Velocidad al seguir un objetivo
    public float velocidadMovimientoBordePantalla = 3f; // Velocidad con movimiento del borde de la pantalla
    public float velocidadMovimientoTeclado = 5f; // Velocidad con movimiento del teclado
    
    public float velocidadRotacion = 3f;
    public float velocidadPan = 10f;
    public float velocidadRotacionMouse = 10f;

    public bool limitarMapa = true;

    public float límiteX = 50f; // Límite en el eje X del mapa
    public float límiteY = 50f; // Límite en el eje Z del mapa

    public Transform objetivoSeguimiento; // Objetivo a seguir
    public Vector3 compensaciónObjetivo;

    public bool usarEntradaBordePantalla = true;
    public float bordePantalla = 25f;

    public bool usarEntradaTeclado = true;
    public string ejeHorizontal = "Horizontal";
    public string ejeVertical = "Vertical";

    public bool usarPan = true;
    public KeyCode teclaPan = KeyCode.Mouse2;

    public bool usarZoomTeclado = true;
    public KeyCode teclaAcercar = KeyCode.E;
    public KeyCode teclaAlejar = KeyCode.Q;

    public bool usarRotaciónTeclado = true;
    public KeyCode teclaGirarDerecha = KeyCode.X;
    public KeyCode teclaGirarIzquierda = KeyCode.Z;

    public bool usarRotaciónMouse = true;
    public KeyCode teclaRotaciónMouse = KeyCode.Mouse1;

    private void Start()
    {
        m_Transform = transform;
    }

    private void Update()
    {
        if (!usarFixedUpdate)
            ActualizarCámara();
    }

    private void FixedUpdate()
    {
        if (usarFixedUpdate)
            ActualizarCámara();
    }
    private void ActualizarCámara()
    {
        if (SiguiendoObjetivo)
            SeguirObjetivo();
        else
            Mover();

        Rotación();
        LimitarPosición();
    }
    private void Mover()
    {
        if (usarEntradaTeclado)
        {
            Vector3 movimientoDeseado = new Vector3(EntradaTeclado.x, 0, EntradaTeclado.y);

            movimientoDeseado *= velocidadMovimientoTeclado;
            movimientoDeseado *= Time.deltaTime;
            movimientoDeseado = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, 0f)) * movimientoDeseado;
            movimientoDeseado = m_Transform.InverseTransformDirection(movimientoDeseado);

            m_Transform.Translate(movimientoDeseado, Space.Self);
        }

        if (usarEntradaBordePantalla)
        {
            Vector3 movimientoDeseado = new Vector3();

            Rect margenIzquierdo = new Rect(0, 0, bordePantalla, Screen.height);
            Rect margenDerecho = new Rect(Screen.width - bordePantalla, 0, bordePantalla, Screen.height);
            Rect margenArriba = new Rect(0, Screen.height - bordePantalla, Screen.width, bordePantalla);
            Rect margenAbajo = new Rect(0, 0, Screen.width, bordePantalla);

            movimientoDeseado.x = margenIzquierdo.Contains(EntradaMouse) ? -1 : margenDerecho.Contains(EntradaMouse) ? 1 : 0;
            movimientoDeseado.z = margenArriba.Contains(EntradaMouse) ? 1 : margenAbajo.Contains(EntradaMouse) ? -1 : 0;

            movimientoDeseado *= velocidadMovimientoBordePantalla;
            movimientoDeseado *= Time.deltaTime;
            movimientoDeseado = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, 0f)) * movimientoDeseado;
            movimientoDeseado = m_Transform.InverseTransformDirection(movimientoDeseado);

            m_Transform.Translate(movimientoDeseado, Space.Self);
        }

        if (usarPan && Input.GetKey(teclaPan) && EjeMouse != Vector2.zero)
        {
            Vector3 movimientoDeseado = new Vector3(-EjeMouse.x, 0, -EjeMouse.y);

            movimientoDeseado *= velocidadPan;
            movimientoDeseado *= Time.deltaTime;
            movimientoDeseado = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, 0f)) * movimientoDeseado;
            movimientoDeseado = m_Transform.InverseTransformDirection(movimientoDeseado);

            m_Transform.Translate(movimientoDeseado, Space.Self);
        }
    }
    private void Rotación()
    {
        if (usarRotaciónTeclado)
            transform.Rotate(Vector3.up, DirecciónRotación * Time.deltaTime * velocidadRotacion, Space.World);

        if (usarRotaciónMouse && Input.GetKey(teclaRotaciónMouse))
            m_Transform.Rotate(Vector3.up, -EjeMouse.x * Time.deltaTime * velocidadRotacionMouse, Space.World);
    }

    private void SeguirObjetivo()
    {
        Vector3 posiciónObjetivo = new Vector3(objetivoSeguimiento.position.x, m_Transform.position.y, objetivoSeguimiento.position.z) + compensaciónObjetivo;
        m_Transform.position = Vector3.MoveTowards(m_Transform.position, posiciónObjetivo, Time.deltaTime * velocidadSeguimiento);
    }
    private void LimitarPosición()
    {
        if (!limitarMapa)
            return;

        m_Transform.position = new Vector3(Mathf.Clamp(m_Transform.position.x, -límiteX, límiteX),
            m_Transform.position.y,
            Mathf.Clamp(m_Transform.position.z, -límiteY, límiteY));
    }
    public void EstablecerObjetivo(Transform objetivo)
    {
        objetivoSeguimiento = objetivo;
    }
    public void RestablecerObjetivo()
    {
        objetivoSeguimiento = null;
    }

    public bool SiguiendoObjetivo
    {
        get
        {
            return objetivoSeguimiento != null;
        }
    }
    private Vector2 EntradaTeclado
    {
        get { return usarEntradaTeclado ? new Vector2(Input.GetAxis(ejeHorizontal), Input.GetAxis(ejeVertical)) : Vector2.zero; }
    }

    private Vector2 EntradaMouse
    {
        get { return Input.mousePosition; }
    }
    private Vector2 EjeMouse
    {
        get { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); }
    }
    private int DirecciónRotación
    {
        get
        {
            bool girarDerecha = Input.GetKey(teclaGirarDerecha);
            bool girarIzquierda = Input.GetKey(teclaGirarIzquierda);
            if (girarIzquierda && girarDerecha)
                return 0;
            else if (girarIzquierda && !girarDerecha)
                return -1;
            else if (!girarIzquierda && girarDerecha)
                return 1;
            else
                return 0;
        }
    }
}