#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Yamara
{
    public class ScriptBuilder
    {
        private const string SctiptExtension = ".cs";
        private const string IndentText = "    ";

        public StringBuilder stringBuilder { get; set; } = new();
        public int indentCount { get; set; } = 0;
        private bool indented = false;

        public ScriptBuilder PlusIndent()
        {
            indentCount++;
            return this;
        }
        public ScriptBuilder MinusIndent()
        {
            indentCount--;
            return this;
        }
        public ScriptBuilder TryIndent()
        {
            if (!indented) Indent();
            return this;
        }
        public ScriptBuilder Indent()
        {
            indented = true;
            for (int i = indentCount; i > 0; i--) stringBuilder.Append(IndentText);
            return this;
        }
        public ScriptBuilder SingleIndent()
        {
            stringBuilder.Append(IndentText);
            return this;
        }

        public ScriptBuilder Append(object str)
        {
            TryIndent().stringBuilder.Append(str.ToString());
            return this;
        }
        public ScriptBuilder Append(object str, bool removeInvalidChars)
            => Append(removeInvalidChars ? RemoveInvalidChars(str.ToString()) : str);
        public ScriptBuilder Append(object str, bool removeInvalidChars, int padRight)
            => Append(removeInvalidChars
                ? RemoveInvalidChars(str.ToString()).PadRight(padRight, ' ')
                : str.ToString().PadRight(padRight, ' '));

        public ScriptBuilder AppendLine(object str) => Append(str).NewLine();
        public ScriptBuilder AppendLine(object str, bool removeInvalidChars)
            => Append(str, removeInvalidChars).NewLine();
        public ScriptBuilder AppendLine(object str, bool removeInvalidChars, int padRight)
            => Append(str, removeInvalidChars, padRight).NewLine();

        public ScriptBuilder Remove(int startIndex, int length)
        {
            stringBuilder.Remove(startIndex, length);
            return this;
        }
        public ScriptBuilder InvRemove(int lastIndex, int length)
        {
            var startIndex = stringBuilder.Length - lastIndex - length;
            stringBuilder.Remove(startIndex, length);
            return this;
        }
        public ScriptBuilder Pop(int length) => InvRemove(0, length);
        public ScriptBuilder PopIndentLength() => Pop(IndentText.Length * indentCount);
        public ScriptBuilder PopSingleIndentLength() => Pop(IndentText.Length);

        private static readonly string[] InvalidChars =
        {
            " ", "!", "\"", "#", "$",
            "%", "&", "\'", "(", ")",
            "-", "=", "^",  "~", "\\",
            "|", "[", "{",  "@", "`",
            "]", "}", ":",  "*", ";",
            "+", "/", "?",  ".", ">",
            ",", "<"
        };
        private string RemoveInvalidChars(string str)
        {
            Array.ForEach(InvalidChars, c => str = str.Replace(c, string.Empty));
            return str;
        }

        public ScriptBuilder OpenBrace() => TryIndent().AppendLine("{").PlusIndent();
        public ScriptBuilder CloseBrace() => MinusIndent().TryIndent().AppendLine("}");
        public ScriptBuilder NewLine()
        {
            stringBuilder.AppendLine(string.Empty);
            indented = false;
            return this;
        }

        public ScriptBuilder UsingDirectives(params string[] nameSpaces)
        {
            foreach (var nameSpace in nameSpaces) AppendLine(string.Format($"using {nameSpace};"));
            return this;
        }
        public ScriptBuilder Namespace(string nameSpace) => AppendLine("namespace " + nameSpace);

        public ScriptBuilder Comment(string comment) => AppendLine("// " + comment);
        public ScriptBuilder Summary(string summary)
        {
            AppendLine("/// <summary>");
            foreach (var line in summary.Replace("\r\n", "\n").Split(new[] { '\n', '\r' })) AppendLine("/// " + line);
            AppendLine("/// </summary>");
            return this;
        }

        public ScriptBuilder Clear()
        {
            stringBuilder.Clear();
            indentCount = 0;
            indented = false;
            return this;
        }

        public ScriptBuilder WriteToFile(string directoryPath, string fileName)
        {
            var exportPath = Path.Combine(directoryPath, fileName + SctiptExtension);
            var directoryName = Path.GetDirectoryName(exportPath);
            if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);

            var exportString = stringBuilder.ToString();

            if (File.Exists(exportPath))
            {
                var sr = new StreamReader(exportPath, Encoding.UTF8);
                var isSame = sr.ReadToEnd() == exportString;
                sr.Close();
                if (isSame) return this;
            }
            File.WriteAllText(exportPath, exportString, Encoding.UTF8);
            AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
            Debug.Log("Wrote script : " + exportPath);
            return this;
        }
    }
}
#endif
