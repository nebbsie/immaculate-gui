using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immaculate.objects
{
    class FilmInfoRaw
    {
        public string page { get; set; }
        public int total_results { get; set; }
        public string total_pages { get; set; }
        public IList<dynamic> results { get; set; }
    }
}
