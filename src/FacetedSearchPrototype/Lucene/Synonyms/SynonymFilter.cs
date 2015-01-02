using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;

//namespace FacetedSearchPrototype.Lucene.Synonyms
//{
    public class SynonymFilter : TokenFilter
    {
        public const string TOKEN_TYPE_SYNONYM = "SYNONYM";

        private Stack<string> synonymStack;
        private ISynonymEngine engine;
        private AttributeSource.State current;

        private readonly TermAttribute termAtt;
        private readonly PositionIncrementAttribute posIncrAtt;

        public SynonymFilter(TokenStream in_Renamed, ISynonymEngine engine)
            : base(in_Renamed)
        {
            synonymStack = new Stack<string>();
            this.engine = engine;

            //termAtt = (TermAttribute)AddAttribute(typeof(TermAttribute));
            //posIncrAtt = (PositionIncrementAttribute)AddAttribute(typeof(PositionIncrementAttribute));

            this.termAtt = new TermAttribute();
            AddAttributeImpl(this.termAtt);

            this.posIncrAtt = new PositionIncrementAttribute();
            AddAttributeImpl(this.posIncrAtt);
        }

        public override Boolean IncrementToken()
        {
            if (synonymStack.Count > 0)
            {
                string syn = synonymStack.Pop();
                RestoreState(current);
                termAtt.SetTermBuffer(syn);

                // This ensures the new word is treated as a synonym
                posIncrAtt.PositionIncrement = 0;
                return true;
            }

            if (!input.IncrementToken())
                return false;

            // Push synonyms to stack
            if (AddAliasesToStack())
            {
                // Save current token
                current = CaptureState();
            }

            return true;
        }

        private Boolean AddAliasesToStack()
        {
            string[] synonyms = engine.GetSynonyms(termAtt.Term);

            if (synonyms == null)
            {
                return false;
            }
            foreach (string synonym in synonyms)
            {
                synonymStack.Push(synonym);
            }
            return true;
        }
    }
//}