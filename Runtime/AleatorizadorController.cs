using EMT;
using EMT.Core;
using NaughtyAttributes;
using UnityEngine;

public class AleatorizadorController : MonoBehaviour
{
    [BoxGroup("Posicion")]
    public bool posicion;

    [ShowIf("posicion")]
    [BoxGroup("Posicion")]
    public bool usarLocal;

    [ShowIf("usarLocal")]
    [BoxGroup("Posicion")]
    [Required]
    public Transform padre;

    [ShowIf("posicion")]
    [BoxGroup("Posicion")]
    public Vector3 posIncio;

    [ShowIf("posicion")]
    [BoxGroup("Posicion")]
    public Vector3 posFinal;

    [ShowIf("posicion")]
    [BoxGroup("Posicion")]
    public Color posGizmosColor = Color.yellow;

    private void Awake()
    {
        Probar();
    }

    [Button]
    public void Probar()
    {
        if (!posicion) return;

        var posInicioFix = padre != null && usarLocal ? padre.position + posIncio : posIncio;
        var posFinalFix = padre != null && usarLocal ? padre.position + posFinal : posFinal;

        transform.position = Utilities.RandomRange(posInicioFix, posFinalFix);
    }

    [Button]
    public void CopiarPosicionInicial()
    {
        posIncio = transform.localPosition;
    }

    [Button]
    public void CopiarPosicionFinal()
    {
        posFinal = transform.localPosition;
    }

    private void OnDrawGizmosSelected()
    {
        if (posicion)
        {
            Gizmos.color = posGizmosColor;

            var posInicioFix = padre != null && usarLocal ? padre.position + posIncio : posIncio;
            var posFinalFix = padre != null && usarLocal ? padre.position + posFinal : posFinal;

            Gizmos.DrawWireSphere(posInicioFix, 1);
            Gizmos.DrawWireSphere(posFinalFix, 1);
            Gizmos.DrawLine(posInicioFix, posFinalFix);
        }
    }
}
