using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ComputationTheorySimulator.Exceptions
{
    public class SimulationException : Exception
    {
        public SimulationException(string message) : base(message) { }
    }
}