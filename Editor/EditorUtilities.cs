using UnityEngine;
using UnityEditor;

public static class EditorUtilities
{
    [MenuItem("Tools/Editor Utilities/Remove Missing Mono Scripts")]
    public static void RemoveMissingMonoScripts()
    {
        GameObject[] roots = Selection.gameObjects;

        if (roots == null || roots.Length == 0)
        {
            Debug.LogWarning("Selecciona al menos un GameObject en la jerarquía.");
            return;
        }

        int removedCount = 0;

        foreach (GameObject root in roots)
        {
            removedCount += RemoveMissingScriptsFromHierarchy(root);
        }

        Debug.Log($"Se eliminaron {removedCount} Missing Mono Scripts.");
    }

    private static int RemoveMissingScriptsFromHierarchy(GameObject root)
    {
        int removedCount = 0;

        // Revisar el objeto raíz
        removedCount += RemoveMissingScripts(root);

        // Revisar todos sus hijos
        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.gameObject == root)
                continue;

            removedCount += RemoveMissingScripts(child.gameObject);
        }

        return removedCount;
    }

    private static int RemoveMissingScripts(GameObject gameObject)
    {
        int removedCount = 0;

        // Cuenta los componentes MonoBehaviour que están en Missing
        int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);

        if (missingCount <= 0)
            return 0;

        // Elimina todos los Missing Mono Scripts
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);

        removedCount += missingCount;

        EditorUtility.SetDirty(gameObject);

        return removedCount;
    }

    [MenuItem("Tools/Editor Utilities/Remove Missing Scripts From Entire Scene")]
    public static void RemoveMissingScriptsFromEntireScene()
    {
        int removedCount = 0;

        // Obtiene todos los objetos raíz, incluyendo los inactivos
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager
            .GetActiveScene()
            .GetRootGameObjects();

        foreach (GameObject root in rootObjects)
        {
            removedCount += RemoveMissingScriptsFromHierarchy(root);
        }

        Debug.Log($"[EditorUtilities] Se eliminaron {removedCount} Missing Mono Scripts de toda la escena.");
    }
}