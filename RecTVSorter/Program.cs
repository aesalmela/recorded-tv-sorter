using Shell32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TVDBSharp;
using TVDBSharp.Models.Enums;

namespace RecTVSorter
{
    internal class Program
    {
        public static string logPath = "";
        public static string apikey = "";

        public static void Main(string[] args)
        {
            bool goodLogFile = false;
            try
            {
                //Setup variables
                apikey = ConfigurationManager.AppSettings["apikey"];

                string[] sources = ConfigurationManager.AppSettings["searchLocations"].Split(',');
                string destination = ConfigurationManager.AppSettings["sortedLocation"];
                logPath = ConfigurationManager.AppSettings["log"];

                //Pre Checks
                string preCheckMsg = "";
                if (apikey == "")
                {
                    preCheckMsg = "API Key is empty. ";
                }
                if (sources.Length < 1)
                {
                    preCheckMsg += "No search locations exist. ";
                }
                else
                {
                    foreach (string sourcePath in sources)
                    {
                        if (!Directory.Exists(sourcePath))
                        {
                            preCheckMsg += "Source location " + sourcePath + " doesn't exist. ";
                        }
                    }
                }
                if (destination == "")
                {
                    preCheckMsg += "Location to put sorted files does not exist. ";
                }
                else
                {
                    if (!Directory.Exists(destination))
                    {
                        preCheckMsg += "Destination location " + destination + " does not exist. ";
                    }
                }

                if (logPath == "")
                {
                    preCheckMsg += "Log file location does not exist. ";
                }
                else
                {
                    FileInfo logFH = new FileInfo(logPath);
                    if (!logFH.Directory.Exists)
                    {
                        preCheckMsg += "Log file path " + logFH.Directory.ToString() + " does not exist. ";
                    }
                    else
                    {
                        goodLogFile = true;
                    }
                }

                if (preCheckMsg == "")
                {
                    List<string> tvShows = new List<string>();
                    foreach (string srcLoc in sources)
                    {
                        tvShows.AddRange(GetTVFiles(srcLoc.Trim(), false));
                    }

                    if (tvShows.Count > 0)
                    {
                        //We have work to do
                        Console.WriteLine("Sorting recorded shows. This will move large files and may take awhile...");
                        sortTV(tvShows, destination);
                    }
                    else
                    {
                        File.AppendAllText(logPath, DateTime.Now.ToString() + ": No new tv recordings to rename and move." + Environment.NewLine);
                    }

                    KeepLastFiveOnly(destination);
                    DeleteEmptyDirs(destination);
                }
                else
                {
                    if (goodLogFile)
                    {
                        File.AppendAllText(logPath, DateTime.Now.ToString() + ": " + preCheckMsg.Trim() + Environment.NewLine);
                        Console.WriteLine("Errors occurred before getting started. Check the log file at " + logPath);
                    }
                    else
                    {
                        Console.WriteLine(preCheckMsg.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                if (goodLogFile)
                {
                    File.AppendAllText(logPath, DateTime.Now.ToString() + ": " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine);
                    Console.WriteLine("Errors occurred before getting started. Check the log file at " + logPath);
                }
                else
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        private static void sortTV(List<string> tvShows, string destination)
        {
            try {
                foreach (string showPath in tvShows)
                {
                    File.AppendAllText(logPath, DateTime.Now.ToString() + ": Attempting to sort " + showPath.Trim() + Environment.NewLine);

                    RecTV show = GetRecTVDetails(showPath.Trim());

                    File.AppendAllText(logPath, "\tTitle:\t\t" + show.title + Environment.NewLine);
                    File.AppendAllText(logPath, "\tEpisodeID:\t" + show.epID + Environment.NewLine);
                    File.AppendAllText(logPath, "\tEpisode:\t" + show.epName + Environment.NewLine);
                    File.AppendAllText(logPath, "\tRecord Time:\t" + show.recDateTime.ToString() + Environment.NewLine);

                    if (show.sortable)
                    {
                        RenameAndMove(ref show, ref destination);
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, DateTime.Now.ToString() + ": " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine);
                Console.WriteLine("Errors occurred. Check the log file at " + logPath);
            }
        }

        private static RecTV GetRecTVDetails(string showPath)
        {
            RecTV show = new RecTV(showPath);
            show.sortable = true;

            try {
                FileInfo showFH = new FileInfo(showPath.Trim());
                show.parentDir = showFH.DirectoryName;
                show.fileExt = showFH.Extension;
                show.fileName = showFH.Name.Replace(show.fileExt, "").TrimEnd('.');

                string episodeIDPattern = @"^S\d{1,2}E\d{1,2}$";
                string dateTimePattern = @"^\d{4}_\d{2}_\d{2}_\d{2}_\d{2}_\d{2}$";
                string[] parts = show.fileName.Split(new string[] { " - " }, StringSplitOptions.None);
                switch (parts.Count())
                {
                    case 2:
                        show.title = parts[0];

                        Match m1 = Regex.Match(parts[1].ToUpper(), episodeIDPattern);
                        if (m1.Success)
                        {
                            show.epID = parts[1].ToUpper();
                        }
                        else
                        {
                            Match m2 = Regex.Match(parts[1], dateTimePattern);
                            if (m2.Success)
                            {
                                show.recDateTime = DateTime.ParseExact(parts[1], "yyyy_MM_dd_HH_mm_ss", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                show.epName = parts[1];
                            }
                        }
                        break;
                    case 3:
                        show.title = parts[0];

                        Match m3 = Regex.Match(parts[1].ToUpper(), episodeIDPattern);
                        if (m3.Success)
                        {
                            show.epID = parts[1].ToUpper();
                        }
                        else
                        {
                            show.epName = parts[1];
                        }

                        Match m4 = Regex.Match(parts[2], dateTimePattern);
                        if (m4.Success)
                        {
                            show.recDateTime = DateTime.ParseExact(parts[2], "yyyy_MM_dd_HH_mm_ss", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            show.epName = parts[2];
                        }

                        break;
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                        show.sortable = false;
                        File.AppendAllText(logPath, "\tCan't sort. Must be an extra hyphen in the Title or Episode name." + Environment.NewLine);
                        break;

                }

                if (show.recDateTime.Year == 0001)
                {
                    show.recDateTime = showFH.CreationTime;
                }

                if (show.epID != "" && show.epID != null)
                {
                    FormatEpID(ref show);
                }
                else
                {
                    if (show.title != "" && show.title != null)
                    {
                        //Read extended properties
                        if (show.epName == null || show.epName == "")
                        {
                            GetEpisodeInfoFromMetaData(ref show);
                        }

                        if (show.epName != "" && show.epName != null)
                        {
                            GetEpIDFromTVDB(ref show);
                        }
                        else
                        {
                            if (show.epDesc != "" && show.epDesc != null)
                            {
                                GetEpFromTVDBByDesc(ref show);
                                if (show.epID == "" || show.epID == null)
                                {
                                    GetEpFromTVDBByRecTime(ref show);
                                }
                            }
                            else
                            {
                                GetEpFromTVDBByRecTime(ref show);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                show.sortable = false;
                File.AppendAllText(logPath, DateTime.Now.ToString() + ": " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine);
                Console.WriteLine("Errors occurred. Check the log file at " + logPath);
            }

            return show;
        }

        private static void RenameAndMove(ref RecTV show, ref string destination)
        {
            try {

                List<string> noSeasonFolders = ConfigurationManager.AppSettings["NoSeasonFolders"].Split(',').ToList();

                string destPath = FindShowTitleFolder(ref destination, show.title);
                string newFileName = show.title + " - ";
                string title = show.title;

                if (!noSeasonFolders.Where(s => s.ToLower().Equals(title.ToLower())).Any())
                {
                    if (show.epID != "" && show.epID != null)
                    {
                        newFileName += show.epID + " - ";

                        int season = Convert.ToInt32(show.epID.Substring(1, 2));
                        if (!Directory.Exists(destPath + @"\" + "Season " + season))
                        {
                            Directory.CreateDirectory(destPath + @"\" + "Season " + season);
                        }
                        destPath += @"\" + "Season " + season;
                    }
                }

                if (show.epName != "" && show.epName != null)
                {
                    newFileName += show.epName;

                    //Add date for unique name if episode name already exist
                    if (File.Exists(destPath + @"\" + newFileName + show.fileExt))
                    {
                        newFileName += " - " + show.recDateTime.ToString("yyyy_MM_dd_hh_mm_ss");
                    }
                }
                else
                {
                    if (show.epID != "" && show.epID != null)
                    {
                        newFileName = newFileName.Trim().TrimEnd('-').Trim();
                    }
                    else
                    {
                        newFileName += show.recDateTime.ToString("yyyy_MM_dd_hh_mm_ss");
                    }
                }
                newFileName += show.fileExt;

                //clean bad characters
                newFileName = String.Join(" ", newFileName.Split(Path.GetInvalidFileNameChars()));

                if (!File.Exists(destPath + @"\" + newFileName))
                {
                    File.AppendAllText(logPath, "\tMoving to:\t" + destPath + @"\" + newFileName + Environment.NewLine);
                    //move file
                    try {
                        File.Move(show.filePath, destPath + @"\" + newFileName);
                    }
                    catch (IOException e)
                    {
                        File.AppendAllText(logPath, "\tError: Cannot move. File is still being used." + Environment.NewLine);
                    }
                }
                else
                {
                    File.AppendAllText(logPath, "\tError: cannot move. The file " + destPath + @"\" + newFileName + " already exists." + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, DateTime.Now.ToString() + ": " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine);
                Console.WriteLine("Errors occurred. Check the log file at " + logPath);
            }
        }

        private static string FindShowTitleFolder(ref string destination,string title)
        {
            string destPath = destination + @"\";

            var matchDirs = Directory.GetDirectories(destination).Where(d => d.Remove(0, destPath.Length).ToLower().Trim().StartsWith(title.ToLower().Trim())).ToList();
            if (matchDirs.Count > 0)
            {
                destPath = matchDirs[0];
            }
            else
            {
                destPath += title.Trim();
                //clean bad characters
                destPath = String.Join("", destPath.Split(Path.GetInvalidPathChars()));
                //create dir
                if (!Directory.Exists(destPath))
                {
                    Directory.CreateDirectory(destPath);
                }
            }

            return destPath;
        }

        private static List<string> GetTVFiles (string path, bool recursive)
        {
            List<string> files = new List<string>();
            if (recursive)
            {
                files = Directory.GetFiles(path, "*", SearchOption.AllDirectories).Where(f => f.EndsWith(".wtv") || f.EndsWith(".mp4") || f.EndsWith(".avi")).ToList();
            }
            else
            {
                files = Directory.GetFiles(path).Where(f => f.EndsWith(".wtv") || f.EndsWith(".mp4") || f.EndsWith(".avi")).ToList();
            }
            return files;
        }

        private static void FormatEpID(ref RecTV show)
        {
            try
            {
                string[] epIDParts = show.epID.Split(new char[] { 'S', 'E' });
                string seasonNum = epIDParts[1].PadLeft(2, '0');
                string epNum = epIDParts[2].PadLeft(2, '0');

                show.epID = 'S' + seasonNum + 'E' + epNum;
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, DateTime.Now.ToString() + ": " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine);
                Console.WriteLine("Errors occurred. Check the log file at " + logPath);
            }
        }

        private static void GetEpisodeInfoFromMetaData(ref RecTV show)
        {
            try {
                Shell32.Shell shell = new Shell32.Shell();
                Shell32.Folder objFolder;

                objFolder = shell.NameSpace(show.parentDir);

                //get the index of the path item
                int index = -1;
                FileInfo fh = new FileInfo(show.filePath);
                DirectoryInfo dh = fh.Directory;
                FileSystemInfo[] dirContent = dh.GetFileSystemInfos();

                int desktopINIOffset = 0;
                if (dirContent.Where(f => f.Name == "desktop.ini").ToList().Count() > 0)
                {
                    desktopINIOffset = 1;
                }
                int tempRecOffset = 0;
                if (dirContent.Where(f => f.Name == "TempRec").ToList().Count() > 0)
                {
                    tempRecOffset = 1;
                }
                int tempSBEOffset = 0;
                if (dirContent.Where(f => f.Name == "TempSBE").ToList().Count() > 0)
                {
                    tempSBEOffset = 1;
                }
                int thumbsOffset = 0;
                if (dirContent.Where(f => f.Name == "Thumbs.db").ToList().Count() > 0)
                {
                    thumbsOffset = 1;
                }

                for (int i = 0; i < dh.GetFileSystemInfos().Count(); i++)
                {
                    if (dh.GetFileSystemInfos().ElementAt(i).Name == fh.Name) //we've found the item in the folder
                    {
                        if (fh.Name.CompareTo("Thumbs.db") > 0)
                        {
                            index = i - desktopINIOffset - tempRecOffset - tempSBEOffset - thumbsOffset;
                        }
                        else
                        {
                            if (fh.Name.CompareTo("TempSBE") > 0)
                            {
                                index = i - desktopINIOffset - tempRecOffset - tempSBEOffset;
                            }
                            else
                            {
                                if (fh.Name.CompareTo("TempRec") > 0)
                                {
                                    index = i - desktopINIOffset - tempRecOffset;
                                }
                                else
                                {
                                    if (fh.Name.CompareTo("desktop.ini") > 0)
                                    {
                                        index = i - desktopINIOffset;
                                    }
                                    else
                                    {
                                        index = i;
                                    }
                                }
                            }
                        }
                        break;
                    }
                }

                if (index > -1)
                {
                    FolderItem fi = objFolder.Items().Item(index);
                    string name = fi.Name;
                    if (name == show.fileName + show.fileExt)
                    {
                        //Episode name is 254
                        string episodeName = objFolder.GetDetailsOf(fi, 254);
                        show.epName = episodeName;
                        if (episodeName != "")
                        {
                            File.AppendAllText(logPath, "\tFound episode name: " + episodeName + " for file " + name + Environment.NewLine);
                        }

                        //Episode description is 259
                        string episodeDesc = objFolder.GetDetailsOf(fi, 259);
                        show.epDesc = episodeDesc;
                    }
                    else
                    {
                        File.AppendAllText(logPath, "\tError: " + show.fileName + show.fileExt  + " does not match " + name + Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, DateTime.Now.ToString() + ": " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine);
                Console.WriteLine("Errors occurred. Check the log file at " + logPath);
            }
        }

        private static void GetEpIDFromTVDB(ref RecTV show)
        {
            try {
                char[] ignoreChars = new char[] { ' ', ';', ',', '\r', '\t', '\n' };
                var tvdb = new TVDB(apikey);
                string epName = show.epName.ToLower();

                var shows = tvdb.Search(show.title);
                File.AppendAllText(logPath, "\tFound: \t" + shows.Count + " matching titles. Checking episodes..." + Environment.NewLine);

                foreach (var s in shows)
                {
                    var episodes = s.Episodes.Where(ep => Regex.Replace(ep.Title.ToLower(), "[^0-9a-zA-Z]+", "") == Regex.Replace(epName, "[^0-9a-zA-Z]+", "")).ToList();
                    if (episodes.Count > 0)
                    {
                        show.epID = 'S' + episodes[0].SeasonNumber.ToString() + 'E' + episodes[0].EpisodeNumber.ToString();
                        FormatEpID(ref show);
                        File.AppendAllText(logPath, "\tFound a matching episode name.  Taking the first match: " + show.epID + Environment.NewLine);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, DateTime.Now.ToString() + ": " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine);
                Console.WriteLine("Errors occurred. Check the log file at " + logPath);
            }
        }

        private static void GetEpFromTVDBByDesc(ref RecTV show)
        {
            try
            {
                var tvdb = new TVDB(apikey);
                string epDesc = show.epDesc.ToLower();

                var shows = tvdb.Search(show.title);
                File.AppendAllText(logPath, "\tFound: \t" + shows.Count + " matching titles. Checking episodes..." + Environment.NewLine);

                switch (show.title.ToLower())
                {
                    case "saturday night live":
                        Regex rgxStart = new Regex("^Host ");
                        epDesc = rgxStart.Replace(epDesc, "");
                        Regex rgxEnd = new Regex(" performs.$");
                        epDesc = rgxEnd.Replace(epDesc, "");
                        Regex rgxEnd2 = new Regex(" hosts and performs.$");
                        epDesc = rgxEnd2.Replace(epDesc, "");
                        epDesc = epDesc.Replace("; ", "/");
                        break;
                }

                foreach (var s in shows)
                {
                    var episodes = s.Episodes.Where(ep => ep.Description.ToLower() == epDesc).ToList();
                    if (episodes.Count > 0)
                    {
                        show.epName = episodes[0].Title;
                        show.epID = 'S' + episodes[0].SeasonNumber.ToString() + 'E' + episodes[0].EpisodeNumber.ToString();
                        FormatEpID(ref show);
                        File.AppendAllText(logPath, "\tFound a matching episode name.  Taking the first match: " + show.epID + Environment.NewLine);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, DateTime.Now.ToString() + ": " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine);
                Console.WriteLine("Errors occurred. Check the log file at " + logPath);
            }
        }

        private static void GetEpFromTVDBByRecTime(ref RecTV show)
        {
            try
            {
                var tvdb = new TVDB(apikey);
                DateTime epRecTime = show.recDateTime;

                var shows = tvdb.Search(show.title);
                File.AppendAllText(logPath, "\tFound: \t" + shows.Count + " matching titles. Checking episodes..." + Environment.NewLine);


                foreach (var s in shows)
                {
                    var episodes = s.Episodes.Where(ep => ep.FirstAired == epRecTime.Date).ToList();
                    if (episodes.Count > 0)
                    {
                        show.epName = episodes[0].Title;
                        show.epID = 'S' + episodes[0].SeasonNumber.ToString() + 'E' + episodes[0].EpisodeNumber.ToString();
                        FormatEpID(ref show);
                        File.AppendAllText(logPath, "\tFound a matching episode name.  Taking the first match: " + show.epID + Environment.NewLine);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, DateTime.Now.ToString() + ": " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine);
                Console.WriteLine("Errors occurred. Check the log file at " + logPath);
            }
        }

        private static void KeepLastFiveOnly(string destination)
        {
            if (ConfigurationManager.AppSettings["Keep5"] != null)
            {
                string[] seriesToClean = ConfigurationManager.AppSettings["Keep5"].Split(',');

                foreach (string series in seriesToClean)
                {
                    string path = destination + @"\" + series.Trim();
                    if (Directory.Exists(path))
                    {
                        List<string> files = GetTVFiles(path, true);
                        foreach (string file in files)
                        {
                            FileInfo fi = new FileInfo(file.Trim());
                            string fileName = fi.Name.Replace(fi.Extension, "").TrimEnd('.');

                            string dateTimePattern = @"^\d{4}_\d{2}_\d{2}_\d{2}_\d{2}_\d{2}$";
                            string[] parts = fileName.Split(new string[] { " - " }, StringSplitOptions.None);
                            DateTime dtNow = DateTime.Now;
                            int olderThan = 7;
                            switch (dtNow.DayOfWeek)
                            {
                                case DayOfWeek.Saturday:
                                    olderThan++;
                                    break;
                                case DayOfWeek.Sunday:
                                    olderThan++;
                                    olderThan++;
                                    break;
                            }
                            DateTime dtRecTime = dtNow;
                            string recDatePart = parts[parts.Length - 1];
                            Match m = Regex.Match(parts[1], dateTimePattern);
                            if (m.Success)
                            {
                                dtRecTime = DateTime.ParseExact(recDatePart, "yyyy_MM_dd_HH_mm_ss", System.Globalization.CultureInfo.InvariantCulture);
                            }

                            TimeSpan elapsed = dtNow.Subtract(dtRecTime);
                            if (elapsed.TotalDays > olderThan)
                            {
                                File.AppendAllText(logPath, "Recording is older than 5 days. Deleting:" + file + Environment.NewLine);
                                File.Delete(file);
                            }
                        }
                    }
                }
            }
        }

        private static void DeleteEmptyDirs(string dir)
        {
            if (String.IsNullOrEmpty(dir))
                throw new ArgumentException(
                    "Starting directory is a null reference or an empty string",
                    "dir");

            try
            {
                foreach (var d in Directory.EnumerateDirectories(dir))
                {
                    DeleteEmptyDirs(d);
                }

                var entries = Directory.EnumerateFileSystemEntries(dir);

                if (!entries.Any())
                {
                    File.AppendAllText(logPath, DateTime.Now.ToString() + ": Removing empty directory " + dir + Environment.NewLine);
                    Directory.Delete(dir);
                }
            }
            catch (Exception ex) {
                File.AppendAllText(logPath, DateTime.Now.ToString() + ": " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine);
                Console.WriteLine("Errors occurred. Check the log file at " + logPath);
            }
        }





        private static void GetSpecificShow(TVDB tvdb)
        {
            Console.WriteLine("Game of Thrones");
            var got = tvdb.GetShow(121361);
            DisplayShowDetails.Print(got);
            Console.WriteLine("-----------");
        }

        private static void GetSpecificEpisode(TVDB tvdb)
        {
            Console.WriteLine("Game of Thrones s04e01");
            var episode = tvdb.GetEpisode(4721938);
            DisplayEpisodeDetails.Print(episode);
            Console.WriteLine("-----------");
        }

        private static void GetEpisodeTitlesForSeason(TVDB tvdb)
        {
            Console.WriteLine("Episodes of Game of Thrones season 2");
            var show = tvdb.GetShow(121361);
            var season2Episodes = show.Episodes.Where(ep => ep.SeasonNumber == 2).ToList();
            DisplayEpisodeTitles.Print(season2Episodes);
            Console.WriteLine("-----------");
        }

        private static void SearchShow(TVDB tvdb)
        {
            //KEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Media Center\Service\Recording

            //Add or edit the String Value – ‘filenaming’
            //% Ch – Channel Number
            //% Cn – Station Name
            //% T – Title of show
            //% Et – Episode Title
            //% Dt – Date and Time

            Console.WriteLine("Search for Saturday Night Live episode Lame Gretzky on tvdb");
            var shows = tvdb.Search("Goldbergs");
            foreach (var s in shows)
            {
                Console.WriteLine("Show name is: " + s.Name);
            }
            foreach (var e in shows[1].Episodes)
            {
                Console.WriteLine("Title: " + e.Title);
            }
            var episodes = shows[1].Episodes.Where(ep => ep.Title == "Lame Gretzky").ToList();
            Console.WriteLine("Season number is: " + episodes[0].SeasonNumber.ToString());
            Console.WriteLine("Episode number is: " + episodes[0].EpisodeNumber.ToString());
            Console.WriteLine("-----------");
        }

        private static void GetUpdates(TVDB tvdb)
        {
            var updates = tvdb.GetUpdates(Interval.Day);
            Console.WriteLine("Updates during the last 24 hours on thetvdb, since {0}", updates.Timestamp);
            DisplayUpdates.Print(updates);
        }
    }
}