using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

//namespace FacetedSearchPrototype.Lucene.Synonyms
//{
    public class SpecialSynonymEngine : ISynonymEngine
    {
        private static Dictionary<string, List<string>> map = new Dictionary<string, List<string>>();

        static SpecialSynonymEngine()
        {
            map.Add("stylecraft", new List<string>() { "style craft" });
            map.Add("style", new List<string>() { "stylecraft" });
        }

        #region ISynonymEngine Members

        public string[] GetSynonyms(string s)
        {
            if (map.Keys.Contains(s))
                return map[s].ToArray();
            else
                return null;
        }

        #endregion
    }
//}