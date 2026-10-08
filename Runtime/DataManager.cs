using UnityEngine;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EMT;
using EMT.Core;

public class DataManager : MonoBehaviour, ISingleton
{
    [HideInInspector] public Data currentData;

    private string DataPath => Path.Combine(Application.persistentDataPath, "data.json");
    private string BackupPath => Path.Combine(Application.persistentDataPath, "data_backup.json");
    private string TempPath => Path.Combine(Application.persistentDataPath, "data.tmp");

    public GameObject verificacionIdioma;
    bool activarVerificacion = false;

    async void Awake()
    {
        await CargarDatosAsync();
    }

    #region Guardado

    public bool encriptar = true;

    public async Task GuardarDatosAsync()
    {
        try
        {
            Data copia = CrearCopia(currentData);

            string dataPath = DataPath;
            string backupPath = BackupPath;
            string tempPath = TempPath;

            await Task.Run(() =>
            {
                string json = JsonConvert.SerializeObject(copia);
                string encrypted = AESUtil.Encrypt(json);

                // Backup del archivo anterior
                if (File.Exists(dataPath))
                {
                    File.Copy(dataPath, backupPath, true);
                }

                // Escritura atómica
                File.WriteAllText(tempPath, encriptar ? encrypted : json);

                if (File.Exists(dataPath))
                    File.Delete(dataPath);

                File.Move(tempPath, dataPath);
            });

            Debug.Log("Archivo guardado");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error guardando datos:\n{ex}");
        }
    }

    #endregion

    #region Carga

    public async Task CargarDatosAsync()
    {
        string dataPath = DataPath;
        string backupPath = BackupPath;     

        Data datos = await Task.Run(() =>
        {
            // 1. Intentar cargar principal
            Data d = IntentarLeer(dataPath);

            if (d != null) return d;
                
            // 2. Intentar cargar backup
            Debug.LogWarning("Intentando restaurar desde copia de seguridad...");

            d = IntentarLeer(backupPath);

            if (d != null)
            {
                try
                {
                    File.Copy(backupPath, dataPath, true);
                }
                catch { }

                return d;
            }

            Debug.LogWarning("No se encontraron datos, creando nuevos...");

            // 3. Crear datos si no regresa nada desde datos locales o si hay error de conversion

            d = new();

            Debug.Log("Se crearon nuevos datos");

            activarVerificacion = true;

            return d;
        });

        currentData = datos;

        NotificarTodosOnLoadLocalData();

        // Si no existía archivo, guardar inmediatamente
        if (!File.Exists(dataPath)) await GuardarDatosAsync();
    }

    private void Update()
    {
        if (activarVerificacion)
        {
            verificacionIdioma.SetActive(true);
            activarVerificacion = false;
        }
    }

    private Data IntentarLeer(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;


            string encrypted = File.ReadAllText(path);

            var json = string.Empty;

            if (encriptar)
            {
                if (!AESUtil.TryDecrypt(encrypted, out json))
                return null;
            }     

            return JsonConvert.DeserializeObject<Data>(encriptar ? json : encrypted);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Utilidades

    private Data CrearCopia(Data origen)
    {
        if (origen == null) throw new NullReferenceException("Origen es null");     

        Data copia = new();

        foreach (var campo in typeof(Data).GetFields(
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic))
        {
            if (campo.IsInitOnly)
                continue;

            object valor = campo.GetValue(origen);

            // Copia arrays para evitar compartir la misma referencia
            if (valor is Array array)
                valor = array.Clone();

            campo.SetValue(copia, valor);
        }

        return copia;
    }

    private void NotificarTodosOnLoadLocalData()
    {
        var listeners = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).OfType<IPostLoadData>();

        foreach (var listener in listeners)
        {
            listener.OnLoadLocalData();
        }
    }

    #endregion
}

public interface IPostLoadData
{
    public void OnLoadLocalData();
}

[Serializable]
public class Data
{
    public LenguageSettings lenguageSettings { get; private set; } = new();
    public SoundSettings soundSettings { get; private set; } = new();
}

public class LenguageSettings
{
    public Lenguage sellectedLenguage = Lenguage.UNSELECTED; 
}

public enum Lenguage
{
    UNSELECTED, SPANISH, ENGLISH
}

public class SoundSettings
{
    public int musicPercent = 90;
    public int SFXPercent = 90;
}