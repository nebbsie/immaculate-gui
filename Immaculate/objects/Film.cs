using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immaculate.objects
{
    class Film
    {
        public string title { get; set; }
        public string titleWithoutYear { get; set; }
        public string year { get; set; }
        public string origin { get; set; }
        public FilmDetails details { get; set; }
        public ArrayList rawName { get; set; }
        public string extention { get; set; }
    }
}
