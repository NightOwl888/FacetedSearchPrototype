using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Configuration;
using System.Xml;
using System.Xml.XPath;
using Lucene.Net;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Search;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;


using BoboBrowse.Api;
using BoboBrowse.Facets.impl;
using BoboBrowse.Facets;


// References
// http://code.google.com/p/bobo-browse/wiki/GettingStarted
// http://www.devatwork.nl/articles/lucenenet/faceted-search-and-drill-down-lucenenet/
// http://simplelucene.codeplex.com/
// https://github.com/apache/lucene.net
// https://cwiki.apache.org/LUCENENET/


namespace FacetedSearchPrototype.Controllers
{
    public class SearchController : Controller
    {
        private string IndexDirectory { get; set; }
        private string AutoCompleteIndexDirectory { get; set; }
        private Cipher.Services.SearchAutoComplete SearchSvc { get; set; }

        public SearchController()
        {
            IndexDirectory = this.MapPathSafe(ConfigurationManager.AppSettings["IndexDirectory"]);
            AutoCompleteIndexDirectory = this.MapPathSafe(ConfigurationManager.AppSettings["AutoCompleteIndexDirectory"]);

            // create the index reader
            SearchSvc = new Cipher.Services.SearchAutoComplete(AutoCompleteIndexDirectory);
        }

        private string MapPathSafe(string indexDirectory)
        {
            if (!string.IsNullOrEmpty(indexDirectory) && indexDirectory.StartsWith("~"))
            {
                return HostingEnvironment.MapPath(indexDirectory);
            }
            return indexDirectory;
        }


        //
        // GET: /Search/exterior-shutters

        public ActionResult Index()
        {
            return View(new Models.Search());
        }

        [HttpPost]
        public ActionResult Index(Models.Search model)
        {

            if (!string.IsNullOrEmpty(model.Phrase))
            {

                //// Use this style query to search for documents - it returns all results
                //// including those matching only some of the terms
                //Query query = new QueryParser(
                //        Lucene.Net.Util.Version.LUCENE_29,
                //        "title",
                //        new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29)
                //    ).Parse(model.Phrase);


                // Use this style query for products - all of the terms entered must match.
                QueryParser parser = new QueryParser(
                        Lucene.Net.Util.Version.LUCENE_29,
                        "title",
                        new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29)
                    );

                //ISynonymEngine engine = new SpecialSynonymEngine();

                //// Same as above, but modified to handle synonyms
                //QueryParser parser = new QueryParser(
                //        Lucene.Net.Util.Version.LUCENE_29,
                //        "title",
                //        new SynonymAnalyzer(Lucene.Net.Util.Version.LUCENE_29, engine)
                //    );

                // This ensures all words must match in the phrase
                parser.SetDefaultOperator(QueryParser.Operator.AND);

                // This ensures similar words will match
                //parser.SetPhraseSlop(3);

                // Sets the current culture
                parser.SetLocale(System.Threading.Thread.CurrentThread.CurrentCulture);
                Query query = parser.Parse(model.Phrase);

                // Use query.Combine to merge this query with individual facets ??


                //// Get the terms from the query
                //string[] terms = model.Phrase.Split(" ".ToCharArray());

                //PhraseQuery query = new PhraseQuery();
                //query.SetSlop(4);

                //foreach (var term in terms)
                //{
                //    query.Add(new Term("title", term));
                //}



                BrowseResult result = this.PerformSearch(query, this.IndexDirectory, null);




                //// Build results for display
                //int totalHits = result.NumHits;
                //BrowseHit[] hits = result.Hits;

                //model.Hits = result.Hits;
                //model.FacetMap = result.FacetMap;
                ////model.TotalHitCount = totalHits;


                PopulateModelResult(model, result);
            }

            return View(model);
        }



        [HttpPost]
        public ActionResult BuildIndex(Models.Search model)
        {
            IndexProducts(this.IndexDirectory);
            IndexSearchTerms(this.IndexDirectory);

            return RedirectToAction("Index");
        }


        [HttpPost]
        public ActionResult SubmitSearchSelections(Models.Search model)
        {
            // Use this style query for products - all of the terms entered must match.
            QueryParser parser = new QueryParser(
                    Lucene.Net.Util.Version.LUCENE_29,
                    "title",
                    new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29)
                );

            // This ensures all words must match in the phrase
            parser.SetDefaultOperator(QueryParser.Operator.AND);

