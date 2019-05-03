using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSP
{
    public class Location : IEquatable<Location>
    {
        public int Id { get; set; }
        public Double X { get; set; }
        public Double Y { get; set; }

        // override object.Equals
        public bool Equals(Location other)
        {
            return (this.Id == other.Id);
        }
    }

    class DataModel
    {
        public List<Location> Data = null;

        public DataModel(string path)
        {
            Data = new List<Location>();

            using (StreamReader sr = new StreamReader(path))
            {
                String line;
                bool start = false;
                String[] split;
                while ((line = sr.ReadLine()) != null)
                {

                    if (line == "EOF") break;

                    if (start)
                    {
                        split = line.Split(' ');
                        Data.Add(new Location()
                        {
                            Id = Int32.Parse(split[0]),
                            X = Double.Parse(split[1], CultureInfo.InvariantCulture),
                            Y = Double.Parse(split[2], CultureInfo.InvariantCulture)
                        });
                    }

                    if (line == "NODE_COORD_SECTION") start = true;
                }
            }
        }
    }
}
