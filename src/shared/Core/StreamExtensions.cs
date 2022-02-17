using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GitCredentialManager
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Read a dictionary in the form `key1=value\nkey2=value\n\n` from the specified <see cref="TextReader"/>.
        /// </summary>
        /// <remarks>
        /// Uses the <see cref="StringComparer.Ordinal"/> comparer for dictionary keys.
        /// <para/>
        /// Also accepts dictionary lines terminated using \r\n (CR LF) as well as \n (LF).
        /// </remarks>
        /// <param name="reader">Text reader to read a dictionary from.</param>
        /// <returns>Dictionary read from the text reader.</returns>
        public static IDictionary<string, string> ReadDictionary(this TextReader reader) =>
            ReadDictionary(reader, StringComparer.Ordinal);

        /// <summary>
        /// Read a dictionary in the form `key1=value1\nkey1=value2\nkey2=other\n\n` from the specified
        /// <see cref="TextReader"/>.
        /// </summary>
        /// <remarks>
        /// Uses the <see cref="StringComparer.Ordinal"/> comparer for dictionary keys.
        /// <para/>
        /// Also accepts dictionary lines terminated using \r\n (CR LF) as well as \n (LF).
        /// <para/>
        /// Unlike <see cref="ReadDictionary(System.IO.TextReader)"/>, duplicate key values are gathered in a list
        /// in the order in which they appear in the text stream.
        /// </remarks>
        /// <param name="reader">Text reader to read a multi-dictionary from.</param>
        /// <returns>Multi-dictionary read from the text reader.</returns>
        public static IDictionary<string, IList<string>> ReadMultiDictionary(this TextReader reader) =>
            ReadMultiDictionary(reader, StringComparer.Ordinal);

        /// <summary>
        /// Read a dictionary in the form `key1=value\nkey2=value\n\n` from the specified <see cref="TextReader"/>,
        /// with the specified <see cref="StringComparer"/> used to compare dictionary keys.
        /// </summary>
        /// <remarks>Also accepts dictionary lines terminated using \r\n (CR LF) as well as \n (LF).</remarks>
        /// <param name="reader">Text reader to read a dictionary from.</param>
        /// <param name="comparer">Comparer to use when comparing dictionary keys.</param>
        /// <returns>Dictionary read from the text reader.</returns>
        public static IDictionary<string, string> ReadDictionary(this TextReader reader, StringComparer comparer)
        {
            var dict = new Dictionary<string, string>(comparer);

            string line;
            while ((line = reader.ReadLine()) != null && !string.IsNullOrWhiteSpace(line))
            {
                if (TryParseLine(line, out string key, out string value))
                {
                    dict[key] = value;
                }
            }

            return dict;
        }

        /// <summary>
        /// Read a dictionary in the form `key1=value1\nkey1=value2\nkey2=other\n\n` from the specified
        /// <see cref="TextReader"/>, with the specified <see cref="StringComparer"/> used to compare dictionary keys.
        /// </summary>
        /// <remarks>
        /// Also accepts dictionary lines terminated using \r\n (CR LF) as well as \n (LF).
        /// <para/>
        /// Unlike <see cref="ReadDictionary(System.IO.TextReader, System.StringComparer)"/>, duplicate key values are
        /// gathered in a list in the order in which they appear in the text stream.
        /// </remarks>
        /// <param name="reader">Text reader to read a multi-dictionary from.</param>
        /// <param name="comparer">Comparer to use when comparing multi-dictionary keys.</param>
        /// <returns>Multi-dictionary read from the text reader.</returns>
        public static IDictionary<string, IList<string>> ReadMultiDictionary(this TextReader reader, StringComparer comparer)
        {
            var dict = new Dictionary<string, IList<string>>(comparer);

            string line;
            while ((line = reader.ReadLine()) != null && !string.IsNullOrWhiteSpace(line))
            {
                if (TryParseLine(line, out string key, out string value))
                {
                    if (!dict.TryGetValue(key, out IList<string> values))
                    {
                        values = new List<string>();
                        dict[key] = values;
                    }

                    values.Add(value);
                }
            }

            return dict;
        }

        /// <summary>
        /// Asynchronously read a dictionary in the form `key1=value\nkey2=value\n\n` from the specified <see cref="TextReader"/>.
        /// </summary>
        /// <remarks>
        /// Uses the <see cref="StringComparer.Ordinal"/> comparer for dictionary keys.
        /// <para/>
        /// Also accepts dictionary lines terminated using \r\n (CR LF) as well as \n (LF).
        /// </remarks>
        /// <param name="reader">Text reader to read a dictionary from.</param>
        /// <returns>Dictionary read from the text reader.</returns>
        public static Task<IDictionary<string, string>> ReadDictionaryAsync(this TextReader reader) =>
            ReadDictionaryAsync(reader, StringComparer.Ordinal);

        /// <summary>
        /// Asynchronously read a dictionary in the form `key1=value1\nkey1=value2\nkey2=other\n\n` from the
        /// specified <see cref="TextReader"/>.
        /// </summary>
        /// <remarks>
        /// Uses the <see cref="StringComparer.Ordinal"/> comparer for dictionary keys.
        /// <para/>
        /// Also accepts dictionary lines terminated using \r\n (CR LF) as well as \n (LF).
        /// <para/>
        /// Unlike <see cref="ReadDictionary(System.IO.TextReader)"/>, duplicate key values are gathered in a list
        /// in the order in which they appear in the text stream.
        /// </remarks>
        /// <param name="reader">Text reader to read a multi-dictionary from.</param>
        /// <returns>Multi-dictionary read from the text reader.</returns>
        public static Task<IDictionary<string, IList<string>>> ReadMultiDictionaryAsync(this TextReader reader) =>
            ReadMultiDictionaryAsync(reader, StringComparer.Ordinal);

        /// <summary>
        /// Asynchronously read a dictionary in the form `key1=value\nkey2=value\n\n` from the specified <see cref="TextReader"/>,
        /// with the specified <see cref="StringComparer"/> used to compare dictionary keys.
        /// </summary>
        /// <remarks>Also accepts dictionary lines terminated using \r\n (CR LF) as well as \n (LF).</remarks>
        /// <param name="reader">Text reader to read a dictionary from.</param>
        /// <param name="comparer">Comparer to use when comparing dictionary keys.</param>
        /// <returns>Dictionary read from the text reader.</returns>
        public static async Task<IDictionary<string, string>> ReadDictionaryAsync(this TextReader reader, StringComparer comparer)
        {
            var dict = new Dictionary<string, string>(comparer);

            string line;
            while ((line = await reader.ReadLineAsync()) != null && !string.IsNullOrWhiteSpace(line))
            {
                if (TryParseLine(line, out string key, out string value))
                {
                    dict[key] = value;
                }
            }

            return dict;
        }

        /// <summary>
        /// Asynchronously read a dictionary in the form `key1=value1\nkey1=value2\nkey2=other\n\n` from the specified
        /// <see cref="TextReader"/>, with the specified <see cref="StringComparer"/> used to compare dictionary keys.
        /// </summary>
        /// <remarks>
        /// Also accepts dictionary lines terminated using \r\n (CR LF) as well as \n (LF).
        /// <para/>
        /// Unlike <see cref="ReadDictionaryAsync(System.IO.TextReader, System.StringComparer)"/>, duplicate key values are
        /// gathered in a list in the order in which they appear in the text stream.
        /// </remarks>
        /// <param name="reader">Text reader to read a multi-dictionary from.</param>
        /// <param name="comparer">Comparer to use when comparing multi-dictionary keys.</param>
        /// <returns>Multi-dictionary read from the text reader.</returns>
        public static async Task<IDictionary<string, IList<string>>> ReadMultiDictionaryAsync(this TextReader reader, StringComparer comparer)
        {
            var dict = new Dictionary<string, IList<string>>(comparer);

            string line;
            while ((line = await reader.ReadLineAsync()) != null && !string.IsNullOrWhiteSpace(line))
            {
                if (TryParseLine(line, out string key, out string value))
                {
                    if (!dict.TryGetValue(key, out IList<string> values))
                    {
                        values = new List<string>();
                        dict[key] = values;
                    }

                    values.Add(value);
                }
            }

            return dict;
        }

        /// <summary>
        /// Write a dictionary in the form `key1=value\nkey2=value\n\n` to the specified <see cref="TextWriter"/>,
        /// where \n is the configured new-line (see <see cref="TextWriter.NewLine"/>).
        /// </summary>
        /// <remarks>The output dictionary new-lines are determined by the <see cref="TextWriter.NewLine"/> property.</remarks>
        /// <param name="writer">Text writer to write a dictionary to.</param>
        /// <param name="dict">Dictionary to write to the text writer.</param>
        public static void WriteDictionary(this TextWriter writer, IDictionary<string, string> dict)
        {
            foreach (var kvp in dict)
            {
                WriteKeyValuePair(writer, kvp);
            }

            // Write terminating line
            writer.WriteLine();
        }

        /// <summary>
        /// Write a multi-dictionary in the form `key1=value1\nkey1=value1\nkey2=other\n\n` to the specified
        /// <see cref="TextWriter"/>, where \n is the configured new-line (see <see cref="TextWriter.NewLine"/>).
        /// </summary>
        /// <remarks>
        /// The output dictionary new-lines are determined by the <see cref="TextWriter.NewLine"/> property.
        /// <para/>
        /// Multiple entries for the same key are written to the text stream in the same order they appear in
        /// the <see cref="IList{T}"/>.
        /// </remarks>
        /// <param name="writer">Text writer to write a dictionary to.</param>
        /// <param name="dict">Dictionary to write to the text writer.</param>
        public static void WriteMultiDictionary(this TextWriter writer, IDictionary<string, IList<string>> dict)
        {
            foreach (var kvp in dict)
            {
                foreach (string v in kvp.Value)
                {
                    WriteKeyValuePair(writer, kvp.Key, v);
                }
            }

            // Write terminating line
            writer.WriteLine();
        }

        /// <summary>
        /// Asynchronously write a dictionary in the form `key1=value\nkey2=value\n\n` to the specified <see cref="TextWriter"/>,
        /// where \n is the configured new-line (see <see cref="TextWriter.NewLine"/>).
        /// </summary>
        /// <remarks>The output dictionary new-lines are determined by the <see cref="TextWriter.NewLine"/> property.</remarks>
        /// <param name="writer">Text writer to write a dictionary to.</param>
        /// <param name="dict">Dictionary to write to the text writer.</param>
        public static async Task WriteDictionaryAsync(this TextWriter writer, IDictionary<string, string> dict)
        {
            foreach (var kvp in dict)
            {
                await WriteKeyValuePairAsync(writer, kvp);
            }

            // Write terminating line
            await writer.WriteLineAsync();
        }

        /// <summary>
        /// Asynchronously write a multi-dictionary in the form `key1=value\nkey2=value\n\n` to the specified
        /// <see cref="TextWriter"/>, where \n is the configured new-line (see <see cref="TextWriter.NewLine"/>).
        /// </summary>
        /// <remarks>
        /// The output dictionary new-lines are determined by the <see cref="TextWriter.NewLine"/> property.
        /// <para/>
        /// Multiple entries for the same key are written to the text stream in the same order they appear in
        /// the <see cref="IList{T}"/>.
        /// </remarks>
        /// <param name="writer">Text writer to write a dictionary to.</param>
        /// <param name="dict">Dictionary to write to the text writer.</param>
        public static async Task WriteMultiDictionaryAsync(this TextWriter writer, IDictionary<string, IList<string>> dict)
        {
            foreach (var kvp in dict)
            {
                foreach (string v in kvp.Value)
                {
                    await WriteKeyValuePairAsync(writer, kvp.Key, v);
                }
            }

            // Write terminating line
            await writer.WriteLineAsync();
        }

        private static void WriteKeyValuePair(this TextWriter writer, KeyValuePair<string, string> kvp)
            => WriteKeyValuePair(writer, kvp.Key, kvp.Value);

        private static void WriteKeyValuePair(this TextWriter writer, string key, string value)
        {
            writer.WriteLine($"{key}={value}");
        }

        private static Task WriteKeyValuePairAsync(this TextWriter writer, KeyValuePair<string, string> kvp)
            => WriteKeyValuePairAsync(writer, kvp.Key, kvp.Value);

        private static Task WriteKeyValuePairAsync(this TextWriter writer, string key, string value)
        {
            return writer.WriteLineAsync($"{key}={value}");
        }

        private static bool TryParseLine(string line, out string key, out string value)
        {
            int splitIndex = line.IndexOf('=');
            if (splitIndex > 0)
            {
                key = line.Substring(0, splitIndex);
                value = line.Substring(splitIndex + 1);

                return true;
            }

            key = null;
            value = null;
            return false;
        }
    }
}
