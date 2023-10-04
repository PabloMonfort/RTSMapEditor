using UnityEngine;
using System.Collections;

public class ControladorDeZoom : MonoBehaviour 
{
    public float sensibilidadZoomTeclado = 25f;
    public float sensibilidadZoomRuedaScroll = 25f;
    public bool usarZoomRuedaScroll = true;
    public bool usarZoomTeclado = true;
    public string ejeZoom = "Mouse ScrollWheel";
    private void Start()
    {
    }

    private void Update()
    {
        if (usarZoomRuedaScroll)
        {
            if (RuedaScroll > 0)
            {
                if (Camera.main.transform.position.y > 25)
                {
                    Camera.main.transform.position += Camera.main.transform.forward * RuedaScroll * sensibilidadZoomRuedaScroll;
                }
            }
            else if (RuedaScroll < 0)
            {
                if (Camera.main.transform.position.y < 35)
                {
                    Camera.main.transform.position += Camera.main.transform.forward * RuedaScroll * sensibilidadZoomRuedaScroll;
                }
            }
        }
        if (usarZoomTeclado)
        {
            if (RuedaScroll > 0)
            {
                if (Camera.main.transform.position.y > 25)
                {
                    Camera.main.transform.position += Camera.main.transform.forward * DirecciónZoom * sensibilidadZoomTeclado;
                }
            }
            else if (RuedaScroll < 0)
            {
                if (Camera.main.transform.position.y < 35)
                {
                    Camera.main.transform.position += Camera.main.transform.forward * DirecciónZoom * sensibilidadZoomTeclado;
                }
            }
        }
    }

    private int DirecciónZoom
    {
        get
        {
            bool acercarZoom = Input.GetKey(KeyCode.Q);
            bool alejarZoom = Input.GetKey(KeyCode.E);
            if (acercarZoom && alejarZoom)
                return 0;
            else if (!acercarZoom && alejarZoom)
                return 1;
            else if (acercarZoom && !alejarZoom)
                return -1;
            else
                return 0;
        }
    }
    private float RuedaScroll
    {
        get { return Input.GetAxis(ejeZoom); }
    }
}
