// Saatan Historia - Objeto que Iktan puede recoger (molletes, la foto de Iktan...).
// Necesita un Collider marcado como Is Trigger. Al tocarlo Iktan, suma uno al inventario y desaparece.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class Recogible : MonoBehaviour
{
    public string objeto = "Mollete";
    public UnityEvent alRecoger;

    static readonly Dictionary<string, int> inventario = new Dictionary<string, int>();
    public static int Cantidad(string objeto) => inventario.TryGetValue(objeto, out int n) ? n : 0;

    void Reset() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerEnter(Collider otro)
    {
        if (otro.GetComponentInParent<ControlIktan>() == null) return;
        inventario[objeto] = Cantidad(objeto) + 1;
        alRecoger.Invoke();
        Debug.Log("Iktan recogió: " + objeto + " (" + Cantidad(objeto) + ")");
        Destroy(gameObject);
    }
}
