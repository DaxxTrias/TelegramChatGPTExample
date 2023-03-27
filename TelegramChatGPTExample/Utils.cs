using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloysterGPT
{
    internal class Utils
    {
        internal static bool WriteLine(string input)
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
            catch (Exception)
            {
                //todo: surely we dont need to capture the exception?
                return false;
            }
        }
    }
}
