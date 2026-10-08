using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
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

// DOTween y TextMeshPro solo se usan en DOCounter. El símbolo DOTWEEN lo define
// el setup de DOTween; así el archivo compila también en proyectos que no lo tengan.
#if DOTWEEN
using DG.Tweening;
using TMPro;
#endif

namespace EMT.Core
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

        /// <summary>
        /// Devuelve un elemento aleatorio de la lista.
        /// Lanza <see cref="InvalidOperationException"/> si la lista está vacía.
        /// </summary>
        public static T GetRandomElement<T>(this IList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Count == 0)
                throw new InvalidOperationException("No se puede elegir un elemento de una lista vacía.");

            return list[Random.Range(0, list.Count)]; // max exclusivo en int
        }

        /// <summary>
        /// Devuelve un elemento aleatorio distinto de <paramref name="excluded"/>.
        /// Todos los candidatos tienen la misma probabilidad y nunca entra en un bucle:
        /// si no hay ninguna alternativa (lista de un elemento o todos iguales al excluido)
        /// devuelve un elemento cualquiera. Devuelve default si la lista es nula o está vacía.
        /// </summary>
        /// <param name="list">Lista de la que se elige.</param>
        /// <param name="excluded">Elemento que se quiere evitar.</param>
        public static T GetRandomExcluding<T>(this IList<T> list, T excluded)
        {
            if (list == null || list.Count == 0)
                return default;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            // Primera pasada: contar los candidatos válidos (sin asignar memoria).
            int validCount = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (!comparer.Equals(list[i], excluded))
                    validCount++;
            }

            // Sin alternativa: no queda más remedio que repetir.
            if (validCount == 0)
                return list[Random.Range(0, list.Count)];

            // Segunda pasada: avanzar hasta el candidato válido número k.
            int target = Random.Range(0, validCount);
            for (int i = 0; i < list.Count; i++)
            {
                if (comparer.Equals(list[i], excluded))
                    continue;

                if (target == 0)
                    return list[i];

                target--;
            }

            // Inalcanzable: validCount garantiza que se encontró un candidato.
            return default;
        }

        /// <summary>
        /// Indica si la secuencia es nula o no tiene elementos.
        /// Para colecciones usa Count; para el resto solo avanza un elemento (no la recorre completa).
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null)
                return true;

            if (source is ICollection<T> collection)
                return collection.Count == 0;

            using (IEnumerator<T> enumerator = source.GetEnumerator())
                return !enumerator.MoveNext();
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

        private const string HexDigits = "0123456789abcdef";

        /// <summary>
        /// Calcula el hash SHA-256 de un texto (UTF-8) y lo devuelve en hexadecimal minúscula (64 caracteres).
        /// Devuelve vacío si el texto es nulo o vacío.
        /// </summary>
        public static string ComputeSha256(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                char[] chars = new char[hash.Length * 2];

                for (int i = 0; i < hash.Length; i++)
                {
                    chars[i * 2] = HexDigits[hash[i] >> 4];
                    chars[i * 2 + 1] = HexDigits[hash[i] & 0x0F];
                }

                return new string(chars);
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

            return ReplaceAccents(text, graveToAcute: false);
        }

        /// <summary>
        /// Convierte las vocales con tilde grave en su versión con tilde aguda
        /// (à → á, è → é, ì → í, ò → ó, ù → ú, y sus mayúsculas).
        /// Útil para corregir textos mal escritos con tilde invertida.
        /// Si no hay nada que corregir se devuelve la misma instancia.
        /// </summary>
        public static string FixGraveAccents(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return ReplaceAccents(text, graveToAcute: true);
        }

        /// <summary>
        /// Convierte el texto a "Formato Título": primera letra de cada palabra en mayúscula
        /// y el resto en minúscula (cultura invariante).
        /// </summary>
        public static string ToTitleCase(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // ToTitleCase no modifica palabras que ya están completamente en mayúsculas,
            // por eso se pasa primero a minúscula.
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
        }

        /// <summary>
        /// Convierte un texto en un "slug" apto para URLs o IDs: minúsculas, sin tildes ni ñ,
        /// solo letras a-z y números, con los demás caracteres colapsados en un único separador.
        /// Ejemplo: "  ¡Niño Ágil, 2024!  " → "nino-agil-2024".
        /// </summary>
        /// <param name="text">Texto a convertir.</param>
        /// <param name="separator">Carácter separador (por defecto '-').</param>
        public static string Slugify(this string text, char separator = '-')
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Nunca se necesita más espacio que la longitud original:
            // cada separador insertado corresponde a al menos un carácter descartado.
            char[] buffer = new char[text.Length];
            int length = 0;
            bool separatorPending = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = StripAccent(text[i]);

                if (c == 'ñ' || c == 'Ñ')
                    c = 'n';

                c = char.ToLowerInvariant(c);

                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    if (separatorPending && length > 0)
                        buffer[length++] = separator;

                    separatorPending = false;
                    buffer[length++] = c;
                }
                else
                {
                    separatorPending = true;
                }
            }

            return new string(buffer, 0, length);
        }

        /// <summary>
        /// Oculta la parte local de un email dejando solo el primer carácter.
        /// Ejemplo: "juan@mail.com" → "j***@mail.com".
        /// Si el texto no tiene un '@' válido se devuelve sin modificar.
        /// </summary>
        /// <param name="email">Email a ocultar.</param>
        /// <param name="maskChar">Carácter de enmascarado.</param>
        public static string MaskEmail(this string email, char maskChar = '*')
        {
            if (string.IsNullOrEmpty(email))
                return email;

            int atIndex = email.IndexOf('@');
            if (atIndex <= 0)
                return email;

            return email[0] + new string(maskChar, 3) + email.Substring(atIndex);
        }

        /// <summary>
        /// Núcleo común de RemoveAccents y FixGraveAccents: reemplaza carácter a carácter
        /// en dos pasadas, haciendo una única copia del texto solo si hay algo que cambiar.
        /// </summary>
        /// <param name="text">Texto no nulo.</param>
        /// <param name="graveToAcute">True: grave → aguda. False: quitar la tilde.</param>
        private static string ReplaceAccents(string text, bool graveToAcute)
        {
            // Primera pasada: buscar el primer carácter a reemplazar (sin asignar memoria).
            int firstIndex = -1;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if ((graveToAcute ? GraveToAcute(c) : StripAccent(c)) != c)
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
                chars[i] = graveToAcute ? GraveToAcute(chars[i]) : StripAccent(chars[i]);

            return new string(chars);
        }

        /// <summary>
        /// Devuelve la vocal con tilde aguda si recibe una con tilde grave (o el mismo carácter si no aplica).
        /// </summary>
        private static char GraveToAcute(char c)
        {
            switch (c)
            {
                case 'à': return 'á';
                case 'è': return 'é';
                case 'ì': return 'í';
                case 'ò': return 'ó';
                case 'ù': return 'ú';
                case 'À': return 'Á';
                case 'È': return 'É';
                case 'Ì': return 'Í';
                case 'Ò': return 'Ó';
                case 'Ù': return 'Ú';
                default: return c;
            }
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

        #region Weekdays & Date Math

        /// <summary>Nombres de los días en español, en el mismo orden que <see cref="Weekday"/> (lunes = 0).</summary>
        private static readonly string[] WeekdayNames =
        {
            "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"
        };

        /// <summary>
        /// Devuelve la fecha de hoy sin la hora.
        /// </summary>
        public static DateTime GetToday()
        {
            return DateTime.Today;
        }

        /// <summary>
        /// Suma (o resta, si es negativo) días a una fecha.
        /// </summary>
        public static DateTime AddDays(DateTime date, int days)
        {
            return date.AddDays(days);
        }

        /// <summary>
        /// Devuelve el día de la semana en español junto con la fecha (dd/MM/yyyy, cultura invariante).
        /// Ejemplo: "Miércoles 07/10/2026".
        /// </summary>
        public static string FormatWeekdayWithDate(DateTime date)
        {
            string dayName = WeekdayNames[(int)ToWeekday(date.DayOfWeek)];
            return dayName + " " + date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Convierte un <see cref="DayOfWeek"/> de .NET (domingo = 0) a <see cref="Weekday"/> (lunes = 0).
        /// Devuelve <see cref="Weekday.Invalid"/> si el valor está fuera de rango.
        /// </summary>
        public static Weekday ToWeekday(DayOfWeek day)
        {
            int value = (int)day;

            if (value < 0 || value > 6)
                return Weekday.Invalid;

            // Domingo (0) pasa a 6; el resto se desplaza una posición.
            return (Weekday)((value + 6) % 7);
        }

        /// <summary>
        /// Convierte un <see cref="Weekday"/> (lunes = 0) a <see cref="DayOfWeek"/> de .NET (domingo = 0).
        /// Lanza <see cref="ArgumentOutOfRangeException"/> si el valor es <see cref="Weekday.Invalid"/>.
        /// </summary>
        public static DayOfWeek ToDayOfWeek(Weekday day)
        {
            int value = (int)day;

            if (value < (int)Weekday.Monday || value > (int)Weekday.Sunday)
                throw new ArgumentOutOfRangeException(nameof(day), day, "El día de la semana no es válido.");

            return (DayOfWeek)((value + 1) % 7);
        }

        /// <summary>
        /// Convierte un índice (0 = lunes ... 6 = domingo) a <see cref="DayOfWeek"/>.
        /// Lanza <see cref="ArgumentOutOfRangeException"/> si el índice no está entre 0 y 6.
        /// </summary>
        public static DayOfWeek DayOfWeekFromIndex(int index)
        {
            if (index < 0 || index > 6)
                throw new ArgumentOutOfRangeException(nameof(index), index, "El índice debe estar entre 0 (lunes) y 6 (domingo).");

            return (DayOfWeek)((index + 1) % 7);
        }

        /// <summary>
        /// Calcula la fecha del próximo día de la semana indicado a partir de una fecha base.
        /// Siempre es posterior a la fecha base: si ese día ya coincide, devuelve el de la semana siguiente (+7 días).
        /// </summary>
        /// <param name="baseDate">Fecha de partida.</param>
        /// <param name="desiredDay">Día de la semana buscado.</param>
        public static DateTime GetNextWeekday(DateTime baseDate, DayOfWeek desiredDay)
        {
            int daysUntilNext = ((int)desiredDay - (int)baseDate.DayOfWeek + 7) % 7;

            if (daysUntilNext == 0)
                daysUntilNext = 7;

            return baseDate.AddDays(daysUntilNext);
        }

        #endregion

        #region Numbers & Time Formatting

        private static readonly string[] CompactSuffixes = { "", "K", "M", "B", "T" };

        /// <summary>
        /// Convierte segundos a texto "mm:ss", o "hh:mm:ss" si hay horas
        /// (o si <paramref name="forceHours"/> es true). Los valores negativos o inválidos se tratan como 0.
        /// </summary>
        /// <param name="seconds">Tiempo en segundos.</param>
        /// <param name="forceHours">Mostrar siempre las horas.</param>
        public static string FormatTime(float seconds, bool forceHours = false)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                seconds = 0f;

            // Límite para evitar desbordamiento al convertir a long.
            long total = (long)Math.Min(seconds, 3.6e9f);
            long hours = total / 3600;
            long minutes = (total % 3600) / 60;
            long secs = total % 60;

            if (hours > 0 || forceHours)
                return $"{hours:00}:{minutes:00}:{secs:00}";

            return $"{minutes:00}:{secs:00}";
        }

        /// <summary>
        /// Abrevia un número grande: 1500 → "1.5K", 2300000 → "2.3M".
        /// Trunca (no redondea), por lo que 1999 → "1.9K" y nunca se obtiene "1000K".
        /// Usa cultura invariante (punto decimal).
        /// </summary>
        /// <param name="value">Número a abreviar.</param>
        /// <param name="decimals">Decimales máximos (0 a 3).</param>
        public static string FormatCompactNumber(long value, int decimals = 1)
        {
            decimals = Mathf.Clamp(decimals, 0, 3);

            if (value > -1000 && value < 1000)
                return value.ToString(CultureInfo.InvariantCulture);

            // decimal evita errores de coma flotante al truncar (ej. 4.35 * 100 = 434.999...).
            decimal absolute = Math.Abs((decimal)value);
            int suffixIndex = 0;

            while (absolute >= 1000m && suffixIndex < CompactSuffixes.Length - 1)
            {
                absolute /= 1000m;
                suffixIndex++;
            }

            decimal factor = 1m;
            for (int i = 0; i < decimals; i++)
                factor *= 10m;

            absolute = Math.Floor(absolute * factor) / factor;

            string format = decimals == 0 ? "0" : "0." + new string('#', decimals);
            string sign = value < 0 ? "-" : string.Empty;

            return sign + absolute.ToString(format, CultureInfo.InvariantCulture) + CompactSuffixes[suffixIndex];
        }

        /// <summary>
        /// Reescala un valor de un rango a otro. Ejemplo: 5.Remap(0, 10, 0, 100) = 50.
        /// </summary>
        /// <param name="value">Valor a reescalar.</param>
        /// <param name="fromMin">Mínimo del rango de origen.</param>
        /// <param name="fromMax">Máximo del rango de origen.</param>
        /// <param name="toMin">Mínimo del rango de destino.</param>
        /// <param name="toMax">Máximo del rango de destino.</param>
        /// <param name="clamp">Si es true, el resultado no sale del rango de destino.</param>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax, bool clamp = false)
        {
            // Rango de origen de tamaño cero: evita la división por cero.
            if (Mathf.Approximately(fromMin, fromMax))
                return toMin;

            float t = (value - fromMin) / (fromMax - fromMin);

            if (clamp)
                t = Mathf.Clamp01(t);

            return Mathf.LerpUnclamped(toMin, toMax, t);
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
        /// Convierte un Transform en RectTransform (para objetos de UI).
        /// Lanza <see cref="InvalidCastException"/> si el objeto no tiene RectTransform,
        /// lo que delata el error de inmediato en lugar de fallar más adelante.
        /// </summary>
        public static RectTransform ToRectTransform(this Transform transform)
        {
            return (RectTransform)transform;
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
        /// Devuelve null si el objeto es nulo o fue destruido.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject target)
            where T : Component
        {
            if (target == null)
                return null;

            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        /// <summary>
        /// Destruye todos los hijos directos de un Transform.
        /// En ejecución usa Destroy (la destrucción real ocurre al final del frame);
        /// en modo edición usa DestroyImmediate.
        /// </summary>
        public static void DestroyAllChildren(this Transform parent)
        {
            if (parent == null)
                return;

            // Se recorre al revés por seguridad ante destrucciones inmediatas.
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;

                if (Application.isPlaying)
                    Object.Destroy(child);
                else
                    Object.DestroyImmediate(child);
            }
        }

        /// <summary>
        /// Asigna una capa al objeto y a todos sus descendientes (activos o no).
        /// </summary>
        /// <param name="obj">Objeto raíz.</param>
        /// <param name="layer">Índice de capa (0 a 31).</param>
        public static void SetLayerRecursively(this GameObject obj, int layer)
        {
            if (obj == null)
                return;

            obj.layer = layer;

            // Recursión sobre Transform: evita el array que crearía GetComponentsInChildren.
            Transform transform = obj.transform;
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetLayerRecursively(layer);
        }

        #endregion

        #region Vector Helpers

        /// <summary>Devuelve una copia del vector con el eje X reemplazado.</summary>
        public static Vector3 WithX(this Vector3 vector, float x)
        {
            return new Vector3(x, vector.y, vector.z);
        }

        /// <summary>Devuelve una copia del vector con el eje Y reemplazado.</summary>
        public static Vector3 WithY(this Vector3 vector, float y)
        {
            return new Vector3(vector.x, y, vector.z);
        }

        /// <summary>Devuelve una copia del vector con el eje Z reemplazado.</summary>
        public static Vector3 WithZ(this Vector3 vector, float z)
        {
            return new Vector3(vector.x, vector.y, z);
        }

        /// <summary>
        /// Devuelve un punto aleatorio dentro de la caja definida por dos vectores
        /// (cada eje se sortea de forma independiente entre min y max).
        /// </summary>
        public static Vector3 RandomRange(Vector3 min, Vector3 max)
        {
            return new Vector3(
                Random.Range(min.x, max.x),
                Random.Range(min.y, max.y),
                Random.Range(min.z, max.z));
        }

        #endregion

        #region Color Helpers

        /// <summary>
        /// Convierte un color a texto hexadecimal: "#RRGGBB", o "#RRGGBBAA" si se incluye el alfa.
        /// </summary>
        public static string ToHex(this Color color, bool includeAlpha = false)
        {
            return "#" + (includeAlpha
                ? ColorUtility.ToHtmlStringRGBA(color)
                : ColorUtility.ToHtmlStringRGB(color));
        }

        /// <summary>
        /// Convierte un texto hexadecimal ("#RGB", "#RGBA", "#RRGGBB" o "#RRGGBBAA", con o sin '#') a color.
        /// Lanza <see cref="FormatException"/> si el texto no es válido; usar
        /// <see cref="TryHexToColor"/> para evitar la excepción.
        /// </summary>
        public static Color HexToColor(string hex)
        {
            if (!TryHexToColor(hex, out Color color))
                throw new FormatException($"'{hex}' no es un color hexadecimal válido.");

            return color;
        }

        /// <summary>
        /// Versión sin excepciones de <see cref="HexToColor"/>. Si falla, el color de salida no es significativo.
        /// </summary>
        public static bool TryHexToColor(string hex, out Color color)
        {
            color = Color.white;

            if (string.IsNullOrWhiteSpace(hex))
                return false;

            hex = hex.Trim();

            // ColorUtility exige el '#' al inicio.
            if (hex[0] != '#')
                hex = "#" + hex;

            return ColorUtility.TryParseHtmlString(hex, out color);
        }

        #endregion
    }

    /// <summary>
    /// Dirección horizontal.
    /// </summary>
    public enum Direction
    {
        Left,
        Right
    }

    /// <summary>
    /// Día de la semana empezando en lunes (Monday = 0 ... Sunday = 6).
    /// A diferencia de <see cref="DayOfWeek"/>, donde domingo = 0.
    /// </summary>
    public enum Weekday
    {
        Monday = 0,
        Tuesday = 1,
        Wednesday = 2,
        Thursday = 3,
        Friday = 4,
        Saturday = 5,
        Sunday = 6,
        Invalid = 7
    }
}
