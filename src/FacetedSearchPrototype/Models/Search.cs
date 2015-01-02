using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lucene.Net.Search;
using BoboBrowse.Net;

namespace FacetedSearchPrototype.Models
{
    public class Search
    {
        public Search()
        {
            //Hits = new BrowseHit[0];
            //FacetMap = new Dictionary<string, IFacetAccessible>();
            SelectionGroups = new List<SelectionGroup>();

            Results = new List<string>();
            FacetGroups = new List<FacetGroup>();
        }

        [Display(Name = "Search phrase")]
        public string Phrase { get; set; } // INPUT
        public List<SelectionGroup> SelectionGroups { get; set; } // INPUT 



        //////public SimpleFacetedSearch.Hits Hits { get; set; } // OUTPUT
        //////public SearchHitsPerFacet[] Hits { get; set; }
        //////public long TotalHitCount { get; set; }

        //public BrowseHit[] Hits { get; set; }

        //public Dictionary<String, IFacetAccessible> FacetMap { get; set; }


        // OUTPUT
        public List<string> Results { get; set; } // NOTE: these will need to be objects in production so additional values can be passed
        public List<FacetGroup> FacetGroups { get; set; }
    }

    //public class SearchHitsPerFacet
    //{
    //    public long HitCount { get; set; }
    //    public string Name { get; set; }
    //}


    // OUTPUT FROM LUCENE
    public class Facet
    {
        public string Name { get; set; }
        public long Count { get; set; }
    }


    // OUTPUT FROM LUCENE
    public class FacetGroup
    {
        public FacetGroup()
        {
            Facets = new List<Facet>();
        }

        public string Name { get; set; }
        public List<Facet> Facets { get; set; }
    }


 



        // INPUT FROM PAGE
    public class SelectionGroup
    {
        public SelectionGroup()
        {
            Selections = new List<string>();
        }

        public string Name { get; set; }
        public List<string> Selections { get; set; }
    }

}