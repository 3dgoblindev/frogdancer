using UnityEngine;

/// <summary>
/// Define un TIPO de renacuajo: qué prefab usa y sus stats base.
/// Esto es un preset de diseño (ScriptableObject), no cambia en runtime.
/// Crear instancias desde Assets > Create > Frogs > Tadpole Type.
/// </summary>
[CreateAssetMenu(fileName = "TadpoleType", menuName = "Frogs/Tadpole Type")]
public class TadpoleType : ScriptableObject
{
    [Tooltip("Identificador interno (para logs, debug, referencias por código si hace falta).")]
    public string typeId = "renacuajo_comun";

    [Tooltip("Prefab a instanciar. Debe tener el componente Tadpole.")]
    public GameObject tadpolePrefab;

    [Header("Stats base de este tipo")]
    public int pointsValue = 10;
    public float moveSpeed = 1.5f;
}