    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Util;

//namespace FacetedSearchPrototype.Lucene.Synonyms
//{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;


    public class SynonymAnalyzer : Analyzer
    {
        private ISynonymEngine engine;
        private Lucene.Net.Util.Version version;

        public SynonymAnalyzer(Lucene.Net.Util.Version version, ISynonymEngine engine)
        {
            this.version = version;
            this.engine = engine;
        }

        public override TokenStream TokenStream(string fieldName, System.IO.TextReader reader)
        {
            TokenStream result = new SynonymFilter(
                                    new StopFilter(true,
                                        new LowerCaseFilter(
                                            new StandardFilter(
                                               new StandardTokenizer(this.version, reader))),
                                               StopAnalyzer.ENGLISH_STOP_WORDS_SET),
                                               engine
                                               );

            return result;
        }
    }
//}