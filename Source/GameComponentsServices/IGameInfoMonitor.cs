using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.GameComponentsServices
{
    public interface IGameInfoMonitor
    {
        void Set(string name, string value, uint order);

        void Unset(string name);

        void Switch();

        void Switch(bool value);
    }
}