            // This ensures similar words will match
            //parser.SetPhraseSlop(3);



            // Sets the current culture
            parser.SetLocale(System.Threading.Thread.CurrentThread.CurrentCulture);
            Query query = parser.Parse(model.Phrase);

            // Use query.Combine to merge this query with individual facets

            BrowseResult result = this.PerformSearch(query, this.IndexDirectory, model.SelectionGroups);



            //// Build results for display
            //int totalHits = result.NumHits;
            //BrowseHit[] hits = result.Hits;

            //model.Hits = result.Hits;
            //model.FacetMap = result.FacetMap;
            ////model.TotalHitCount = totalHits;

            PopulateModelResult(model, result);


            return Json(model, "application/json");

            //return View("Index", new Models.Search());
        }

        // from http://stackoverflow.com/questions/120180/how-to-do-query-auto-completion-suggestions-in-lucene
        // GET: /MyApp/SuggestTerms?term=something
        public JsonResult SuggestTerms(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new string[] { });

            term = term.Split().Last();

            

            // Fetch suggestions
            string[] suggestions = SearchSvc.SuggestTermsFor(term).ToArray();

            return Json(suggestions, JsonRequestBehavior.AllowGet);
        }



        //[HttpPost]
        //public ActionResult GetAutoComplete(Models.Search model)
        //{

        //    var result = PerformAutoCompleteLookup(model.Phrase, this.IndexDirectory);

        //    var facetMap = result.FacetMap;
        //    var titleFacets = facetMap["title"];
        //    var facetVals = titleFacets.GetFacets();

        //    foreach (var facet in facetVals)
        //    {
        //        model.Results.Add(facet.ToString());
        //    }

        //    model.Phrase = string.Empty;
        //    model.SelectionGroups.Clear();
        //    model.FacetGroups.Clear();

        //    return Json(model, "application/json");
        //}



        private void IndexProducts(string indexPath)
        {
            DateTime startIndexing = DateTime.Now;
            Console.WriteLine("start indexing at: " + startIndexing);

            // read in the books xml
            var productsXml = new XmlDocument();

            string productDataPath = Server.MapPath("~/ProductData/Products.xml");

            productsXml.Load(productDataPath);

            // create the indexer with a standard analyzer
            //var indexWriter = new IndexWriter(indexPath, new StandardAnalyzer(), true);

            var directory = FSDirectory.Open(new System.IO.DirectoryInfo(indexPath));
            bool recreateIndex = true;
            //Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29); // get analyzer

            ISynonymEngine engine = new SpecialSynonymEngine();
            Analyzer analyzer = new SynonymAnalyzer(Lucene.Net.Util.Version.LUCENE_29, engine);

            var indexWriter = new IndexWriter(
                directory,
                analyzer,
                recreateIndex,
                IndexWriter.MaxFieldLength.UNLIMITED);

            try
            {
                // loop through all the books in the books.xml
                foreach (XPathNavigator product in productsXml.CreateNavigator().Select("//product"))
                {
                    // create a Lucene document for this book
                    var doc = new Document();

                    // add the ID as stored but not indexed field, not used to query on
                    //doc.Add(new Field("id", product.GetAttribute("id", string.Empty), Field.Store.YES, Field.Index.NO, Field.TermVector.NO));



                    // add the title and description as stored and tokenized fields, the analyzer processes the content
                    doc.Add(new Field("title", product.SelectSingleNode("title").Value, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO));
                    //doc.Add(new Field("Title2", product.SelectSingleNode("title").Value, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
                    //doc.Add(new Field("description", product.SelectSingleNode("description").Value, Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.NO));


                    // add the title and genre as stored and un tokenized fields, the value is stored as is
                    doc.Add(new Field("Material", product.SelectSingleNode("properties//material").Value, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
                    doc.Add(new Field("Style", product.SelectSingleNode("properties//style").Value, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
                    doc.Add(new Field("Mounting", product.SelectSingleNode("properties//mounting").Value, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
                    doc.Add(new Field("Brand", product.SelectSingleNode("properties//brand").Value, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));


                    // add the publication date as stored and un tokenized field, note the special date handling
                    //DateTime publicationDate = DateTime.Parse(product.SelectSingleNode("publish_date").Value, CultureInfo.InvariantCulture);
                    //doc.Add(new Field("publicationDate", DateField.DateToString(publicationDate), Field.Store.YES, Field.Index.UN_TOKENIZED, Field.TermVector.NO));

                    // add the document to the index
                    indexWriter.AddDocument(doc);
                }

                // make lucene fast
                indexWriter.Optimize();
            }
            finally
            {
                // close the index writer
                indexWriter.Dispose();
            }

            DateTime endIndexing = DateTime.Now;
            Console.WriteLine("end indexing at: " + endIndexing);
            Console.WriteLine("Duration: " + (endIndexing - startIndexing).Seconds + " seconds");
            Console.WriteLine("Number of indexed document: " + indexWriter.NumDocs());
        }

        private void IndexSearchTerms(string sourceIndexPath)
        {
            var directory = FSDirectory.Open(new System.IO.DirectoryInfo(sourceIndexPath));
            SearchSvc.BuildAutoCompleteIndex(directory, "title");
            //SearchSvc.BuildAutoCompleteIndex(directory, "Material");
            //SearchSvc.BuildAutoCompleteIndex(directory, "Style");
            //SearchSvc.BuildAutoCompleteIndex(directory, "Mounting");
            //SearchSvc.BuildAutoCompleteIndex(directory, "Brand");
        }


        private void PopulateModelResult(Models.Search model, BrowseResult result)
        {
            // Clear the input fields
            model.SelectionGroups.Clear();
            model.Phrase = string.Empty;

            // Get the facets and return them in compact format
            if (result.FacetMap.Count() > 0)
            {
                foreach (var map in result.FacetMap)
                {
                    var group = new Models.FacetGroup();

                    group.Name = map.Key;

                    foreach (var f in map.Value.GetFacets())
                    {
                        var facet = new Models.Facet();

                        facet.Name = f.Value.ToString();
                        facet.Count = f.HitCount;

                        group.Facets.Add(facet);
                    }

                    model.FacetGroups.Add(group);
                }
            }

            // Get the results

            foreach (var doc in result.Hits)
            {
                model.Results.Add(doc.StoredFields.GetField("title").StringValue());
            }

        }


        private BrowseResult PerformSearch(Query query, string indexPath, IEnumerable<Models.SelectionGroup> selectionGroups)
        {
            string[] FieldNames = new string[] { "Material", "Style", "Mounting", "Brand" };

            var handlers = new List<FacetHandler>();

            foreach (string field in FieldNames)
            {
                handlers.Add(new SimpleFacetHandler(field));
            }


            // Run the search

            // create the index reader
            var directory = FSDirectory.Open(new System.IO.DirectoryInfo(indexPath));
            var indexReader = DirectoryReader.Open(directory, true);



            //// This is how to get a searcher for executing the search, not for working with facets
            //IndexSearcher indexSearcher = new IndexSearcher(directory, true);
            //indexSearcher.Search(


            // Decorate it with the Bobo index reader
            BoboIndexReader boboReader = BoboIndexReader.GetInstance(indexReader, handlers);

            // create a browse request
            BrowseRequest browseRequest = new BrowseRequest();


            // NOTE: these must be used in production to page the results
            browseRequest.Count = 50;
            browseRequest.Offset = 0;


            browseRequest.FetchStoredFields = true;


            //Query query = new QueryParser(
            //        Lucene.Net.Util.Version.LUCENE_29,
            //        "title",
            //        new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29)
            //    ).Parse(model.Phrase);


            if (selectionGroups != null)
            {
                // Add the selections to the search

                foreach (var group in selectionGroups)
                {
                    BrowseSelection sel = new BrowseSelection(group.Name);
                    foreach (var value in group.Selections)
                    {
                        sel.AddValue(value);
                    }
                    browseRequest.AddSelection(sel);
                }

           

            }
            

            browseRequest.Query = query;

            //// add the facet output specs
            //FacetSpec brandSpec = new FacetSpec();
            //brandSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;

            //browseRequest.SetFacetSpec("Brand", brandSpec);

            foreach (var name in FieldNames)
            {
                // add the facet output specs
                FacetSpec spec = new FacetSpec();
                //spec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;


                // NOTE: if this is a secondary search, we want to get the facets with 0 
                // hits so the checkboxes can be set on the UI...or otherwise program the UI 
                // to disable and set to 0 all of the selections that aren't in the result
                if (selectionGroups != null)
                {
                    spec.MinHitCount = 0;
                }

                browseRequest.SetFacetSpec(name, spec);
            }


            // perform browse
            IBrowsable browser = new BoboBrowser(boboReader);

            BrowseResult result = browser.Browse(browseRequest);

            return result;
        }

        private BrowseResult PerformAutoCompleteLookup(string prefix, string indexPath)
        {
            FacetHandler handler = new MultiValueFacetHandler("title");

            Directory directory = FSDirectory.Open(new System.IO.DirectoryInfo(indexPath));
            IndexReader indexReader = IndexReader.Open(directory, true);

            // decorate it with a bobo index reader
            BoboIndexReader boboReader = BoboIndexReader.GetInstance(indexReader, new FacetHandler[] { handler });

            BrowseRequest browseRequest = new BrowseRequest();
            browseRequest.Count = 8;
            browseRequest.Offset = 0;
            browseRequest.FetchStoredFields = true;

            // add a selection
            BrowseSelection sel = new BrowseSelection("title");
            //sel.AddValue("alexey");
            browseRequest.AddSelection(sel);

            // parse a query
            // NOTE: this was "Entity" originally
            QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "title", new KeywordAnalyzer());
            Query q = parser.Parse("SPListItem");
            browseRequest.Query = q;

            // add the facet output specs
            FacetSpec spec = new FacetSpec();
            spec.Prefix = prefix;
            spec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;

            browseRequest.SetFacetSpec("title", spec);

            // perform browse
            IBrowsable browser = new BoboBrowser(boboReader);

            BrowseResult result = browser.Browse(browseRequest);

            return result;

            //// Showing results now
            //Dictionary<String, IFacetAccessible> facetMap = result.FacetMap;

            //IFacetAccessible colorFacets = facetMap["Body"];

            //IEnumerable<BrowseFacet> facetVals = colorFacets.GetFacets();

            //Debug.WriteLine("Facets:");

            //int count = 0;
            //foreach (BrowseFacet facet in facetVals)
            //{
            //    count++;
            //    Debug.WriteLine(facet.ToString());
            //}
            //Debug.WriteLine("Total = " + count);
        }



        #region Old Code

        //[HttpPost]
        //public ActionResult Index(Models.Search model)
        //{
        //    //if (ModelState.IsValid && WebSecurity.Login(model.UserName, model.Password, persistCookie: model.RememberMe))
        //    //{
        //    //    return RedirectToLocal(returnUrl);
        //    //}

        //    //// If we got this far, something failed, redisplay form
        //    //ModelState.AddModelError("", "The user name or password provided is incorrect.");
        //    //return View(model);

        //    // Run the search
        //    var hits = PerformSearchForFacets(model.Phrase, this.IndexDirectory);
        //    //var facetList = new List<Models.SearchHitsPerFacet>();
        //    ////long hitCount = hits.TotalHitCount;

        //    ////for (long i = 0; i < hitCount; i++)
        //    ////{
        //    ////    var f = new Models.SearchHitsPerFacet();
        //    ////    f.HitCount = 
        //    ////}

        //    //foreach (SimpleFacetedSearch.HitsPerFacet hit in hits.HitsPerFacet)
        //    //{
        //    //    var f = new Models.SearchHitsPerFacet();
        //    //    f.HitCount = hit.HitCount;
        //    //    f.Name = hit.Name.ToString();
        //    //    facetList.Add(f);
        //    //}

        //    //model.Hits = facetList.ToArray();
        //    //model.TotalHitCount = facetList.Count();

        //    string[] properties = new string[] { "material", "style", "mounting", "brand" };

        //    var list = new List<Models.Facet>();

        //    foreach (string property in properties)
        //    {
        //        var p = new Models.Facet();
        //        p.Name = property;

        //        foreach (SimpleFacetedSearch.HitsPerFacet hpg in hits.HitsPerFacet)
        //        {
        //            long hitCountPerGroup = hpg.HitCount;
        //            SimpleFacetedSearch.FacetName facetName = hpg.Name;

        //                for (int i = 0; i < facetName.Length; i++)
        //                {
        //                    string part = facetName[i];

        //                    if (part == hpg.Current.GetField(property).StringValue)
        //                    {
        //                        p.Count += 1;
        //                    }
        //                }

        //            foreach (Document doc in hpg.Documents)
        //            {
        //                string text = doc.GetField("text").StringValue;
        //                System.Diagnostics.Debug.WriteLine(">>" + facetName + ": " + text);
        //            }
        //        }
        //    }



        //    model.Hits = hits;
        //    model.TotalHitCount = hits.HitsPerFacet.Count();


        //    return View(model);
        //}



        //private void IndexProducts(string indexPath)
        //{
        //    DateTime startIndexing = DateTime.Now;
        //    Console.WriteLine("start indexing at: " + startIndexing);

        //    // read in the books xml
        //    var productsXml = new XmlDocument();

        //    string productDataPath = Server.MapPath("~/ProductData/Products.xml");

        //    productsXml.Load(productDataPath);

        //    // create the indexer with a standard analyzer
        //    //var indexWriter = new IndexWriter(indexPath, new StandardAnalyzer(), true);

        //    var directory = FSDirectory.Open(indexPath);
        //    bool recreateIndex = true;
        //    Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30); // get analyzer

        //    var indexWriter = new IndexWriter(
        //        directory,
        //        analyzer, 
        //        recreateIndex, 
        //        IndexWriter.MaxFieldLength.UNLIMITED);

        //    try
        //    {
        //        // loop through all the books in the books.xml
        //        foreach (XPathNavigator product in productsXml.CreateNavigator().Select("//product"))
        //        {
        //            // create a Lucene document for this book
        //            var doc = new Document();

        //            // add the ID as stored but not indexed field, not used to query on
        //            //doc.Add(new Field("id", product.GetAttribute("id", string.Empty), Field.Store.YES, Field.Index.NO, Field.TermVector.NO));



        //            // add the title and description as stored and tokenized fields, the analyzer processes the content
        //            doc.Add(new Field("title", product.SelectSingleNode("title").Value, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO));
        //            //doc.Add(new Field("description", product.SelectSingleNode("description").Value, Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.NO));


        //            // add the title and genre as stored and un tokenized fields, the value is stored as is
        //            doc.Add(new Field("material", product.SelectSingleNode("properties//material").Value, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
        //            doc.Add(new Field("style", product.SelectSingleNode("properties//style").Value, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
        //            doc.Add(new Field("mounting", product.SelectSingleNode("properties//mounting").Value, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
        //            doc.Add(new Field("brand", product.SelectSingleNode("properties//brand").Value, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));


        //            // add the publication date as stored and un tokenized field, note the special date handling
        //            //DateTime publicationDate = DateTime.Parse(product.SelectSingleNode("publish_date").Value, CultureInfo.InvariantCulture);
        //            //doc.Add(new Field("publicationDate", DateField.DateToString(publicationDate), Field.Store.YES, Field.Index.UN_TOKENIZED, Field.TermVector.NO));

        //            // add the document to the index
        //            indexWriter.AddDocument(doc);
        //        }

        //        // make lucene fast
        //        indexWriter.Optimize();
        //    }
        //    finally
        //    {
        //        // close the index writer
        //        indexWriter.Dispose();
        //    }

        //    DateTime endIndexing = DateTime.Now;
        //    Console.WriteLine("end indexing at: " + endIndexing);
        //    Console.WriteLine("Duration: " + (endIndexing - startIndexing).Seconds + " seconds");
        //    Console.WriteLine("Number of indexed document: " + indexWriter.NumDocs());
        //}



        //private SimpleFacetedSearch.Hits PerformSearchForFacets(string phrase, string indexPath)
        //{
        //    // Run the search

        //    // create the index reader
        //    var directory = FSDirectory.Open(indexPath);
        //    var indexReader = DirectoryReader.Open(directory, true);

        //    // This is how to get a searcher for executing the search, not for working with facets
        //    //IndexReader indexReader = new IndexSearcher(directory);

        //    // create the query
        //    Query query = new QueryParser(
        //            Lucene.Net.Util.Version.LUCENE_30, 
        //            "text", 
        //            new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30)
        //        ).Parse(phrase);

        //    // Pass in the reader and the names of the facets
        //    string[] facets = new string[] { "material", "style", "mounting", "brand" };
        //    SimpleFacetedSearch sfs = new SimpleFacetedSearch(indexReader, facets);

        //    // Pass in the query like you would with a typical search class!!!!
        //    SimpleFacetedSearch.Hits hits = sfs.Search(query, 50);


        //    // What comes back is different from a normal search.
        //    // The result documents and hits are grouped by facets.

        //    //// Iterate over the groups of hits-per-facet
        //    //long totalHits = hits.TotalHitCount;
        //    //foreach (SimpleFacetedSearch.HitsPerFacet hpf in hits.HitsPerFacet)
        //    //{

        //    //}


        //    // Show the results

        //    return hits;
        //}


        #endregion

    }
}
