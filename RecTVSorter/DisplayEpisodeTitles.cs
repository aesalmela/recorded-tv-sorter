using System;
using System.Collections.Generic;
using TVDBSharp.Models;

namespace RecTVSorter
{
    /// <summary>
    ///     This example will demonstrate how to retrieve the titles of every episode in a season.
    /// </summary>
    public class DisplayEpisodeTitles
    {
        public static void Print(List<Episode> episodes)
        {
            foreach (var episode in episodes)
            {
                Console.WriteLine(episode.Title);
            }
        }
    }
}