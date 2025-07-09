
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

namespace eSearch.Models.Search.Synonyms
{
    /*
        Example of what UTP Synonyms file looks like:-
		The 'name' field is ignored and is purely for UI, does not affect search in any way.
		The 'synonyms' field shows list of synonyms.

		<?xml version="1.0" encoding="UTF-8" ?>
		<dtSearchUserThesaurus>
			<Item>
				<Name>Personal computer</Name>
				<Synonyms>"Personal computer" PC laptop</Synonyms>
			</Item>
			<Item>
				<Name>how much</Name>
				<Synonyms>"how much is" "what's the price of" "what's the cost of" "how much does"</Synonyms>
			</Item>
			<Item>
				<Name>Guilder</Name>
				<Synonyms>ƒ Guilder Lira</Synonyms>
			</Item>
			<Item>
				<Name>sing expand</Name>
				<Synonyms>sing sang sung</Synonyms>
			</Item>
		</dtSearchUserThesaurus>
	*/


	/// <summary>
	/// Represents the contents of a single User Thesaurus XML File.
	/// </summary>
    public class UTP_Thesaurus : IThesaurus
    {

        private SynonymGroup[] SynonymGroups = null;
		private string FileName;

		/// <summary>
		/// Load a Thesaurus xml file created by UTP
		/// 
		/// No exception handling here, may lead to XML parse errors, file io errors etc.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
        public static UTP_Thesaurus LoadThesaurus(string fileName)
        {
			List<SynonymGroup> synonymGroups = new List<SynonymGroup>();
            var doc = new XmlDocument();
            doc.Load(fileName);
			
            var items = doc.DocumentElement.SelectNodes("Item");

            foreach (var item in items)
            {
				if (item is XmlNode itemNode)
				{
					var name = itemNode["Name"].InnerText;
					var synonymStr = itemNode["Synonyms"].InnerText;
					if (synonymStr.Contains("keep")) Debug.WriteLine("SynonymStr raw " + synonymStr);
					string[] synonyms = toSynonymsArray(synonymStr);
					if (synonymStr.Contains("keep")) Debug.WriteLine("CSV: #" + string.Join("#,#", synonyms) + "#");
                    synonymGroups.Add(new SynonymGroup { Name = name, Synonyms = synonyms });
				}
            }
			return new UTP_Thesaurus { SynonymGroups = synonymGroups.ToArray(), FileName = fileName };
        }

		public void SaveThesaurus(SynonymGroup[] newGroups)
		{
			var xmlDocument		= new XmlDocument();
			var xmlDeclaration	= xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
			var xmlRoot			= xmlDocument.DocumentElement;
			xmlDocument.InsertBefore(xmlDeclaration, xmlRoot);
			var elementDtSearchUserThesaurus = xmlDocument.CreateElement("eSearchUserThesaurus");
			xmlDocument.AppendChild(elementDtSearchUserThesaurus);
			foreach(var synonymGroup in newGroups)
			{
				var elementItem		= xmlDocument.CreateElement("Item");
				var elementName		= xmlDocument.CreateElement("Name");
				var elementSynonyms = xmlDocument.CreateElement("Synonyms");
				elementName.InnerText = synonymGroup.Name;
				string synonymsStr = fromSynonymsArray(synonymGroup.Synonyms);
				elementSynonyms.InnerText = synonymsStr;

				elementItem.AppendChild(elementName);
				elementItem.AppendChild(elementSynonyms);
				elementDtSearchUserThesaurus.AppendChild(elementItem);
			}
			xmlDocument.Save(FileName);
		}

        public string[] GetSynonyms(string word)
        {
			HashSet<string> synonyms = new HashSet<string>();
			synonyms.Add(word.ToLower()); // Ensure the word itself always comes back.
            int i = SynonymGroups.Length;
			int s;
			while (i --> 0)
			{
				if (SynonymGroups[i].Synonyms.Contains(word.ToLower()))
				{
					s = SynonymGroups[i].Synonyms.Length;
					while (s --> 0)
					{
						synonyms.Add(SynonymGroups[i].Synonyms[s]);
					}
				}
			}
			return synonyms.ToArray();
        }

		private string fromSynonymsArray(string[] synonyms)
		{
			StringBuilder sb = new StringBuilder();
			int i = 0;
			int len = synonyms.Length;
			while (i < len)
			{
				string synonym = synonyms[i].Trim();
				if (synonym.Contains(" "))
				{
					sb.Append("\"");
					sb.Append(synonym);
					sb.Append("\"");
				} else
				{
					sb.Append(synonym);
				}
				sb.Append(" ");
				++i;
			}
			return sb.ToString().Trim();
		}

        private static string[] toSynonymsArray(string synonymStrRaw)
		{
			// Incoming string like
			// "Personal computer" PC laptop
			// Quoted ones are phrases, otherwise treat as single words
			List<string> synonyms = new List<string>();

			StringBuilder sb = new StringBuilder();
			int i = 0;
			int len = synonymStrRaw.Length;

			bool phrase = false;

			while (i < len)
			{
				char c = synonymStrRaw[i];

				if (c == '"') { phrase = !phrase; }
				else
				{
					if (c == ' ' && !phrase)
					{
						string word = sb.ToString();
						if (word != "") synonyms.Add(word.ToLower());
						sb.Clear();
					} else
					{
						sb.Append(c);
					}
				}
				++i;
			}
			string lastWord = sb.ToString();
			if (lastWord != "") synonyms.Add(lastWord.ToLower());
			sb.Clear();
			return synonyms.ToArray();
        }

        public SynonymGroup[] GetSynonymGroups()
        {
			return SynonymGroups;
        }
    }
}
