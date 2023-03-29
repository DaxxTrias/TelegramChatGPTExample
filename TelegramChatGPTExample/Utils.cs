using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloysterGPT
{
    internal class Utils
    {
        internal static bool WriteLine(string input, int level = 0)
        {
            try
            {
                if (input?.Length > 0)
                {
                    string sb = $"{DateTime.Now}: " + input;
                    Console.WriteLine(sb);

                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured: " + ex.Message + " loglevel: " + level.ToString());

                return false;
            }
        }
    }
}
