using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

// UnityEditor no existe en los builds: el using DEBE estar protegido,
// de lo contrario el proyecto no compila para plataforma.
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EMT
{
    /// <summary>
    /// Colección de utilidades generales: listas, texto, fechas, validaciones
    /// y ayudas para objetos/componentes de Unity.
    /// </summary>
    public static class Utilities
    {
        #region Constants & Cached Data

        private const string ShortDateFormat = "dd/MM/yy";

        /// <summary>Máximo de año de dos dígitos que se interpreta como 20xx (ver <see cref="ShortDateCulture"/>).</summary>
        private const int TwoDigitYearMax = 2099;

        /// <summary>
        /// Cultura invariante con TwoDigitYearMax ampliado.
        /// Por defecto .NET interpreta "yy" en el rango 1930-2029, por lo que
        /// una fecha como "01/01/35" se leería como 1935.
        /// </summary>
        private static readonly CultureInfo ShortDateCulture = CreateShortDateCulture();

        /// <summary>Formatos aceptados al convertir texto a fecha (se crea una sola vez).</summary>
        private static readonly string[] AcceptedDateFormats =
        {
            "yyyy-MM-dd",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ssK", // K admite "Z" y offsets como +01:00
            "dd-MM-yyyy",
            "MM-dd-yyyy",
            "dd/MM/yyyy",
            "MM/dd/yyyy"
        };

        private static readonly char[] DateSeparators = { '-', '/' };

        /// <summary>
        /// Valida: algo@dominio.ext. El dominio no puede tener etiquetas vacías
        /// (rechaza "a@b..c", "a@.b.c" y "a@b.c.").
        /// </summary>
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$",
            RegexOptions.CultureInvariant);

        private const int MinPasswordLength = 8;

        private static CultureInfo CreateShortDateCulture()
        {
            // Clone() devuelve una copia editable (la cultura invariante original es de solo lectura).
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();

            try
            {
                culture.DateTimeFormat.Calendar.TwoDigitYearMax = TwoDigitYearMax;
            }
            catch (InvalidOperationException)
            {
                // Si la plataforma no permite modificarlo, se usa el comportamiento por defecto.
            }

            return culture;
        }

        #endregion

        #region Collections

        /// <summary>
        /// Desordena los elementos de una lista (algoritmo Fisher-Yates).
        /// </summary>
        /// <typeparam name="T">Tipo de los elementos.</typeparam>
        /// <param name="list">Lista a desordenar (se modifica in situ).</param>
        public static void ShuffleList<T>(this IList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            int count = list.Count;

            // El último elemento ya queda en su sitio, no hace falta iterarlo.
            for (int i = 0; i < count - 1; i++)
            {
                int randomIndex = Random.Range(i, count); // max exclusivo en int
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }

        #endregion

        #region Encoding

        /// <summary>
        /// Convierte un texto (UTF-8) a Base64. Devuelve vacío si el texto es nulo o vacío.
        /// </summary>
        public static string ToBase64(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// Convierte un Base64 a texto (UTF-8). Devuelve vacío si es nulo o vacío.
        /// Lanza <see cref="FormatException"/> si el Base64 no es válido; usar
        /// <see cref="TryFromBase64"/> para evitar la excepción.
        /// </summary>
        public static string FromBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return string.Empty;

            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }

        /// <summary>
        /// Versión segura de <see cref="FromBase64"/>: devuelve false en lugar de lanzar excepción.
        /// </summary>
        public static bool TryFromBase64(string base64, out string result)
        {
            result = string.Empty;

            if (string.IsNullOrEmpty(base64))
                return true;

            try
            {
                result = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        #endregion

        #region Strings

        /// <summary>
        /// Devuelve la primera palabra de un texto (separada por cualquier espacio en blanco).
        /// No genera arrays intermedios.
        /// </summary>
        public static string GetFirstWord(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            int start = 0;
            while (char.IsWhiteSpace(text[start]))
                start++;

            int end = start;
            while (end < text.Length && !char.IsWhiteSpace(text[end]))
                end++;

            return text.Substring(start, end - start);
        }

        /// <summary>
        /// Trunca un texto al número de palabras indicado y añade
        /// puntos suspensivos si fue recortado.
        /// Si no hace falta recortar se devuelve el texto original sin modificar.
        /// </summary>
        /// <param name="text">Texto a truncar.</param>
        /// <param name="maxWords">Máximo de palabras (mayor que cero).</param>
        /// <param name="ellipsis">Sufijo que se añade al recortar.</param>
        public static string TruncateByWords(this string text, int maxWords, string ellipsis = "...")
        {
            if (maxWords <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxWords), "maxWords debe ser mayor que cero.");

            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Recorrido único sin Split: evita crear un array y un string por palabra.
            int firstWordStart = -1;
            int lastWordEnd = -1;
            int wordCount = 0;
            bool insideWord = false;

            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    if (insideWord)
                    {
                        insideWord = false;
                        lastWordEnd = i; // fin de la última palabra completa
                    }

                    continue;
                }

                if (insideWord)
                    continue;

                insideWord = true;
                wordCount++;

                // Empieza una palabra de más: se corta justo al final de la anterior.
                if (wordCount > maxWords)
                    return text.Substring(firstWordStart, lastWordEnd - firstWordStart) + ellipsis;

                if (firstWordStart < 0)
                    firstWordStart = i;
            }

            return text;
        }

        /// <summary>
        /// Elimina los espacios en blanco al inicio y al final del texto.
        /// </summary>
        public static string TrimEdges(this string text)
        {
            return string.IsNullOrEmpty(text) ? text : text.Trim();
        }

        /// <summary>
        /// Indica si el texto empieza con el carácter indicado.
        /// </summary>
        public static bool StartsWithChar(this string text, char character)
        {
            return !string.IsNullOrEmpty(text) && text[0] == character;
        }

        /// <summary>
        /// Indica si el texto termina con el carácter indicado.
        /// </summary>
        public static bool EndsWithChar(this string text, char character)
        {
            return !string.IsNullOrEmpty(text) && text[text.Length - 1] == character;
        }

        /// <summary>
        /// Recorta los extremos y colapsa los espacios (' ') repetidos en uno solo.
        /// No modifica tabulaciones ni saltos de línea.
        /// </summary>
        public static string NormalizeInnerSpaces(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return CollapseWhitespace(text, onlySpaces: true);
        }

        /// <summary>
        /// Recorta los extremos y colapsa cualquier secuencia de espacios en blanco
        /// (espacios, tabulaciones, saltos de línea) en un único espacio.
        /// Devuelve vacío si el texto es nulo o solo contiene espacios.
        /// </summary>
        public static string NormalizeWhitespace(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return CollapseWhitespace(text, onlySpaces: false);
        }

        /// <summary>
        /// Elimina las tildes y diéresis de las vocales (á, à, ä, â → a, etc.),
        /// respetando mayúsculas. La ñ y otros caracteres no se tocan.
        /// Si el texto no contiene caracteres acentuados se devuelve la misma instancia.
        /// </summary>
        public static string RemoveAccents(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Primera pasada: buscar el primer carácter a reemplazar (sin asignar memoria).
            int firstIndex = -1;
            for (int i = 0; i < text.Length; i++)
            {
                if (StripAccent(text[i]) != text[i])
                {
                    firstIndex = i;
                    break;
                }
            }

            if (firstIndex < 0)
                return text;

            // Segunda pasada: una sola copia del texto en lugar de una por cada Replace.
            char[] chars = text.ToCharArray();
            for (int i = firstIndex; i < chars.Length; i++)
                chars[i] = StripAccent(chars[i]);

            return new string(chars);
        }

        /// <summary>
        /// Devuelve el carácter sin acento (o el mismo carácter si no tiene).
        /// </summary>
        private static char StripAccent(char c)
        {
            switch (c)
            {
                case 'á': case 'à': case 'ä': case 'â': return 'a';
                case 'Á': case 'À': case 'Ä': case 'Â': return 'A';
                case 'é': case 'è': case 'ë': case 'ê': return 'e';
                case 'É': case 'È': case 'Ë': case 'Ê': return 'E';
                case 'í': case 'ì': case 'ï': case 'î': return 'i';
                case 'Í': case 'Ì': case 'Ï': case 'Î': return 'I';
                case 'ó': case 'ò': case 'ö': case 'ô': return 'o';
                case 'Ó': case 'Ò': case 'Ö': case 'Ô': return 'O';
                case 'ú': case 'ù': case 'ü': case 'û': return 'u';
                case 'Ú': case 'Ù': case 'Ü': case 'Û': return 'U';
                default: return c;
            }
        }

        /// <summary>
        /// Núcleo común de normalización: recorta los extremos y colapsa separadores en una sola pasada.
        /// </summary>
        /// <param name="text">Texto no nulo.</param>
        /// <param name="onlySpaces">True: solo ' ' cuenta como separador. False: cualquier espacio en blanco.</param>
        private static string CollapseWhitespace(string text, bool onlySpaces)
        {
            int start = 0;
            int end = text.Length - 1;

            while (start <= end && char.IsWhiteSpace(text[start])) start++;
            while (end >= start && char.IsWhiteSpace(text[end])) end--;

            if (start > end)
                return string.Empty;

            char[] buffer = new char[end - start + 1];
            int length = 0;
            bool previousWasSeparator = false;

            for (int i = start; i <= end; i++)
            {
                char c = text[i];
                bool isSeparator = onlySpaces ? c == ' ' : char.IsWhiteSpace(c);

                if (isSeparator)
                {
                    if (previousWasSeparator)
                        continue;

                    buffer[length++] = ' ';
                    previousWasSeparator = true;
                }
                else
                {
                    buffer[length++] = c;
                    previousWasSeparator = false;
                }
            }

            return new string(buffer, 0, length);
        }

        #endregion

        #region Dates

        /// <summary>
        /// Convierte una fecha a texto con formato dd/MM/yy (cultura invariante,
        /// para que el separador sea siempre '/').
        /// </summary>
        public static string DateToString(DateTime date)
        {
            return date.ToString(ShortDateFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Convierte un texto con formato dd/MM/yy a fecha.
        /// Los años de dos dígitos se interpretan como 20xx.
        /// Lanza <see cref="FormatException"/> si el formato no es válido.
        /// </summary>
        public static DateTime StringToDate(string text)
        {
            if (!TryStringToDate(text, out DateTime result))
                throw new FormatException($"El string '{text}' no tiene el formato esperado {ShortDateFormat}.");

            return result;
        }

        /// <summary>
        /// Versión sin excepciones de <see cref="StringToDate"/>.
        /// </summary>
        public static bool TryStringToDate(string text, out DateTime result)
        {
            return DateTime.TryParseExact(
                text,
                ShortDateFormat,
                ShortDateCulture,
                DateTimeStyles.None,
                out result);
        }

        /// <summary>
        /// Normaliza un DateTime. Si <paramref name="toUtc"/> es true lo devuelve en UTC;
        /// si su Kind es Unspecified se asume hora local (comportamiento de ToUniversalTime).
        /// </summary>
        public static DateTime GetSafeDateTime(DateTime input, bool toUtc = true)
        {
            return toUtc ? input.ToUniversalTime() : input;
        }

        /// <summary>
        /// Convierte un texto a DateTime probando varios formatos (ver <see cref="AcceptedDateFormats"/>).
        /// Lanza <see cref="FormatException"/> si ningún formato coincide.
        /// </summary>
        public static DateTime GetSafeDateTime(string input, bool toUtc = true)
        {
            if (!TryGetSafeDateTime(input, out DateTime result, toUtc))
                throw new FormatException("Formato de fecha inválido: " + input);

            return result;
        }

        /// <summary>
        /// Versión sin excepciones de <see cref="GetSafeDateTime(string, bool)"/>.
        /// Los textos sin zona horaria se asumen locales. Ante fechas día/mes ambiguas
        /// (ej. 03-04-2024) se prioriza dd-MM y se registra una advertencia.
        /// </summary>
        public static bool TryGetSafeDateTime(string input, out DateTime result, bool toUtc = true)
        {
            if (!DateTime.TryParseExact(
                    input,
                    AcceptedDateFormats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out result))
            {
                return false;
            }

            if (IsAmbiguousDayMonth(input))
                Debug.LogWarning("Fecha potencialmente ambigua: " + input);

            if (toUtc)
                result = result.ToUniversalTime();

            return true;
        }

        /// <summary>
        /// Convierte una fecha UTC a hora local. Si el Kind es Unspecified se asume UTC;
        /// si ya es local se devuelve tal cual.
        /// </summary>
        public static DateTime UtcToLocal(DateTime utcTime)
        {
            if (utcTime.Kind == DateTimeKind.Local)
                return utcTime;

            if (utcTime.Kind == DateTimeKind.Unspecified)
                utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);

            return utcTime.ToLocalTime();
        }

        /// <summary>
        /// Crea una fecha a partir de día, mes y año.
        /// </summary>
        public static DateTime CreateDate(int day, int month, int year)
        {
            return new DateTime(year, month, day);
        }

        /// <summary>
        /// Calcula la edad en años completos entre dos fechas.
        /// Quien nació un 29 de febrero cumple años el 1 de marzo en años no bisiestos.
        /// Devuelve 0 si la fecha actual es anterior al nacimiento.
        /// </summary>
        public static int CalculateAge(DateTime birthDate, DateTime currentDate)
        {
            int age = currentDate.Year - birthDate.Year;

            bool birthdayPending =
                currentDate.Month < birthDate.Month ||
                (currentDate.Month == birthDate.Month && currentDate.Day < birthDate.Day);

            if (birthdayPending)
                age--;

            return Math.Max(age, 0);
        }

        /// <summary>
        /// Detecta si un texto con formato "AA-BB-yyyy" o "AA/BB/yyyy" puede
        /// interpretarse tanto como día/mes como mes/día.
        /// </summary>
        private static bool IsAmbiguousDayMonth(string input)
        {
            int firstSeparator = input.IndexOfAny(DateSeparators);

            // Si el primer bloque tiene más de 2 dígitos es yyyy-MM-dd: no hay ambigüedad.
            if (firstSeparator <= 0 || firstSeparator > 2)
                return false;

            int secondSeparator = input.IndexOfAny(DateSeparators, firstSeparator + 1);
            if (secondSeparator < 0)
                return false;

            if (!int.TryParse(input.Substring(0, firstSeparator), NumberStyles.None, CultureInfo.InvariantCulture, out int first) ||
                !int.TryParse(input.Substring(firstSeparator + 1, secondSeparator - firstSeparator - 1), NumberStyles.None, CultureInfo.InvariantCulture, out int second))
            {
                return false;
            }

            // Si ambos son iguales (ej. 05-05-2024) el resultado es el mismo en cualquier orden.
            return first <= 12 && second <= 12 && first != second;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Valida de forma básica el formato de un email (no comprueba que exista).
        /// </summary>
        public static bool IsValidEmail(this string email)
        {
            return !string.IsNullOrEmpty(email) && EmailRegex.IsMatch(email);
        }

        /// <summary>
        /// Valida los requisitos mínimos de una contraseña (longitud y sin espacios).
        /// </summary>
        /// <param name="password">Contraseña a validar.</param>
        /// <param name="message">Mensaje (para mostrar al usuario) con el motivo del rechazo, o vacío si es válida.</param>
        /// <returns>True si la contraseña cumple los requisitos.</returns>
        public static bool IsValidPassword(this string password, out string message)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
            {
                message = $"La contraseña debe tener al menos {MinPasswordLength} caracteres.";
                return false;
            }

            if (password.IndexOf(' ') >= 0)
            {
                message = "La contraseña no puede contener espacios.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        #endregion

        #region Device

        /// <summary>
        /// Obtiene el ID único del dispositivo.
        /// Atención: en algunas plataformas (ej. WebGL) puede no estar soportado
        /// y devolver "n/a", y en otras puede cambiar al reinstalar o restablecer el dispositivo.
        /// </summary>
        public static string GetDeviceId()
        {
            return SystemInfo.deviceUniqueIdentifier;
        }

        #endregion

        #region GameObject & Component Helpers

        /// <summary>
        /// Desactiva el objeto si existe y está activo.
        /// </summary>
        public static void DeactivateObject(this GameObject obj)
        {
            if (obj != null && obj.activeSelf)
                obj.SetActive(false);
        }

        /// <summary>
        /// Activa el objeto si existe y está inactivo.
        /// </summary>
        public static void ActivateObject(this GameObject obj)
        {
            if (obj != null && !obj.activeSelf)
                obj.SetActive(true);
        }

        /// <summary>
        /// Desactiva un componente (MonoBehaviour, Animator, etc.) si existe.
        /// </summary>
        public static void DisableComponent(this Behaviour component)
        {
            if (component != null)
                component.enabled = false;
        }

        /// <summary>
        /// Activa un componente (MonoBehaviour, Animator, etc.) si existe.
        /// </summary>
        public static void EnableComponent(this Behaviour component)
        {
            if (component != null)
                component.enabled = true;
        }

        /// <summary>
        /// Desactiva un Renderer si existe.
        /// </summary>
        public static void DisableComponent(this Renderer renderer)
        {
            if (renderer != null)
                renderer.enabled = false;
        }

        /// <summary>
        /// Activa un Renderer si existe.
        /// </summary>
        public static void EnableComponent(this Renderer renderer)
        {
            if (renderer != null)
                renderer.enabled = true;
        }

        /// <summary>
        /// Añade un componente al objeto (si no lo tiene ya) y notifica el resultado.
        /// En modo edición la operación se difiere con <c>EditorApplication.delayCall</c>,
        /// evitando errores al añadir componentes durante OnValidate u otros callbacks del editor.
        /// En ejecución se añade inmediatamente.
        /// </summary>
        /// <typeparam name="T">Tipo de componente.</typeparam>
        /// <param name="target">Objeto al que se añade el componente.</param>
        /// <param name="onCreated">Callback con el componente (nuevo o ya existente).</param>
        public static void AddComponentDelayed<T>(GameObject target, Action<T> onCreated = null)
            where T : Component
        {
            if (target == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += () =>
                {
                    // El objeto pudo destruirse mientras se esperaba el delayCall.
                    if (target == null)
                        return;

                    onCreated?.Invoke(GetOrAddComponent<T>(target));
                };

                return;
            }
#endif

            onCreated?.Invoke(GetOrAddComponent<T>(target));
        }

        /// <summary>
        /// Elimina un componente y pone la referencia a null de inmediato.
        /// En modo edición la destrucción se difiere y usa DestroyImmediate;
        /// en ejecución usa Destroy.
        /// </summary>
        /// <typeparam name="T">Tipo de componente.</typeparam>
        /// <param name="component">Referencia al componente (se establece a null).</param>
        public static void RemoveComponentDelayed<T>(ref T component)
            where T : Component
        {
            if (component == null)
                return;

            T componentToRemove = component;
            component = null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += () =>
                {
                    if (componentToRemove != null)
                        Object.DestroyImmediate(componentToRemove);
                };

                return;
            }
#endif

            Object.Destroy(componentToRemove);
        }

        /// <summary>
        /// Devuelve el componente del objeto o lo crea si no existe.
        /// </summary>
        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component ?? target.AddComponent<T>();
        }

        #endregion
    }
}