using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Immaculate.objects;
using System.IO;
using System.Windows.Forms;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Immaculate
{
    public partial class MainWindow : Window
    {
        private ArrayList filmsAdded;
        private string directorySelected= "";
        private String[] fileExentions = { "mp4", "avi", "mkv", "m4v" };
        private String[] unwantedWords =
            { "DVRIP", "AC3", "HDRIP", "XvidHD", "DVDRIP", "Xvid", "EXTENDED", "WEB",
                "DL", "HEVC", "2CH", "x265", "720p", "1080p", "x264", "BLURAY", "FINALCUT",
                "eng", "subs", "H264", "HC", "HDTV", "AAC", "BRRIP", "FXG", "ACC", "ETRG",
                "DTS", "JYK","YIFY", "BOKUTOX", "Ozlem", "ALTERNATE", "ENDING", "[AC 3", "[AC" , "3",
        "JUDAS", "5.1" , "5", "1"};

        private Film selectedFilm;

        // Contructor
        public MainWindow()
        {
            InitializeComponent();
            filmsAdded = new ArrayList();
            renameFileButton.Visibility = Visibility.Hidden;
        }

        // Gives the user the diolog to select folder and recieves the names of the files.
        private void getFilesFromFolder()
        {
            filmsAdded = new ArrayList();
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                directorySelected = fbd.SelectedPath;

                if (!string.IsNullOrWhiteSpace(directorySelected))
                {
                    string[] files = Directory.GetFiles(directorySelected);

                    if (files.Length > 0)
                    {
                        for(int i = 0; i < files.Length; i++)
                        {
                            Film f = new Film();
                            f.origin = files[i];
                            f.title = System.IO.Path.GetFileName(files[i]);
                            filmsAdded.Add(f);
                        }
                    }
                }
            }
        }



        // Returns the film most likelyt to be the correct one.
        private FilmDetails getHighestMatched(string str, FilmInfoRaw account){
            int score = 99999999;
            int num = 0;
            int size = account.results.Count;

            for(int i = 0; i < size; i++){
                int a = levenshtein(str, (string)account.results[i]["title"]);
                if(a < score){
                    score = a;
                    num = i;
                }
            }

            if(score == 99999999){
                return null;
            }else {
                Console.WriteLine("FOUND: " + account.results[num]["title"] + " SCORE:  " + score);
                FilmDetails details = new FilmDetails();
                details.title = account.results[num]["title"];
                details.id = account.results[num]["id"];
                details.overview = account.results[num]["overview"];
                details.date = account.results[num]["release_date"];
                details.poster = account.results[num]["poster_path"];
                details.backdrop = account.results[num]["backdrop_path"];
                details.rating = account.results[num]["vote_average"];

                details.date = getYear(details.date);

                return details;
            }
        }

        // Searches for the film with a name and year, returns info about the film.
        private FilmDetails searchForFilm(string name, string year, string str){
            var json = "";

            try{
                using (var wc = new WebClient()){
                    // var name = "12 feet deep";
                    json = wc.DownloadString("https://api.themoviedb.org/3/search/movie?api_key=01080d24374dfd9c40970be178ae7948&language=en-US&query=" + name
                        + "&page=1&year=" + year);
                }
            }catch (Exception){
                Console.WriteLine("API FAILED");
                return null;
            }

            FilmInfoRaw account = JsonConvert.DeserializeObject<FilmInfoRaw>(json);

            return getHighestMatched(str, account); 
        }

        // Used to populate the list of films from the directory selected.
        private void PopulateList(){
            filmsList.Items.Clear();
            foreach(Film f in filmsAdded){
                filmsList.Items.Add(new { filmItem = f.title, film = f });
            }
        }

        // Called when the select folder button is clicked.
        private void selectFolder(object sender, RoutedEventArgs e) {
            getFilesFromFolder();
            PopulateList();
        }

        private string getYear(string str) {

            string output = "";

            char a = str[0];
            char b = str[1];
            char c = str[2];
            char d = str[3];

            output += a;
            output += b;
            output += c;
            output += d;

            return output;
        }

        

        // called when a film in the list is selected.
        private void clickedFilmItem(object sender, SelectionChangedEventArgs e){
            try {
                // Data from list
                Film f = getFilm(e.AddedItems[0]);

                // Data from list seperated into array list
                ArrayList list = seperateName(f.title);

                f.extention = getExtension(list);

                // Clean the file and rwmove everything after the date.
                f.rawName = cleanArrayList(list);

                // Obejct to hold all information about the film.
                FilmDetails details;

                // Regex to see if it has a date in the name.
                var k = Regex.Matches(f.title, @"\d{4}");

                // If the string has a date.
                if (k.Count > 0) {
                    // Get year of film.
                    f.year = getDate(ArrayToString(f.rawName));

                    // Get title of film without year.
                    f.titleWithoutYear = getStringWithoutDate(f.rawName);

                    // Search the films.
                    details = searchForFilm(f.titleWithoutYear, f.year, ArrayToString(f.rawName));

                    // Check if the film was found.
                    if (details != null) {
                        f.details = details;
                        string reg = Regex.Replace(details.title, "[@,\\.\":'\\\\]", string.Empty);
                        f.details.title = reg;        
                        selectedFilm = f;
                        filmTitle.Text = reg;
                        filmYear.Content = details.date;
                        overview.Text = details.overview;
                        poster.Visibility = Visibility.Visible;
                        poster.Source = new BitmapImage(new Uri("https://image.tmdb.org/t/p/w500/" + details.poster));
                        backdrop.Source = new BitmapImage(new Uri("https://image.tmdb.org/t/p/w500/" + details.backdrop));
                        renameFileButton.Visibility = Visibility.Visible;
                        backdrop.Visibility = Visibility.Visible;
                        if(filmsList.SelectedItems.Count > 1) {
                            renameFileButton.Content = "Rename " + filmsList.SelectedItems.Count + " films";
                        }
                    } else {
                        emptyValues();
                    }
                }
            } catch (Exception) {
                Console.WriteLine("ERROR");
            }
        }

        // Returns the exention.
        private string getExtension(ArrayList str) {
            return (string) str[str.Count - 1];
        }

        // Renames the file.
        private void renameFile(object sender, RoutedEventArgs e) {
            try {
                
                // Check if more than one is selected.
                IList list = filmsList.SelectedItems;
                ArrayList films = new ArrayList();
                if (list.Count > 1) {
                    for (int i = 0; i < list.Count; i++) {

                        try {
                            Film a = getFilm(list[i]);
                            selectedFilm = a;
                            string newFile = directorySelected + "\\" + selectedFilm.details.title + " (" + selectedFilm.details.date + ")" + "." + selectedFilm.extention;
                            System.IO.File.Move(selectedFilm.origin, newFile);
                            Console.WriteLine(i + " " + a.title);
                        } catch (Exception) {

                            Console.WriteLine("FAILED: " + selectedFilm.title);
                        }
                        
                    }
                    // Single film selected.
                } else {
                    string newFile = directorySelected + "\\" + selectedFilm.details.title + " (" + selectedFilm.details.date + ")" + "." + selectedFilm.extention;
                    System.IO.File.Move(selectedFilm.origin, newFile);
                }
               
                reloadList();
            } catch (Exception) {
                // HANDLE
            }
        }

        // Reloads the list after editing it.
        private void reloadList() {
            filmsList.Items.Clear();
            filmsAdded.Clear();
            if (!string.IsNullOrWhiteSpace(directorySelected)) {
                string[] files = Directory.GetFiles(directorySelected);

                if (files.Length > 0) {
                    for (int i = 0; i < files.Length; i++) {
                        Film f = new Film();
                        f.origin = files[i];
                        f.title = System.IO.Path.GetFileName(files[i]);
                        filmsAdded.Add(f);
                    }
                }
            }
            PopulateList();
        }

        // Empties the screen values.
        private void emptyValues() {
            filmTitle.Text = "";
            filmYear.Content = "";
            overview.Text = "";
            poster.Visibility = Visibility.Hidden;
            renameFileButton.Visibility = Visibility.Hidden;
            backdrop.Visibility = Visibility.Hidden;
        }

        // Seperates the name into an array list.
        private ArrayList seperateName(string name) {
            ArrayList list = new ArrayList();
            char[] delimiterChars = { ' ', ',', '.', ':', '\t', '\\', '-' };
            string[] words = name.Split(delimiterChars);
            foreach (string s in words) {
                list.Add(s);
            }

            return list;
        }

        // Prints the array to the string.
        private void printArray(ArrayList list) {
            foreach (string s in list) {
                Console.Write(s + " ");
            }

            Console.WriteLine();
        }

        // levenshtein algorithm
        private Int32 levenshtein(string a, string b) {

            if (string.IsNullOrEmpty(a)) {
                if (!string.IsNullOrEmpty(b)) {
                    return b.Length;
                }
                return 0;
            }

            if (string.IsNullOrEmpty(b)) {
                if (!string.IsNullOrEmpty(a)) {
                    return a.Length;
                }
                return 0;
            }

            Int32 cost;
            Int32[,] d = new int[a.Length + 1, b.Length + 1];
            Int32 min1;
            Int32 min2;
            Int32 min3;

            for (Int32 i = 0; i <= d.GetUpperBound(0); i += 1) {
                d[i, 0] = i;
            }

            for (Int32 i = 0; i <= d.GetUpperBound(1); i += 1) {
                d[0, i] = i;
            }

            for (Int32 i = 1; i <= d.GetUpperBound(0); i += 1) {
                for (Int32 j = 1; j <= d.GetUpperBound(1); j += 1) {
                    cost = Convert.ToInt32(!(a[i - 1] == b[j - 1]));

                    min1 = d[i - 1, j] + 1;
                    min2 = d[i, j - 1] + 1;
                    min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }

            return d[d.GetUpperBound(0), d.GetUpperBound(1)];

        }

        // Removes unwated words from the string to clean them.
        private ArrayList removeUnwantedStrings(ArrayList list) {

            printArray(list);

            for (int i = list.Count - 1; i >= 0; i--) {
                if (containsAny((string)list[i], fileExentions)) {
                    list.RemoveAt(i);
                } else {
                    if (containsAny((string)list[i], unwantedWords)) {
                        list.RemoveAt(i);
                    }
                }
            }
            printArray(list);
            return list;
        }

        // Checks if the word being sent in contains any of the words in the array.
        private bool containsAny(string word, string[] array) {
            foreach (string s in array) {
                if (word.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) {
                    return true;
                }
            }
            return false;
        }

        // Checks if a string has a number in.
        private bool HasNumber(string input) {
            return input.Where(x => Char.IsDigit(x)).Any();
        }

        // Cleans the array list only takes string from before the year.
        private ArrayList cleanArrayList(ArrayList list) {
            ArrayList output = new ArrayList();
            foreach (string a in list) {
                if (!isDate(a)) {
                    output.Add(a);
                } else {
                    output.Add(a);
                    break;
                }
            }
            return output;
        }

        // Returns a string from an arraylist without the date.
        private string getStringWithoutDate(ArrayList list) {
            string output = "";
            for (int i = 0; i < list.Count - 1; i++) {
                output += list[i] + " ";
            }
            return output;
        }

        // Gets the date from a string and returns it.
        private string getDate(string str) {
            var date = Regex.Matches(str, @"\d{4}");
            if (date.Count > 0) {
                return date[0].Value;
            } else {
                return null;
            }
        }

        // Checks if the given object is a number.
        public static bool IsNumeric(object Expression) {
            double retNum;

            bool isNum = Double.TryParse(Convert.ToString(Expression), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
            return isNum;
        }

        // Returns bool if a string is a date.
        private bool isDate(string str) {
            var date = Regex.Matches(str, @"\d{4}");
            if (date.Count > 0) {
                return true;
            } else {
                return false;
            }
        }

        // Converts the annonymous type to a film object.
        private Film getFilm(dynamic param) {
            return param.film;
        }

        // Converts and arraylist of strings to a string.
        private string ArrayToString(ArrayList list) {
            string str = "";
            foreach (string s in list) {
                str += s;
                str += " ";
            }
            return str;
        }

        // About immaculate menu button.
        private void aboutImmaculate(object sender, RoutedEventArgs e) {
            MessageBoxResult result = System.Windows.MessageBox.Show("Created By Aaron Nebbs( aaron@nebbs.com )", "About Immaculate", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Quit app menu option.
        private void quitApp(object sender, RoutedEventArgs e) {
            MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to close Immaculate?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) {
                System.Windows.Application.Current.Shutdown();
            }
        }
    }
}
