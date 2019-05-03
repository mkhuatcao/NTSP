using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TSP
{
    public static class Utils
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            RNGCryptoServiceProvider r = new RNGCryptoServiceProvider();
            int count = list.Count;
            while (count > 0)
            {
                byte[] buffer = new byte[4];
                r.GetBytes(buffer);

                int idx = ((BitConverter.ToInt32(buffer, 0) & 0x7FFFFFFF) % count);
                T value = list[idx];
                list[idx] = list[--count];
                list[count] = value;
            }
        }

        public static List<Location> PMX(List<Location> parent1, List<Location> parent2)
        {
            int count = parent1.Count;
            List<Location> child_list = new List<Location>();

            for (int i = 0; i < count; i++)
            {
                child_list.Add(new Location());
            }
            foreach (var location in child_list)
            {
                location.Id = -1;
            }

            RNGCryptoServiceProvider r = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[4];
            r.GetBytes(buffer);

            int min = 4;
            int init_size = ((BitConverter.ToInt32(buffer, 0) & 0x7FFFFFFF) % (count -min)) + min;

            int start_idx = ((BitConverter.ToInt32(buffer, 0) & 0x7FFFFFFF) % (count-init_size));
            
            List<int> parent1SubString = new List<int>(init_size);
            List<int> parent2SubString = new List<int>(init_size);

            //copy initial substring and save id of parent1 and parent2 substring
            for (int i = start_idx; i < start_idx+init_size; i++)
            {
                child_list[i].Id = parent1[i].Id;
                child_list[i].X = parent1[i].X;
                child_list[i].Y = parent1[i].Y;

                parent1SubString.Add(parent1[i].Id);
                parent2SubString.Add(parent2[i].Id);
            }

            bool found = false;
            //find place for elements from parent2 substring
            for (int i = 0; i < init_size; i++)
            {
                if(!parent1SubString.Contains(parent2SubString[i]))
                {
                    found = false;
                    int index = i;
                    while (!found)
                    {
                        int tmp2 = parent2SubString.IndexOf(parent1SubString[index]);
                        if (tmp2 == -1)
                        {
                            found = true;
                        }
                        else
                        {
                            index = tmp2;
                        }
                    }
                    index = parent2.FindIndex(l => l.Id == parent1SubString[index]);
                    child_list[index].Id = parent2[start_idx + i].Id;
                    child_list[index].X = parent2[start_idx + i].X;
                    child_list[index].Y = parent2[start_idx + i].Y;
                }
            }

            for (int i = 0; i < count; i++)
            {
                if (child_list[i].Id == -1)
                {
                    child_list[i].Id = parent2[i].Id;
                    child_list[i].X = parent2[i].X;
                    child_list[i].Y = parent2[i].Y;
                }
            }

            return child_list;
        }

        public static void SwapEdges(this IList<Location> list)
        {
            if (list == null) return;
            List<Edge> edges = list.ConvertToEdgeList();
            RNGCryptoServiceProvider r = new RNGCryptoServiceProvider();

            int count = edges.Count;
            byte[] buffer = new byte[8];
            r.GetBytes(buffer);
            int idx = ((BitConverter.ToInt32(buffer, 0) & 0x7FFFFFFF) % count);
            int idx2 = ((BitConverter.ToInt32(buffer, 4) & 0x7FFFFFFF) % count);

            var edge1 = edges[idx];
            var edge2 = edges[idx2];

            while (!ValidateEdges(edge1, edge2))
            {
                r.GetBytes(buffer);
                idx2 = ((BitConverter.ToInt32(buffer, 0) & 0x7FFFFFFF) % count);
                edge2 = edges[idx2];
            }

            var i = list.IndexOf(edge1.B);
            var j = list.IndexOf(edge2.B);
            var start = Math.Min(i, j);
            var end = (start == i) ? j : i;
            list.ReverseSection(start, end);
        }

        // start - inclusive
        // end - exclusive
        public static void ReverseSection<T>(this IList<T> list, int start, int end)
        {
            int count = (end - start) / 2;
            for (int i = start; i < end; i++)
            {
                if (count == 0) break;
                var idx = end - (i - start + 1);
                T value = list[i];
                list[i] = list[idx];
                list[idx] = value;
                count--;
            }
        }

        private static bool ValidateEdges(Edge e1, Edge e2)
        {
            return (!e1.Equals(e2) && !ShareVerticle(e1, e2));
        }

        private static bool ShareVerticle(Edge e1, Edge e2)
        {
            return (e1.A == e2.A || e1.A == e2.B || e1.B == e2.A || e1.B == e2.B);
        }

        public static List<Edge> ConvertToEdgeList(this IList<Location> list)
        {
            List<Edge> edges = new List<Edge>();
            for (int i = 0; i < list.Count - 1; i++)
            {
                edges.Add(new Edge()
                {
                    A = list[i],
                    B = list[i + 1]
                });

            }
            edges.Add(new Edge()
            {
                A = list[list.Count - 1],
                B = list[0]
            });

            return edges;
        }


        public static double Distance(Location a, Location b)
        {
            double dX = a.X - b.X;
            double dY = a.Y - b.Y;
            return Math.Sqrt(Math.Pow(dX, 2) + Math.Pow(dY, 2));
        }

        public static double DistanceSum(List<Location> list)
        {
            double sum = 0;
            for (int i = 0; i < list.Count - 1; i++)
            {
                sum += Distance(list[i], list[i + 1]);
            }
            sum += Distance(list[list.Count - 1], list[0]);

            return sum;
        }

    }

    public class Edge : IEquatable<Edge>
    {
        public Location A { get; set; }
        public Location B { get; set; }

        public bool Equals(Edge other)
        {
            return (this.A == other.A && this.B == other.B);
        }
    }

    public class TspResult
    {
        public List<Location> BestTour { get; set; }
        public double Distance { get; set; }
        public double SolutionCount { get; set; }
    }
}