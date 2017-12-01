using System;
using System.Collections.Generic;
using TVDBSharp.Models;

namespace RecTVSorter
{
    public class DisplaySearchResult
    {
        public static void Print(List<Show> searchResults)
        {
            foreach (var show in searchResults)
            {
                Console.WriteLine("{0}:\t{1}", show.Name, show.Id);
            }
        }
    }
}